
using System;
using UnityEngine;

public class LocalPlayerContext : MonoBehaviour
{
    public static LocalPlayerContext Instance { get; private set; }

    public event Action OnPlayerIDChanged;
    public event Action OnLobbyIDChanged;

    public string PlayerID { get; private set; }
    public string LobbyID { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        SetPlayerID("player_1");
    }

    public void SetPlayerID(string playerID)
    {
        PlayerID = playerID;
        OnPlayerIDChanged?.Invoke();
    }

    public void SetLobbyID(string lobbyID)
    {
        if (LobbyID == lobbyID)
            return;

        LobbyID = lobbyID;
        OnLobbyIDChanged?.Invoke();
    }
}
