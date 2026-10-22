
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;

public class LobbyUI : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject lobbyPanel;

    [Header("Buttons")]
    [SerializeField] private Button createButton;
    [SerializeField] private Button joinButton;
    [SerializeField] private Button closeUIButton;
    [SerializeField] private Button leaveLobbyButton;
    [SerializeField] private Button readyButton;

    [Header("Player List")]
    [SerializeField] private Transform playerGrid;
    [SerializeField] private LobbyPlayerItemUI playerItemPrefab;

    private Lobby currentLobby;
    private bool isReady;
    private void ShowError(string message)
    {
        if (GlobalCanvas.Instance != null)
        {
            GlobalCanvas.Instance.SendErrorLog(message, null);
        }
        else
        {
            Debug.LogError(message);
        }
    }

    private void Awake()
    {
        createButton.onClick.AddListener(CreateLobby);
        joinButton.onClick.AddListener(JoinLobby);
        closeUIButton.onClick.AddListener(CloseUI);
        leaveLobbyButton.onClick.AddListener(LeaveLobby);
        readyButton.onClick.AddListener(ToggleReady);

        lobbyPanel.SetActive(false);
        RefreshButtons();
    }

    private void OnDestroy()
    {
        if (createButton != null)
            createButton.onClick.RemoveListener(CreateLobby);

        if (joinButton != null)
            joinButton.onClick.RemoveListener(JoinLobby);

        if (closeUIButton != null)
            closeUIButton.onClick.RemoveListener(CloseUI);

        if (leaveLobbyButton != null)
            leaveLobbyButton.onClick.RemoveListener(LeaveLobby);

        if (readyButton != null)
            readyButton.onClick.RemoveListener(ToggleReady);

        UnsubscribeLobbyEvents();
    }

    private void CreateLobby()
    {
        if (MenuFunction.Instance == null)
            return;

        string playerID = LocalPlayerContext.Instance.PlayerID;
        string lobbyID = $"{playerID}_room";

        currentLobby = MenuFunction.Instance.CreateMultiplayerLobby(lobbyID);

        if (currentLobby == null)
            return;

        isReady = false;

        SubscribeLobbyEvents();
        RefreshPlayerList();
        RefreshButtons();
    }

    private void JoinLobby()
    {
        // TODO: Bổ sung sau.
        // Chưa xử lý mã phòng hoặc tham gia lobby.
        Debug.Log("Join Lobby is not implemented yet.");
    }

    private void CloseUI()
    {
        lobbyPanel.SetActive(false);
    }
    public void OpenUI()
    {
        lobbyPanel.SetActive(true);
    }
    private void LeaveLobby()
    {
        if (currentLobby == null ||
            LobbyManager.Instance == null ||
            LocalPlayerContext.Instance == null)
            return;

        string lobbyID = currentLobby.LobbyID;
        string playerID = LocalPlayerContext.Instance.PlayerID;

        UnsubscribeLobbyEvents();

        LobbyManager.Instance.LeaveLobby(lobbyID, playerID);

        currentLobby = null;
        isReady = false;

        ClearPlayerList();
        RefreshButtons();
    }

    
    private void ToggleReady()
    {
        if (currentLobby == null)
        {
            ShowError("You are not in a lobby.");
            return;
        }

        if (LocalPlayerContext.Instance == null)
        {
            ShowError("Player context is unavailable.");
            return;
        }

        string playerID = LocalPlayerContext.Instance.PlayerID;
        bool newReadyState = !isReady;

        if (!currentLobby.SetReady(playerID, newReadyState))
        {
            ShowError("Failed to change Ready status.");
            return;
        }

        isReady = newReadyState;
        RefreshButtons();
        RefreshPlayerList();
    }


    private void SubscribeLobbyEvents()
    {
        if (currentLobby == null)
            return;

        currentLobby.OnLobbyUpdated += OnLobbyUpdated;
    }

    private void UnsubscribeLobbyEvents()
    {
        if (currentLobby == null)
            return;

        currentLobby.OnLobbyUpdated -= OnLobbyUpdated;
    }

    private void OnLobbyUpdated()
    {
        RefreshPlayerList();
        RefreshButtons();
    }

    private void RefreshPlayerList()
    {
        ClearPlayerList();

        if (currentLobby == null ||
            playerGrid == null ||
            playerItemPrefab == null)
            return;

        List<string> playerIDs = currentLobby.GetPlayerIDs();

        foreach (string playerID in playerIDs)
        {
            LobbyPlayerItemUI item = Instantiate(
                playerItemPrefab, playerGrid);
            item.gameObject.SetActive(true);
            item.Setup(
                playerID,
                playerID == currentLobby.OwnerPlayerID,
                currentLobby.IsPlayerReady(playerID)
            );
        }
    }

    private void ClearPlayerList()
    {
        if (playerGrid == null)
            return;

        foreach (Transform child in playerGrid)
            Destroy(child.gameObject);
    }

    private void RefreshButtons()
    {
        bool inLobby = currentLobby != null;

        if (createButton != null)
            createButton.interactable = !inLobby;

        if (joinButton != null)
            joinButton.interactable = !inLobby;

        if (leaveLobbyButton != null)
            leaveLobbyButton.interactable = inLobby;

        if (readyButton != null)
            readyButton.interactable = inLobby;

        if (readyButton != null)
        {
            TextMeshProUGUI label = readyButton.GetComponentInChildren<TextMeshProUGUI>();
            if (label != null){
                label.text = isReady ? "Unready" : "Ready";
            }
        }
    }
}
