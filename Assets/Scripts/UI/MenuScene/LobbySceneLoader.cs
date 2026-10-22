
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class LobbySceneLoader : MonoBehaviour
{
    private Lobby currentLobby;
    private bool isLoading;

    private void Start()
    {
        if (LocalPlayerContext.Instance == null)
            return;

        LocalPlayerContext.Instance.OnLobbyIDChanged += SubscribeToLobby;
        SubscribeToLobby();
    }

    private void SubscribeToLobby()
    {
        LocalPlayerContext context = LocalPlayerContext.Instance;

        if (context == null ||
            string.IsNullOrEmpty(context.LobbyID))
            return;

        Lobby lobby = LobbyManager.Instance.GetLobby(context.LobbyID);

        if (lobby == null || currentLobby == lobby)
            return;

        if (currentLobby != null)
            currentLobby.OnStartGame -= HandleStartGame;

        currentLobby = lobby;
        currentLobby.OnStartGame += HandleStartGame;
    }

    private void HandleStartGame()
    {
        if (isLoading)
            return;

        isLoading = true;
        SceneManager.LoadScene("PlayScene");
    }

    private void OnDisable()
    {
        if (LocalPlayerContext.Instance != null)
            LocalPlayerContext.Instance.OnLobbyIDChanged -= SubscribeToLobby;

        if (currentLobby != null)
        {
            currentLobby.OnStartGame -= HandleStartGame;
            currentLobby = null;
        }
    }
}
