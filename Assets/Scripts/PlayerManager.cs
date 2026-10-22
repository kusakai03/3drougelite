using System.Collections.Generic;
using System;
using UnityEngine;

public class PlayerManager : MonoBehaviour
{
    public static PlayerManager Instance { get; private set; }

    [SerializeField] private GameObject playerPrefab;

    public GameObject LocalPlayer { get; private set; }

    public event Action<GameObject> OnLocalPlayerSpawned;
    private readonly Dictionary<string, GameObject> players = new();
    
    public event Action OnAllPlayersSpawned;
    public bool IsSpawnComplete { get; private set; }

    private void Awake()
    {
        Instance = this;
        IsSpawnComplete = false;
    }

    private void OnEnable()
    {
        StageManager.Instance.OnSpawnPlayers += SpawnPlayers;
    }

    private void OnDisable()
    {
        if (StageManager.Instance != null)
            StageManager.Instance.OnSpawnPlayers -= SpawnPlayers;
    }

    private void SpawnPlayers(MapNode node, List<string> playerIDs)
    {
        IsSpawnComplete = false;

        foreach (string playerID in playerIDs)
        {
            if (players.ContainsKey(playerID))
                continue;

            SpawnPlayer(node, playerID);
        }

        IsSpawnComplete = true;
        OnAllPlayersSpawned?.Invoke();
    }

    private void SpawnPlayer(MapNode node, string playerID)
    {
        CharacterData character = StageManager.Instance.GetCharacter(playerID);

        if (character == null)
        {
            Debug.LogError($"Character not found: {playerID}");
            return;
        }

        Vector3 position = new Vector3(node.center.x, 1f, node.center.y);

        GameObject obj = Instantiate(playerPrefab, position, Quaternion.identity);

        PlayerInstance instance = obj.GetComponent<PlayerInstance>();

        if (instance == null)
            instance = obj.AddComponent<PlayerInstance>();

        instance.Initialize(playerID);
        CombatInstance combatInstance = obj.GetComponent<CombatInstance>();
        if (!combatInstance) combatInstance = obj.AddComponent<CombatInstance>();
        combatInstance.Initialize(
            playerID, 
        "player",
        character.combat);
        players.Add(playerID, obj);

        if (playerID == LocalPlayerContext.Instance.PlayerID)
        {
            LocalPlayer = obj;
            OnLocalPlayerSpawned?.Invoke(obj);
        }
    }

    public GameObject GetPlayer(string playerID)
    {
        players.TryGetValue(playerID, out GameObject obj);
        return obj;
    }
}