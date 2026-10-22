using UnityEngine;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [SerializeField] private LootPickupUI lootPickupUI;
    [SerializeField] private ArtifactReplaceUI artifactReplaceUI;
    [SerializeField] private PlayerDataUI playerDataUI;
    [SerializeField] private MerchantUI merchantUI;
    [SerializeField] private BlessingUIManager blessingUIManager;
    [SerializeField] private PlayerHUD playerHUD;
    private PlayerMovement lootScanner;
    private GameObject currentUI;
    public bool HasOpenUI => currentUI != null;
    private string localPlayerID;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        PlayerManager.Instance.OnLocalPlayerSpawned += OnLocalPlayerSpawned;
        InputManager.Instance.OnEscape += OnEscape;
        InputManager.Instance.OnBlessingMenu += OnBlessingMenu;

        if (PlayerManager.Instance.LocalPlayer != null)
            OnLocalPlayerSpawned(PlayerManager.Instance.LocalPlayer);

        lootPickupUI.Hide();
        artifactReplaceUI.gameObject.SetActive(false);
        playerDataUI.gameObject.SetActive(false);
        merchantUI.gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        if (PlayerManager.Instance != null)
            PlayerManager.Instance.OnLocalPlayerSpawned -= OnLocalPlayerSpawned;

        if (InputManager.Instance != null)
            InputManager.Instance.OnEscape -= OnEscape;
        if (InputManager.Instance != null)
            InputManager.Instance.OnBlessingMenu -= OnBlessingMenu;

        UnregisterLootScanner();
    }

    private void OnBlessingMenu()
    {
        if (currentUI == blessingUIManager.gameObject)
        {
            CloseCurrentUI();
            return;
        }

        if (currentUI != null)
            return;

        OpenBlessingUI(localPlayerID);
    }
    
    private void OnLocalPlayerSpawned(GameObject player)
    {
        UnregisterLootScanner();

        CombatInstance combat = player.GetComponent<CombatInstance>();

        if (combat == null)
            return;

        localPlayerID = combat.state.instanceID;

        lootScanner = player.GetComponent<PlayerMovement>();

        if (lootScanner != null)
            lootScanner.OnNearestPickupChanged += OnNearestPickupChanged;
        playerHUD.RegisterPlayer(localPlayerID, player);
    }

    private void UnregisterLootScanner()
    {
        if (lootScanner == null)
            return;

        lootScanner.OnNearestPickupChanged -= OnNearestPickupChanged;
        lootScanner = null;
    }

    private bool IsLocalPlayer(string playerID)
    {
        return !string.IsNullOrEmpty(localPlayerID) && playerID == localPlayerID;
    }

    private void OnNearestPickupChanged(string playerID, LootPickup pickup)
    {
        if (!IsLocalPlayer(playerID))
            return;

        if (pickup == null)
        {
            lootPickupUI.Hide();

            if (currentUI == artifactReplaceUI.gameObject)
                CloseUI(playerID);

            return;
        }

        lootPickupUI.Show(pickup);
    }

    public void OpenArtifactReplaceUI(string playerID, LootPickup pickup)
    {
        if (!IsLocalPlayer(playerID))
            return;

        if (pickup == null || pickup.Loot?.artifact == null)
            return;

        artifactReplaceUI.Open(playerID, pickup);
        OpenUI(playerID, artifactReplaceUI.gameObject);
    }
    public void OpenArtifactReplaceUI(string playerID, LootDrop loot)
    {
        if (!IsLocalPlayer(playerID))
            return;

        if (loot == null || loot.artifact == null)
            return;

        artifactReplaceUI.Open(playerID, loot);
        OpenUI(playerID, artifactReplaceUI.gameObject);
    }
    public void ShowItemInfo(string playerID, ItemData item, int quantity, ArtifactInstance artifact, Vector2 position)
    {
        if (!IsLocalPlayer(playerID))
            return;

        lootPickupUI.Show(item, quantity, artifact);
        lootPickupUI.SetPosition(position);
    }

    public void HideItemInfo(string playerID)
    {
        if (!IsLocalPlayer(playerID))
            return;

        lootPickupUI.Hide();
    }

    public void OpenMerchantUI(string playerID, StageRoomData room)
    {
        if (!IsLocalPlayer(playerID))
            return;

        if (room == null || room.merchant == null)
            return;

        merchantUI.Open(playerID, room);
        OpenUI(playerID, merchantUI.gameObject);
    }

    public void CloseMerchantUI(string playerID)
    {
        if (!IsLocalPlayer(playerID))
            return;

        if (currentUI != merchantUI.gameObject)
            return;

        HideItemInfo(playerID);
        CloseUI(playerID);
    }


    public void OpenUI(string playerID, GameObject ui)
    {
        if (!IsLocalPlayer(playerID) || ui == null)
            return;

        if (currentUI != null && currentUI != ui)
            currentUI.SetActive(false);

        currentUI = ui;
        currentUI.SetActive(true);
    }

    public void CloseUI(string playerID)
    {
        if (!IsLocalPlayer(playerID))
            return;

        CloseCurrentUI();
    }

    public void CloseCurrentUI()
    {
        if (currentUI == null)
            return;

        currentUI.SetActive(false);
        currentUI = null;
    }


    private void OnEscape()
    {
        if (currentUI != null)
        {
            CloseCurrentUI();
            return;
        }
        if (lootScanner == null)
        return;
        OpenUI(localPlayerID, playerDataUI.gameObject);
        playerDataUI.Open(localPlayerID, lootScanner.gameObject);
    }

    public bool IsUIOpen(string playerID)
    {
        if (!IsLocalPlayer(playerID))
            return false;

        return currentUI != null;
    }
    public void OpenBlessingUI(string playerID)
    {
        if (!IsLocalPlayer(playerID))
            return;

        if (!blessingUIManager.CanOpen(playerID))
            return;

        OpenUI(playerID, blessingUIManager.gameObject);
        blessingUIManager.Open(playerID);
    }

    public void CloseBlessingUI(string playerID)
    {
        if (!IsLocalPlayer(playerID))
            return;

        if (currentUI != blessingUIManager.gameObject)
            return;

        CloseUI(playerID);
    }
}