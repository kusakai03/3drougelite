using System;
using System.Collections.Generic;
using System.Collections;
using UnityEngine.SceneManagement;
using UnityEngine;

public class StageManager : MonoBehaviour
{
    public static StageManager Instance { get; private set; }
    public Action<CombatState> OnCombatChanged;
    public event Action<string, MerchantItem> OnPlayerPurchase;
    public event Action<string, ArtifactInstance> OnArtifactReplaced;
    public event Action<RunResult> OnRunEnded;
    public List<CharacterData> characters = new();
    public int currentRoomIndex;

    [SerializeField] private RoomManager roomManager;
    [SerializeField] private MapSpawner mapSpawner;
    [SerializeField] private NavMeshGenerator navMeshGenerator;
    [SerializeField] private FloatingDamage floatingDamagePrefab;
    [SerializeField] private StageSettings stageSettings;
    private StageData stageData;

    private List<BiomeData> biomes = new();
    private List<TriggerGroupData> triggerGroups = new();
    private readonly List<GameObject> spawnedEntities = new();
    private readonly List<GameObject> keyEntities = new();
    private readonly HashSet<string> playersInPortal = new();
    private bool portalUnlocked;
    private bool changingRoom;
    private readonly HashSet<string> activatedTriggerIDs = new();
    private GeneratedMap currentMap;
    public Action<MapNode, List<string>> OnSpawnPlayers;
    public Action<string, BlessingTrigger> OnBlessingTrigger;
    [Header("Threat")]
    public int threatLevel = 0;
    public int threatXP = 0;
    public int threatXPCap = 10;

    private Coroutine threatCoroutine;

    private const int MaxThreatLevel = 10;
    private const int ThreatXPPerKill = 3;
    private const float ThreatXPInterval = 4f;
    private float runStartTime;
    private bool runEnded;
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        biomes.AddRange(
            Resources.LoadAll<BiomeData>("Map/Biomes")
        );

