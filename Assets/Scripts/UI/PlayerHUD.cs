using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerHUD : MonoBehaviour
{
    
    private string playerID;
    private GameObject playerObject;

    [Header("HUD UI")]
    [SerializeField] private ItemSlotUI currentThrowSlot;
    [SerializeField] private GameObject aim;
    [SerializeField] private GameObject PHealthBarGrid;
    [SerializeField] private GameObject PHealthBarPrefab;

    private readonly List<Slider> playerHealthBars = new();
    private readonly Dictionary<string, Slider> healthBarsByID = new();

    private bool healthBarsInitialized;

    private void Start()
    {
        if (StageManager.Instance != null)
            StageManager.Instance.OnCombatChanged += UpdateHealthBar;

        if (PlayerManager.Instance != null)
        {
            PlayerManager.Instance.OnAllPlayersSpawned += InitializeHealthBars;

            if (PlayerManager.Instance.IsSpawnComplete)
                InitializeHealthBars();
        }

        if (InputManager.Instance != null)
            InputManager.Instance.OnScroll += HandleScroll;
    }

    private void OnDisable()
    {
        if (StageManager.Instance != null)
            StageManager.Instance.OnCombatChanged -= UpdateHealthBar;

        if (PlayerManager.Instance != null)
            PlayerManager.Instance.OnAllPlayersSpawned -= InitializeHealthBars;

        if (InputManager.Instance != null)
            InputManager.Instance.OnScroll -= HandleScroll;

        if (playerObject != null)
        {
            PlayerThrow playerThrow =
                playerObject.GetComponent<PlayerThrow>();

            if (playerThrow != null)
                playerThrow.OnCurrentThrowChanged -= RefreshCurrentThrow;
        }
    }

    private void InitializeHealthBars()
    {
        if (healthBarsInitialized)
            return;

        PlayerManager manager = PlayerManager.Instance;

        if (manager == null || !manager.IsSpawnComplete)
            return;

        healthBarsInitialized = true;

        playerHealthBars.Clear();
        healthBarsByID.Clear();

        foreach (Transform child in PHealthBarGrid.transform)
            Destroy(child.gameObject);

        foreach (CharacterData character in StageManager.Instance.characters)
        {
            GameObject player =
                manager.GetPlayer(character.playerID);

            if (player == null)
                continue;

            CombatInstance combat =
                player.GetComponent<CombatInstance>();

            if (combat == null)
                continue;

            GameObject bar = Instantiate(
                PHealthBarPrefab,
                PHealthBarGrid.transform
            );
            bar.SetActive(true);
            Slider slider = bar.GetComponentInChildren<Slider>(true);

            if (slider == null)
            {
                Debug.LogError(
                    $"Health Bar prefab thiếu Slider: {bar.name}"
                );

                Destroy(bar);
                continue;
            }

            string id = combat.state.instanceID;

            playerHealthBars.Add(slider);
            healthBarsByID[id] = slider;

            // Hiển thị máu hiện tại ngay sau khi tạo.
            UpdateHealthBar(combat.state);
        }
    }

    private void UpdateHealthBar(CombatState state)
    {
        if (state == null || state.stats == null)
            return;

        if (!healthBarsByID.TryGetValue(
            state.instanceID, out Slider slider))
            return;
        slider.maxValue = state.stats.hp;
        slider.value = state.stats.currentHp;
    }
    public void RegisterPlayer(string playerID, GameObject playerObject)
    {
        this.playerID = playerID;
        this.playerObject = playerObject;
        var playerThrow = playerObject.GetComponent<PlayerThrow>();
        playerThrow.OnCurrentThrowChanged -= RefreshCurrentThrow;
        playerThrow.OnCurrentThrowChanged += RefreshCurrentThrow;  
        RefreshCurrentThrow(playerID);
    }
    private void HandleScroll(float obj)
    {
        RefreshCurrentThrow(playerID);
    }

    public void RefreshCurrentThrow(string playerID)
    {
        CharacterData character = StageManager.Instance.GetCharacter(playerID);
        bool hasObject = true;
        if (character == null)
            return;

        string itemID = character.currentThrowObjectID;

        if (string.IsNullOrEmpty(itemID))
        {
            itemID = "throw_rock";
            hasObject = false;
        }

        ThrowObj throwObj = ItemManager.Instance.GetItem<ThrowObj>(itemID);

        if (throwObj == null)
            return;

        int quantity = 0;

        if (!hasObject)
        {
            quantity = -1;
        }
        else
        {
            InventoryItem inventoryItem = character.inventory.Find(x => x.itemID == itemID);

            if (inventoryItem != null)
                quantity = inventoryItem.quantity;
        }

        currentThrowSlot.SetItem(playerID, -1, throwObj, quantity, isInfinite: !hasObject);
    }
    private void Update()
    {
        aim.SetActive(InputManager.Instance.AimMode);
    }
}
