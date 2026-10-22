
using UnityEngine;

public class MenuFunction : MonoBehaviour
{
    public static MenuFunction Instance { get; private set; }

    private string localPlayerID;
    [SerializeField] private GameObject lobbyPanel;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        LocalPlayerContext context = LocalPlayerContext.Instance;

        if (context == null)
            return;

        localPlayerID = context.PlayerID;
        context.OnPlayerIDChanged += HandlePlayerIDChanged;
    }

    private void OnDestroy()
    {
        if (Instance != this)
            return;

        if (LocalPlayerContext.Instance != null)
            LocalPlayerContext.Instance.OnPlayerIDChanged
                -= HandlePlayerIDChanged;

        Instance = null;
    }

    private void HandlePlayerIDChanged()
    {
        localPlayerID = LocalPlayerContext.Instance.PlayerID;
    }

    // Nút Play Local
    
    public void PlayLocal()
    {
        if (string.IsNullOrEmpty(localPlayerID))
            return;

        LocalPlayerContext context = LocalPlayerContext.Instance;
        LobbyManager manager = LobbyManager.Instance;

        if (context == null || manager == null)
            return;

        // Đang ở trong lobby nào đó thì không tự chuyển sang Solo.
        if (!string.IsNullOrEmpty(context.LobbyID))
        {
            Lobby currentLobby = manager.GetLobby(context.LobbyID);

            if (currentLobby != null)
            {
                GlobalCanvas.Instance.SendErrorLog("You are already in a lobby. Leave the lobby to play solo.",null);
                return;
            }

            // Lobby ID không còn hợp lệ.
            context.SetLobbyID(string.Empty);
        }

        string lobbyID = $"{localPlayerID}_room";

        Lobby lobby = manager.GetLobby(lobbyID);

        if (lobby == null)
            lobby = manager.CreateLobby(lobbyID, localPlayerID);

        if (lobby == null)
            return;

        context.SetLobbyID(lobbyID);
        lobby.SetReady(localPlayerID, true);

        // TODO: Bắt đầu Solo game.
    }


    // Nút Multiplayer ở Main Menu
    public void OpenMultiplayer()
    {
        if (lobbyPanel != null)
            lobbyPanel.SetActive(true);
    }

    // Nút Create Lobby
    
    
    public Lobby CreateMultiplayerLobby(string lobbyID)
    {
        if (string.IsNullOrEmpty(localPlayerID))
        {
            ShowError("Cannot create lobby: Player ID is missing.");
            return null;
        }

        if (LocalPlayerContext.Instance == null)
        {
            ShowError("Cannot create lobby: Player context is missing.");
            return null;
        }

        LobbyManager manager = LobbyManager.Instance;

        if (manager == null)
        {
            ShowError("Cannot create lobby: Lobby manager is unavailable.");
            return null;
        }

        if (!string.IsNullOrEmpty(
            LocalPlayerContext.Instance.LobbyID))
        {
            ShowError("You are already in a lobby. Leave it first.");
            return null;
        }

        Lobby lobby = manager.CreateLobby(lobbyID, localPlayerID);

        if (lobby == null)
        {
            ShowError("Failed to create lobby. The lobby ID may already exist.");
            return null;
        }

        LocalPlayerContext.Instance.SetLobbyID(lobbyID);
        return lobby;
    }



    // Nút Join Lobby
    public void JoinMultiplayerLobby(string lobbyID)
    {
        if (string.IsNullOrEmpty(localPlayerID))
            return;

        LobbyManager manager = LobbyManager.Instance;

        if (manager == null)
            return;

        if (!manager.JoinLobby(lobbyID, localPlayerID))
            return;

        // JoinLobby đã cập nhật LocalPlayerContext.
        // LobbyUI chuyển sang LobbyRoomPanel.
    }
    
    private void ShowError(string message)
    {
        if (GlobalCanvas.Instance != null)
            GlobalCanvas.Instance.SendErrorLog(message, null);
        else
            Debug.LogError(message);
    }

}