        triggerGroups.AddRange(
            Resources.LoadAll<TriggerGroupData>("Game/TriggerGroup")
        );
    }

    
    private void Start()
    {
        StartCoroutine(InitializeStage());
    }

    private IEnumerator InitializeStage()
    {
        for (int attempt = 0; attempt < 5; attempt++)
        {
            LocalPlayerContext context = LocalPlayerContext.Instance;

            // Kiểm tra có PlayerID và LobbyID.
            if (context != null &&
                !string.IsNullOrEmpty(context.PlayerID) &&
                !string.IsNullOrEmpty(context.LobbyID))
            {
                LobbyManager manager = LobbyManager.Instance;

                if (manager != null)
                {
                    Lobby lobby = manager.GetLobby(context.LobbyID);

                    if (lobby != null)
                    {
                        if (lobby.AreAllPlayersReady())
                        {
                            StartRun(lobby.GetPlayerIDs());
                        }

                        yield break;
                    }
                }
            }
            yield return new WaitForSeconds(1f);
        }

        GlobalCanvas.Instance.SendErrorLog(
            "Không tìm thấy phòng chơi!",
            () => SceneManager.LoadScene("MainMenu")
        );
    }

    public CharacterData GetCharacter(string playerID)
    {
        return characters.Find(character => character.playerID == playerID);
    }
    private void CreateStage()
    {
        stageData.stageDifficulty = stageSettings.roomLevel;

        for (int i = 0; i < stageSettings.numberOfRooms; i++)
        {
            StageRoomData room = new StageRoomData();

            room.biome = stageSettings.biome != null
                ? stageSettings.biome
                : biomes[UnityEngine.Random.Range(0, biomes.Count)];

            if (stageSettings.useTriggerGroup)
            {
                TriggerGroupData triggerGroup = FindTriggerGroup(room.biome.biomeTags);

                if (triggerGroup != null)
                    BuildRoomNodes(room, triggerGroup);
            }

            stageData.rooms.Add(room);
        }
    }

    private TriggerGroupData FindTriggerGroup(SpawnTag biomeTags)
    {
        List<TriggerGroupData> validGroups = new();

        foreach (TriggerGroupData group in triggerGroups)
        {
            if ((group.spawnTag & biomeTags) != 0)
            {
                validGroups.Add(group);
            }
        }

        if (validGroups.Count == 0)
        {
            Debug.LogError($"Không tìm thấy TriggerGroup cho SpawnTag: {biomeTags}");
            return null;
        }

        return validGroups[UnityEngine.Random.Range(0, validGroups.Count)];
    }

    private void BuildRoomNodes(StageRoomData room, TriggerGroupData triggerGroup)
    {
        foreach (StageTriggerConfig node in stageSettings.globalTriggerNodes)
        {
            room.nodes.Add(node);
        }

        foreach (StageTriggerConfig node in triggerGroup.uniqueTriggerNodes)
        {
            room.nodes.Add(node);
        }

        int randomCount = Mathf.Max(0, stageSettings.nodeCount - room.nodes.Count);

        List<StageTriggerConfig> randomNodes = triggerGroup.randomTriggerNodes;

        if (randomNodes.Count > 0)
        {
            for (int i = 0; i < randomCount; i++)
            {
                int index = UnityEngine.Random.Range(0, randomNodes.Count);
                room.nodes.Add(randomNodes[index]);
            }
        }
    }

    public void StartRun(List<string> playerIDs)
    {
        stageData = new StageData();
        runEnded = false;
        runStartTime = Time.time;
        characters.Clear();

        foreach (string playerID in playerIDs)
            RegisterPlayer(playerID);

        currentRoomIndex = 0;

        CreateStage();
        roomManager.OnPlayerSpawn += (node) =>
        {
            OnSpawnPlayers?.Invoke(node, stageData.playerIDs);
        };
        roomManager.OnMapGenerated += (map) =>
        {
            currentMap = map;
            StartCoroutine(LoadGeneratedMap(map));
        };

        GenerateCurrentRoom();
    }
    private IEnumerator LoadGeneratedMap(GeneratedMap map)
    {
        float t = Time.realtimeSinceStartup;

        yield return mapSpawner.Spawn(map);

        Debug.Log($"MapSpawner: {(Time.realtimeSinceStartup - t) * 1000f} ms");

        yield return null;

        t = Time.realtimeSinceStartup;

        yield return navMeshGenerator.BakeNavMesh(map);

        Debug.Log($"NavMesh: {(Time.realtimeSinceStartup - t) * 1000f} ms");
    }
    private void ClearEntities()
    {
        foreach (GameObject entity in spawnedEntities)
        {
            if (entity != null)
                Destroy(entity);
        }

        spawnedEntities.Clear();
    }
    private void GenerateCurrentRoom()
    {
        ClearEntities();

        keyEntities.Clear();
        portalUnlocked = !stageData.hasKeyRoom;
        changingRoom = false;
        playersInPortal.Clear();
        activatedTriggerIDs.Clear();

        StageRoomData room = stageData.rooms[currentRoomIndex];

        roomManager.GenerateMap(room);
        GenerateMerchantStock(room);
        Invoke(nameof(StartThreat), 3f);
    }
    public void EndRun(RunEndReason reason = RunEndReason.Debug)
    {
        if (runEnded) return;
        runEnded = true;
        changingRoom = true;

        StopThreat();

        RunResult result = BuildRunResult(reason);
        OnRunEnded?.Invoke(result);
    }

    private RunResult BuildRunResult(RunEndReason reason)
    {
        RunResult result = new();

        // Stage
        if (stageSettings != null)
        {
            result.stageID = stageSettings.stageID;
            result.stageDifficulty = stageSettings.roomLevel;
        }

        if (stageData != null)
        {
            result.totalRooms = stageData.rooms.Count;
            result.hasKeyRoom = stageData.hasKeyRoom;
        }

        result.currentRoomIndex = currentRoomIndex;

        // Threat
        result.threatLevel = threatLevel;
        result.threatXP = threatXP;

        // Players
        if (stageData != null && stageData.playerIDs != null)
        {
            foreach (string playerID in stageData.playerIDs)
            {
                CharacterData character = GetCharacter(playerID);

                if (character == null)
                    continue;

                PlayerResult p = new()
                {
                    playerID = playerID,
                    level = character.currentLevel,
                    currentXP = character.currentXP,
                    currentXPCap = character.currentXPCap,
                    atk = character.combat != null ? character.combat.Atk : 0f,
                    maxHp = character.combat != null ? character.combat.Hp : 0f,
                    currentHp = character.combat != null ? character.combat.currentHp : 0f,
                    tp = character.tp,
                    inventory = new List<InventoryItem>(character.inventory),
                    blessings = new List<BlessingInstance>(character.blessings),
                    artifacts = new List<ArtifactInstance>(character.artifacts),
                };

                result.players.Add(p);
            }
        }

        // Rooms
        if (stageData != null)
        {
            for (int i = 0; i < stageData.rooms.Count; i++)
            {
                StageRoomData room = stageData.rooms[i];

                result.rooms.Add(new RoomResult
                {
                    index = i,
                    biomeID = room.biome != null ? room.biome.biomeID : "",
                    cleared = i < currentRoomIndex
                });
            }
        }

        result.endReason = reason;
        result.runDuration = Time.time - runStartTime;
        result.endTime = DateTime.Now;

        return result;
    }

    public void Clean()
    {
        ClearEntities();
        keyEntities.Clear();

        playersInPortal.Clear();
        activatedTriggerIDs.Clear();
        portalUnlocked = false;
        changingRoom = false;
        runEnded = false;

        characters.Clear();
        stageData = null;
        currentMap = null;
        currentRoomIndex = 0;

        threatLevel = 0;
        threatXP = 0;
        threatXPCap = 10;
        threatCoroutine = null;

    }

    public void NextRoom()
    {
        currentRoomIndex++;

        if (currentRoomIndex >= stageData.rooms.Count)
        {
            EndRun(RunEndReason.Victory);
            return;
        }

        GenerateCurrentRoom();
    }
    
    public void AddExp(string playerID, int baseExp)
    {
        CharacterData character = GetCharacter(playerID);

        if (character == null)
        {
            Debug.LogError($"Character not found: {playerID}");
            return;
        }
        float multi = 1 + currentRoomIndex;
        float finalExp = baseExp * multi;
        character.currentXP += (int)finalExp;

        bool leveledUp = false;

        while (character.currentXP >= character.currentXPCap)
        {
            character.currentLevel++;

            int nextLevelRequirement = CalculateXPCap(character.currentLevel);
            character.currentXPCap += nextLevelRequirement;

            leveledUp = true;

            if (character.currentLevel % 3 == 0 &&
                character.currentLevel > character.highestBlessingLevelQueued)
            {
                QueueBlessingChoice(character);
                character.highestBlessingLevelQueued = character.currentLevel;
            }
        }

        if (leveledUp)
            UpdateCharacterStats(playerID);
    }
    private void QueueBlessingChoice(CharacterData character)
    {
        BlessingChoiceGroup group = new();

        group.choices.AddRange(
            BlessingManager.Instance.GetRandomBlessings(3)
        );

        character.queueBlessings.Add(group);
    }
    public void SelectBlessing(string playerID, BlessingData blessingData)
    {
        CharacterData character = GetCharacter(playerID);

        if (character == null || blessingData == null)
            return;

        if (character.queueBlessings.Count == 0)
            return;

        BlessingInstance blessingInstance = new BlessingInstance(blessingData);

        character.blessings.Add(blessingInstance);

        character.queueBlessings.RemoveAt(0);
    }
    public int CalculateXPCap(int level)
    {
        const float baseXP = 10f;

        return Mathf.RoundToInt(baseXP * Mathf.Pow(level, 1.1f));
    }

    public int CalculateEntityLevel()
    {
        int baseLevel = stageData.stageDifficulty * (currentRoomIndex + 1);
        int randomOffset = UnityEngine.Random.Range(-10, 11);

        return Mathf.Max(1, baseLevel + randomOffset);
    }

    public CombatStats CalculateEntityStats(EntityData entityData, int entityLevel)
    {
        float levelBonus = 0.05f * (entityLevel - 1);
        float threatBonus = 0.1f * threatLevel;
        float multiplier = 1f + levelBonus + threatBonus;

        CombatStats stats = new();

        stats.atk = entityData.baseAtk * multiplier;
        stats.hp = entityData.baseHp * multiplier;
        stats.currentHp = stats.hp;

        return stats;
    }

    public void AddBlessing(string playerID, BlessingData blessingData)
    {
        CharacterData character = GetCharacter(playerID);

        if (character == null || blessingData == null)
            return;

        BlessingInstance blessing = new BlessingInstance(blessingData);

        character.blessings.Add(blessing);
    }
    
    public void UpdateCharacterStats(string playerID)
    {
        CharacterData character = GetCharacter(playerID);

        if (character == null)
            return;

        BonusStats totalBonus = BlessingManager.Instance.CalculateTotalBonus(playerID);
        BonusStats artifactBonus = ArtifactManager.Instance.CalculateTotalBonus(character);
        totalBonus.Add(artifactBonus);
        float oldMaxHp = character.combat.Hp;
        float oldCurrentHp = character.combat.currentHp;

        character.combat = PlayerStatCalculator.Calculate(character, totalBonus);
        character.combat.currentHp = Mathf.Min(oldCurrentHp, character.combat.Hp);

        GameObject player = PlayerManager.Instance.GetPlayer(playerID);

        if (player != null)
        {
            CombatInstance combat =
                player.GetComponent<CombatInstance>();

            if (combat != null)
            {
                CombatState newState = combat.state.Clone();
                newState.stats = character.combat.Clone();

                OnCombatChanged?.Invoke(newState);
            }
        }
    }
    
    public void Hit(CombatInstance attacker, CombatInstance defender, float damageMultiplier)
    {
        DamageHit(attacker, defender, damageMultiplier, DamageType.Physical, DamageHitType.Strike);
    }

    public void DamageEffect(CombatInstance attacker, CombatInstance defender, Vector3 position, float damageMultiplier, DamageType damageType, DamageHitType hitType)
    {
        Debug.Log($"DAMAGE EFFECT | type={damageType} | attacker={attacker} | defender={defender} | pos={position}");

        if (attacker == null)
        {
            Debug.LogWarning("DAMAGE EFFECT STOPPED: attacker is null");
            return;
        }

        switch (damageType)
        {
            case DamageType.Explosion:
                Debug.Log("CALLING EXPLOSION DAMAGE");
                ExplosionDamage(attacker, position, damageMultiplier);
                break;

            default:
                if (defender != null)
                    DamageHit(attacker, defender, damageMultiplier, damageType, hitType);
                break;
        }
    }

    public void DamageHit(CombatInstance attacker, CombatInstance defender, float damageMultiplier, DamageType damageType, DamageHitType hitType, float? rawDamage = null)
    {
        if (attacker == null) return;
        if (defender == null) return;

        if (!stageSettings.takeDamage && damageType != DamageType.Heal)
            {
                float vdamage = rawDamage ?? attacker.state.stats.Atk * damageMultiplier;
                SpawnFloatingDamage(defender, Mathf.Abs(vdamage), damageType);
                return;
            }


        CombatState newState = defender.state.Clone();

        if (newState.stats.currentHp <= 0f && damageType != DamageType.Heal)
            return;

        float damage = rawDamage ?? attacker.state.stats.Atk * damageMultiplier;

        newState.stats.currentHp = damageType == DamageType.Heal ? Mathf.Clamp(newState.stats.currentHp + Mathf.Abs(damage), 0f, newState.stats.Hp) : Mathf.Clamp(newState.stats.currentHp - damage, 0f, newState.stats.Hp);
        if (damageType == DamageType.Heal)
            Debug.Log($"Healing {defender.name} for {Mathf.Abs(damage)} HP. New HP: {newState.stats.currentHp}/{newState.stats.Hp}");
        SpawnFloatingDamage(defender, Mathf.Abs(damage), damageType);

        if (newState.stats.currentHp <= 0f)
        {
            EntityAgent entityAgent = defender.GetComponent<EntityAgent>();

            if (entityAgent != null)
                entityAgent.Die();
        }

        RaiseCombatChanged(newState);
    }
    
    private void RaiseCombatChanged(CombatState newState)
    {
        OnCombatChanged?.Invoke(newState);
        CheckAllPlayersDead();
    }

    public void RestPlayer(PlayerInstance player)
    {
        if (player == null)
            return;

        CombatInstance combat = player.GetComponent<CombatInstance>();

        if (combat == null || combat.state == null)
            return;
        Debug.Log($"Resting player {player.name} | HP before: {combat.state.stats.currentHp}/{combat.state.stats.Hp}");
        float missingHp = combat.state.stats.Hp - combat.state.stats.currentHp;

        if (missingHp > 0f)
            DamageHit(combat, combat, 1f, DamageType.Heal, DamageHitType.Strike, missingHp);

        ReactionReceiver receiver = player.GetComponent<ReactionReceiver>();

        if (receiver != null)
            receiver.ClearTemporaryEffects();
    }

    private void ExplosionDamage(CombatInstance attacker, Vector3 position, float damageMultiplier)
    {
        float radius = 15f;
        float explosionForce = 8f;

        Collider[] hits = Physics.OverlapSphere(position, radius);

        HashSet<CombatInstance> damagedTargets = new();
        HashSet<Rigidbody> pushedBodies = new();

        foreach (Collider hit in hits)
        {
            CombatInstance defender = hit.GetComponentInParent<CombatInstance>();

            if (defender != null && damagedTargets.Add(defender))
                DamageHit(attacker, defender, damageMultiplier, DamageType.Explosion, DamageHitType.Strike);

            Rigidbody rb = hit.attachedRigidbody;

            if (rb != null && !rb.isKinematic && pushedBodies.Add(rb))
                rb.AddExplosionForce(explosionForce, position, radius, 0.5f, ForceMode.Impulse);
        }
    }

    private void SpawnFloatingDamage(CombatInstance defender, float damage, DamageType damageType)
    {
        if (floatingDamagePrefab == null)
            return;
        Color color = GetDamageColor(damageType);
        Vector3 position =
            defender.transform.position + Vector3.up * 2f;

        FloatingDamage popup =
            Instantiate(floatingDamagePrefab, position, Quaternion.identity);
        popup.Setup(damage, color);
    }

    private Color GetDamageColor(DamageType damageType)
    {
        return damageType switch
        {
            DamageType.Physical => Color.white,
            DamageType.Fire => new Color(1f, 0.25f, 0.1f),
            DamageType.Cold => new Color(0.4f, 0.8f, 1f),
            DamageType.Electro => new Color(0.75f, 0.4f, 1f),
            DamageType.Water => new Color(0.2f, 0.55f, 1f),
            DamageType.Explosion => new Color(1f, 0.65f, 0.1f),
            DamageType.Heal => new Color(0.3f, 1f, 0.4f),
            _ => Color.white
        };
    }

    public void EntityDefeated(GameObject entity)
    {
        if (entity == null)
            return;

        if (!spawnedEntities.Contains(entity))
            return;

        AddThreatXP(ThreatXPPerKill);

        foreach (string playerID in stageData.playerIDs)
            AddExp(playerID, 10);

        CheckKeyEntities(entity);
    }

    private void CheckKeyEntities(GameObject defeatedEntity)
    {
        if (portalUnlocked)
            return;

        if (keyEntities.Count == 0)
            return;

        foreach (GameObject entity in keyEntities)
        {
            if (entity == null)
                continue;

            if (entity == defeatedEntity)
                continue;

            CombatInstance combat = entity.GetComponent<CombatInstance>();

            if (combat != null && combat.state.stats.currentHp > 0f)
                return;
        }

        UnlockPortals();
    }

    private void UnlockPortals()
    {
        if (portalUnlocked)
            return;

        portalUnlocked = true;
    }

    public void ActivateTrigger(TriggerInstance trigger)
    {
        List<GameObject> entities = mapSpawner.SpawnEntities(trigger.triggerData.spawnEntities, roomManager.GetNodeByID(trigger.nodeID), currentMap);

        spawnedEntities.AddRange(entities);

        if (trigger.triggerData.type == TriggerType.Boss)
            keyEntities.AddRange(entities);
    }
    
    public void TriggerBlessing(string playerID, BlessingTrigger trigger)
    {
        OnBlessingTrigger?.Invoke(playerID, trigger);
    }
    public void RegisterPlayer(string playerID)
    {
        if (string.IsNullOrEmpty(playerID))
            return;

        if (stageData.playerIDs.Contains(playerID))
            return;

        stageData.playerIDs.Add(playerID);

        CharacterData character = new CharacterData();
        character.playerID = playerID;
        BonusStats bonus = new();

        character.combat = PlayerStatCalculator.Calculate(character, bonus);

        character.currentHp = character.combat.Hp;
        character.combat.currentHp = character.combat.Hp;
        characters.Add(character);
    }
    public IReadOnlyList<string> GetPlayerIDs()
    {
        return stageData.playerIDs;
    }

    private int CalculateThreatXPCap(int threatLevel)
    {
        const float baseXP = 10f;
        return Mathf.RoundToInt(baseXP * Mathf.Pow(threatLevel + 1, 1.1f));
    }
    public void AddThreatXP(int amount)
    {
        if (threatLevel >= MaxThreatLevel)
            return;

        threatXP += amount;

        while (threatXP >= threatXPCap && threatLevel < MaxThreatLevel)
        {
            threatXP -= threatXPCap;
            threatLevel++;

            UpdateEntityStatsForThreat();

            if (threatLevel >= MaxThreatLevel)
            {
                threatLevel = MaxThreatLevel;
                threatXP = 0;
                OnThreatMaxed();
                break;
            }

            threatXPCap = CalculateThreatXPCap(threatLevel);
        }
    }

    private IEnumerator ThreatTimer()
    {
        while (threatLevel < MaxThreatLevel)
        {
            yield return new WaitForSeconds(ThreatXPInterval);
            AddThreatXP(1);
        }

        threatCoroutine = null;
    }

    private void StartThreat()
    {
        if (threatCoroutine != null)
            StopCoroutine(threatCoroutine);

        threatLevel = 0;
        threatXP = 0;
        threatXPCap = CalculateThreatXPCap(0);

        threatCoroutine = StartCoroutine(ThreatTimer());
    }

    private void StopThreat()
    {
        if (threatCoroutine != null)
        {
            StopCoroutine(threatCoroutine);
            threatCoroutine = null;
        }
    }

    private void UpdateEntityStatsForThreat()
    {
        foreach (GameObject entity in spawnedEntities)
        {
            if (entity == null)
                continue;

            CombatInstance combat = entity.GetComponent<CombatInstance>();
            EntityInstance entityInstance = entity.GetComponent<EntityInstance>();

            if (combat == null || entityInstance == null)
                continue;

            if (combat.state.stats.currentHp <= 0f)
                continue;

            float oldMaxHp = combat.state.stats.Hp;
            float oldCurrentHp = combat.state.stats.currentHp;
            bool wasFullHp = Mathf.Approximately(oldCurrentHp, oldMaxHp);

            CombatStats updatedStats = CalculateEntityStats(entityInstance.Data, entityInstance.Level);

            if (wasFullHp)
                updatedStats.currentHp = updatedStats.Hp;
            else
                updatedStats.currentHp = Mathf.Min(oldCurrentHp, updatedStats.Hp);

            CombatState newState = combat.state.Clone();
            newState.stats = updatedStats;

            OnCombatChanged?.Invoke(newState);
        }
    }

    private void OnThreatMaxed()
    {
        mapSpawner.ActivateAllTriggers();
        SetAllEntityDetectionRange(999999f);
    }

    private void SetAllEntityDetectionRange(float range)
    {
        foreach (GameObject entity in spawnedEntities)
        {
            if (entity == null)
                continue;

            EntityAgent agent = entity.GetComponent<EntityAgent>();

            if (agent != null)
                agent.SetDetectionRange(range);
        }
    }

    public bool AddToInventory(string playerID, string itemID, int quantity = 1)
    {
        if (quantity <= 0)
            return false;

        CharacterData character = GetCharacter(playerID);

        if (character == null)
            return false;

        ItemData itemData = ItemManager.Instance.GetItem(itemID);

        if (itemData == null)
            return false;

        InventoryItem existingItem = character.inventory.Find(x => x.itemID == itemID);

        if (existingItem != null)
        {
            existingItem.quantity += quantity;
            return true;
        }

        if (character.inventory.Count >= 12)
            return false;

        character.inventory.Add(new InventoryItem
        {
            itemID = itemID,
            quantity = quantity
        });

        return true;
    }

    public bool AddArtifact(string playerID, ArtifactInstance artifact)
    {
        if (artifact == null)
            return false;

        CharacterData character = GetCharacter(playerID);

        if (character == null)
            return false;

        if (character.artifacts.Count >= 6)
            return false;

        character.artifacts.Add(artifact);

        UpdateCharacterStats(playerID);

        return true;
    }
    public ArtifactInstance ReplaceArtifact(string playerID, int slotIndex, ArtifactInstance newArtifact)
    {
        if (newArtifact == null)
            return null;

        CharacterData character = GetCharacter(playerID);

        if (character == null)
            return null;

        if (slotIndex < 0 || slotIndex >= character.artifacts.Count)
            return null;

        ArtifactInstance oldArtifact = character.artifacts[slotIndex];

        character.artifacts[slotIndex] = newArtifact;

        UpdateCharacterStats(playerID);

        OnArtifactReplaced?.Invoke(playerID, newArtifact);

        return oldArtifact;
    }
    
    public void DropItem(ItemData item, int quantity, Vector3 position, ArtifactInstance artifact = null)
    {
        if (item == null || quantity <= 0)
            return;

        LootDrop drop = new LootDrop
        {
            item = item,
            quantity = artifact != null ? 1 : quantity,
            artifact = artifact
        };

        LootManager.Instance.SpawnDrops(new List<LootDrop> { drop }, position);
    }

    private void GenerateMerchantStock(StageRoomData room)
    {
        if (room == null)
            return;

        room.merchant.items.Clear();

        if (stageSettings.merchantSandbox)
        {
            GenerateAllMerchantBlessings(room.merchant);
            GenerateAllMerchantArtifacts(room.merchant);
            GenerateAllMerchantThrowingObjects(room.merchant);
            return;
        }

        GenerateMerchantBlessings(room.merchant);
        GenerateMerchantArtifacts(room.merchant);
        GenerateMerchantThrowingObjects(room.merchant);
    }

    private void GenerateMerchantBlessings(Merchant merchant)
    {
        List<BlessingData> pool = ItemManager.Instance.GetAllItems<BlessingData>();

        for (int i = 0; i < 5 && pool.Count > 0; i++)
        {
            int index = UnityEngine.Random.Range(0, pool.Count);
            BlessingData blessing = pool[index];

            int cost = merchant.CalculateCost(merchant.blessingCost, currentRoomIndex, blessing.rarity);

            merchant.items.Add(new MerchantItem(blessing, 1, cost));

            pool.RemoveAt(index);
        }
    }

    private void GenerateMerchantThrowingObjects(Merchant merchant)
    {
        List<ThrowObj> pool = ItemManager.Instance.GetAllItems<ThrowObj>();

        for (int i = 0; i < 5 && pool.Count > 0; i++)
        {
            int index = UnityEngine.Random.Range(0, pool.Count);

            ThrowObj throwObj = pool[index];

            int cost = merchant.CalculateCost(merchant.throwingObjectCost, currentRoomIndex, throwObj.rarity);
            merchant.items.Add(new MerchantItem(throwObj, 50, cost));

            pool.RemoveAt(index);
        }
    }

    private void GenerateMerchantArtifacts(Merchant merchant)
    {
        List<ArtifactData> pool = ItemManager.Instance.GetAllItems<ArtifactData>();

        for (int i = 0; i < 5 && pool.Count > 0; i++)
        {
            int index = UnityEngine.Random.Range(0, pool.Count);

            ArtifactData artifactData = pool[index];

            ArtifactInstance artifact = ArtifactManager.Instance.CreateArtifact(artifactData);

            int cost = merchant.CalculateCost(merchant.artifactCost, currentRoomIndex, artifact.level);
            merchant.items.Add(new MerchantItem(artifactData, 1, cost, artifact));

            pool.RemoveAt(index);
        }
    }

    private void GenerateAllMerchantBlessings(Merchant merchant)
    {
        List<BlessingData> pool = ItemManager.Instance.GetAllItems<BlessingData>();

        foreach (BlessingData blessing in pool)
        {
            merchant.items.Add(
                new MerchantItem(blessing, 1, 0)
            );
        }
    }
    private void GenerateAllMerchantThrowingObjects(Merchant merchant)
    {
        List<ThrowObj> pool = ItemManager.Instance.GetAllItems<ThrowObj>();

        foreach (ThrowObj throwObj in pool)
        {
            merchant.items.Add(
                new MerchantItem(throwObj, 50, 0)
            );
        }
    }
    private void GenerateAllMerchantArtifacts(Merchant merchant)
    {
        List<ArtifactData> pool = ItemManager.Instance.GetAllItems<ArtifactData>();

        foreach (ArtifactData artifactData in pool)
        {
            ArtifactInstance artifact = ArtifactManager.Instance.CreateArtifact(artifactData);

            merchant.items.Add(
                new MerchantItem(artifactData, 1, 0, artifact)
            );
        }
    }

    public bool CanBuyMerchantItem(string playerID, MerchantItem merchantItem)
    {
        if (merchantItem == null || merchantItem.item == null)
            return false;

        if (merchantItem.soldOut)
            return false;

        CharacterData character = GetCharacter(playerID);

        if (character == null)
            return false;

        return character.tp >= merchantItem.cost;
    }

    public MerchantPurchaseResult BuyMerchantItem(string playerID, MerchantItem merchantItem)
    {
        if (!CanBuyMerchantItem(playerID, merchantItem))
            return MerchantPurchaseResult.Failed;

        CharacterData character = GetCharacter(playerID);

        if (character == null)
            return MerchantPurchaseResult.Failed;

        if (merchantItem.item.itemType == ItemType.Artifact && character.artifacts.Count >= 6)
            return MerchantPurchaseResult.RequireArtifactReplace;

        if (!GiveMerchantItem(playerID, merchantItem))
            return MerchantPurchaseResult.Failed;

        if (!CompleteMerchantPurchase(playerID, merchantItem))
            return MerchantPurchaseResult.Failed;

        return MerchantPurchaseResult.Success;
    }

    private bool GiveMerchantItem(string playerID, MerchantItem merchantItem)
    {
        switch (merchantItem.item.itemType)
        {
            case ItemType.ThrowObject:
                return AddToInventory(playerID, merchantItem.item.itemID, merchantItem.quantity);

            case ItemType.Blessing:
            {
                BlessingData blessing = merchantItem.item as BlessingData;

                if (blessing == null)
                    return false;

                CharacterData character = GetCharacter(playerID);

                if (character == null)
                    return false;

                character.blessings.Add(new BlessingInstance(blessing));

                UpdateCharacterStats(playerID);

                return true;
            }

            case ItemType.Artifact:
            {
                if (merchantItem.artifact == null)
                    return false;

                return AddArtifact(playerID, merchantItem.artifact);
            }
        }

    return false;
}
    public bool RefreshMerchant(string playerID, StageRoomData room)
    {
        if (room == null || room.merchant == null)
            return false;

        Merchant merchant = room.merchant;

        if (!merchant.allowRefresh)
            return false;

        CharacterData character = GetCharacter(playerID);

        if (character == null)
            return false;

        int cost = merchant.CalculateCost(merchant.refreshCost, currentRoomIndex, merchant.numberOfRefreshed + 1);

        if (character.tp < cost)
            return false;

        character.tp -= cost;

        merchant.numberOfRefreshed++;

        GenerateMerchantStock(room);

        return true;
    }

    public bool UpgradeArtifact(string playerID, StageRoomData room, ArtifactInstance artifact)
    {
        if (room == null || room.merchant == null || artifact == null)
            return false;

        if (artifact.level >= 3)
            return false;

        CharacterData character = GetCharacter(playerID);

        if (character == null)
            return false;

        if (!character.artifacts.Contains(artifact))
            return false;

        int cost = room.merchant.GetArtifactUpgradeCost(artifact, currentRoomIndex);

        if (character.tp < cost)
            return false;

        if (!ArtifactManager.Instance.UpgradeArtifact(artifact))
            return false;

        character.tp -= cost;

        UpdateCharacterStats(playerID);

        return true;
    }
    public void EnterMerchantTrigger(TriggerInstance trigger, PlayerInstance player)
    {
        StageRoomData room = stageData.rooms[currentRoomIndex];

        if (room == null || room.merchant == null)
            return;

        UIManager.Instance.OpenMerchantUI(player.PlayerID, room);
    }

    public void ExitMerchantTrigger(TriggerInstance trigger, PlayerInstance player)
    {
        UIManager.Instance.CloseMerchantUI(player.PlayerID);
    }

    public bool SellInventoryItem(string playerID, int slotIndex)
    {
        CharacterData character = GetCharacter(playerID);

        if (character == null)
            return false;

        if (slotIndex < 0 || slotIndex >= character.inventory.Count)
            return false;

        InventoryItem inventoryItem = character.inventory[slotIndex];
        CurrencyData currency = ItemManager.Instance.GetItem<CurrencyData>(inventoryItem.itemID);

        if (currency == null)
            return false;

        int tpReceived = currency.tpValue * inventoryItem.quantity;

        character.tp += tpReceived;
        character.inventory.RemoveAt(slotIndex);

        return true;
    }

    public bool CompleteMerchantPurchase(string playerID, MerchantItem merchantItem)
    {
        if (merchantItem == null || merchantItem.soldOut)
            return false;

        CharacterData character = GetCharacter(playerID);

        if (character == null)
            return false;

        if (character.tp < merchantItem.cost)
            return false;

        character.tp -= merchantItem.cost;
        merchantItem.soldOut = true;

        OnPlayerPurchase?.Invoke(playerID, merchantItem);

        return true;
    }

    public void OpenChest(TriggerInstance trigger, PlayerInstance player)
    {
        if (trigger == null || trigger.triggerData.chestData == null)
            return;

        if (activatedTriggerIDs.Contains(trigger.nodeID))
            return;

        activatedTriggerIDs.Add(trigger.nodeID);

        ChestData chest = trigger.triggerData.chestData;

        List<LootDrop> drops = LootManager.Instance.RollPool(chest.lootPool, chest.minDrops, chest.maxDrops);
        LootManager.Instance.SpawnDrops(drops, trigger.transform.position);

        trigger.Activate(true);
    }

    public void EnterPortal(TriggerInstance trigger, PlayerInstance player)
    {
        if (!portalUnlocked || player == null)
            return;

        playersInPortal.Add(player.PlayerID);

        CheckAllPlayersInPortal();
    }

    public void ExitPortal(TriggerInstance trigger, PlayerInstance player)
    {
        if (player == null)
            return;

        playersInPortal.Remove(player.PlayerID);
    }

    private void CheckAllPlayersInPortal()
    {
        if (!portalUnlocked)
            return;

        if (changingRoom)
            return;

        if (stageData.playerIDs.Count == 0)
            return;

        foreach (string playerID in stageData.playerIDs)
        {
            if (!playersInPortal.Contains(playerID))
                return;
        }

        changingRoom = true;
        NextRoom();
    }

    public bool IsPlayerInCombat(CombatInstance playerCombat)
    {
        if (playerCombat == null)
            return false;

        foreach (GameObject entity in spawnedEntities)
        {
            if (entity == null)
                continue;

            EntityAgent agent = entity.GetComponent<EntityAgent>();

            if (agent == null || agent.CurrentTarget == null)
                continue;

            if (agent.CurrentTarget == playerCombat)
                return true;
        }

        return false;
    }

    private void CheckAllPlayersDead()
    {
        if (runEnded || changingRoom)
            return;

        if (stageData == null || stageData.playerIDs == null || stageData.playerIDs.Count == 0)
            return;

        foreach (string playerID in stageData.playerIDs)
        {
            GameObject playerObj = PlayerManager.Instance.GetPlayer(playerID);

            if (playerObj == null)
                return;   // chưa spawn → coi như còn sống

            CombatInstance combat = playerObj.GetComponent<CombatInstance>();

            if (combat == null || combat.state == null)
                return;

            if (combat.state.stats.currentHp > 0f)
                return;
        }

        EndRun(RunEndReason.Lose);
    }
}

[Serializable]
public class StageTriggerConfig
{
    public TriggerData triggerData;
    public int width;
    public int height;
}

[Serializable]
public class StageRoomData
{
    public BiomeData biome;

    public List<StageTriggerConfig> nodes = new();
    public Merchant merchant = new();
}

[Serializable]
public class StageData
{
    public int stageDifficulty = 10;
    public bool hasKeyRoom = true;
    public List<StageRoomData> rooms = new();
    public List<string> playerIDs = new();
}
[Serializable]
public class Merchant
{
    public List<MerchantItem> items = new();

    public readonly int blessingCost = 2000;
    public readonly int artifactCost = 800;
    public readonly int throwingObjectCost = 400;
    public readonly int refreshCost = 100;
    public readonly int upgradeArtifactCost = 500;
    public bool allowRefresh = true;
    public int numberOfRefreshed;
    public int CalculateCost(int baseCost, int roomIndex, float multiplier = 1f)
    {
        return Mathf.RoundToInt(baseCost * (roomIndex + 1) * multiplier);
    }

    public int GetBlessingCost(BlessingData blessing, int roomIndex)
    {
        return CalculateCost(blessingCost, roomIndex, blessing.rarity);
    }

    public int GetThrowingObjectCost(ThrowObj throwingObject, int roomIndex)
    {
        return CalculateCost(throwingObjectCost, roomIndex, throwingObject.rarity);
    }

    public int GetArtifactCost(ArtifactInstance artifact, int roomIndex)
    {
        return CalculateCost(artifactCost, roomIndex, artifact.level + 1);
    }

    public int GetRefreshCost(int roomIndex)
    {
        return CalculateCost(refreshCost, roomIndex, numberOfRefreshed + 1);
    }

    public int GetArtifactUpgradeCost(ArtifactInstance artifact, int roomIndex)
    {
        return CalculateCost(upgradeArtifactCost, roomIndex, artifact.level + 1);
    }
}
[Serializable]
public class MerchantItem
{
    public ItemData item;
    public int quantity;
    public int cost;
    public ArtifactInstance artifact;
    public bool soldOut;

    public MerchantItem(ItemData item, int quantity, int cost, ArtifactInstance artifact = null)
    {
        this.item = item;
        this.quantity = quantity;
        this.cost = cost;
        this.artifact = artifact;
        soldOut = false;
    }
}
public enum MerchantPurchaseResult
{
    Failed,
    Success,
    RequireArtifactReplace
}
[Serializable]
public class RunResult
{
    public string stageID;
    public int stageDifficulty;
    public int totalRooms;
    public int currentRoomIndex;
    public bool hasKeyRoom;

    public int threatLevel;
    public int threatXP;

    public List<PlayerResult> players = new();
    public List<RoomResult> rooms = new();

    public RunEndReason endReason;
    public float runDuration;
    public DateTime endTime;
}

[Serializable]
public class PlayerResult
{
    public string playerID;

    public int level;
    public int currentXP;
    public int currentXPCap;

    public float atk;
    public float maxHp;
    public float currentHp;

    public int tp;

    public List<InventoryItem> inventory = new();
    public List<BlessingInstance> blessings = new();
    public List<ArtifactInstance> artifacts = new();
}

[Serializable]
public class RoomResult
{
    public int index;
    public string biomeID;
    public bool cleared;
}

public enum RunEndReason
{
    Lose,
    Victory,
    PlayerQuit,
    ServerShutdown,
    Debug
}