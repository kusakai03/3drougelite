
using System;
using System.Collections.Generic;
using UnityEngine;

public class LobbyManager : MonoBehaviour
{
    public static LobbyManager Instance { get; private set; }

    private readonly Dictionary<string, Lobby> lobbies = new();

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

    // Tạo lobby và đăng ký người tạo làm Host
    public Lobby CreateLobby(string lobbyID, string ownerPlayerID)
    {
        if (string.IsNullOrEmpty(lobbyID) ||
            string.IsNullOrEmpty(ownerPlayerID) ||
            lobbies.ContainsKey(lobbyID) ||
            IsPlayerInAnyLobby(ownerPlayerID))
            return null;

        Lobby lobby = new Lobby(lobbyID, ownerPlayerID);
        lobbies.Add(lobbyID, lobby);

        LocalPlayerContext context = LocalPlayerContext.Instance;

        if (context != null && context.PlayerID == ownerPlayerID)
            context.SetLobbyID(lobbyID);

        return lobby;
    }

    public Lobby GetLobby(string lobbyID)
    {
        if (string.IsNullOrEmpty(lobbyID))
            return null;

        lobbies.TryGetValue(lobbyID, out Lobby lobby);
        return lobby;
    }

    public List<Lobby> GetAllLobbies()
    {
        return new List<Lobby>(lobbies.Values);
    }

    public bool IsPlayerInAnyLobby(string playerID)
    {
        foreach (Lobby lobby in lobbies.Values)
        {
            if (lobby.ContainsPlayer(playerID))
                return true;
        }

        return false;
    }

    // Tham gia một lobby có sẵn
    public bool JoinLobby(string lobbyID, string playerID)
    {
        Lobby lobby = GetLobby(lobbyID);

        if (lobby == null ||
            string.IsNullOrEmpty(playerID) ||
            IsPlayerInAnyLobby(playerID))
            return false;

        if (!lobby.RegisterPlayer(playerID))
            return false;

        LocalPlayerContext context = LocalPlayerContext.Instance;

        if (context != null && context.PlayerID == playerID)
            context.SetLobbyID(lobbyID);

        return true;
    }

    // Đổi Host. Chỉ Host hiện tại mới có quyền đổi.
    public bool SetOwnerLobby(string lobbyID, string requesterPlayerID, string newOwnerPlayerID)
    {
        Lobby lobby = GetLobby(lobbyID);

        if (lobby == null ||
            lobby.OwnerPlayerID != requesterPlayerID)
            return false;

        return lobby.SetOwner(newOwnerPlayerID);
    }

    // Kick một thành viên. Không thể kick chính Host.
    public bool KickPlayer(string lobbyID, string requesterPlayerID, string targetPlayerID)
    {
        Lobby lobby = GetLobby(lobbyID);

        if (lobby == null ||
            lobby.OwnerPlayerID != requesterPlayerID ||
            requesterPlayerID == targetPlayerID)
            return false;

        if (!lobby.RemovePlayer(targetPlayerID))
            return false;

        ClearLocalLobbyIfMatches(lobbyID, targetPlayerID);
        return true;
    }

    // Rời lobby. Nếu Host rời, xóa lobby
    public bool LeaveLobby(string lobbyID, string playerID)
    {
        Lobby lobby = GetLobby(lobbyID);

        if (lobby == null || !lobby.ContainsPlayer(playerID))
            return false;

        if (lobby.OwnerPlayerID == playerID)
        {
            DeleteLobby(lobbyID, playerID);
            return true;
        }

        if (!lobby.RemovePlayer(playerID))
            return false;

        ClearLocalLobbyIfMatches(lobbyID, playerID);
        return true;
    }

    // Chỉ Host được xóa lobby
    public bool DeleteLobby(string lobbyID, string requesterPlayerID)
    {
        Lobby lobby = GetLobby(lobbyID);

        if (lobby == null ||
            lobby.OwnerPlayerID != requesterPlayerID)
            return false;

        List<string> playerIDs = lobby.GetPlayerIDs();

        lobbies.Remove(lobbyID);
        foreach (string playerID in playerIDs)
            ClearLocalLobbyIfMatches(lobbyID, playerID);

        return true;
    }

    // Host yêu cầu bắt đầu game sau khi mọi người Ready.
    public bool StartGame(string lobbyID, string requesterPlayerID)
    {
        Lobby lobby = GetLobby(lobbyID);

        if (lobby == null)
            return false;

        return lobby.StartGame(requesterPlayerID);
    }

    private void ClearLocalLobbyIfMatches(
        string lobbyID,
        string playerID)
    {
        LocalPlayerContext context = LocalPlayerContext.Instance;

        if (context != null &&
            context.PlayerID == playerID &&
            context.LobbyID == lobbyID)
        {
            context.SetLobbyID(string.Empty);
        }
    }
}

[Serializable]
public class Lobby
{
    public string LobbyID { get; }

    public string OwnerPlayerID { get; private set; }

    public bool IsStarted { get; private set; }

    private readonly Dictionary<string, bool> players = new();

    public event Action OnLobbyUpdated;
    public event Action OnStartGame;

    public Lobby(string lobbyID, string ownerPlayerID)
    {
        LobbyID = lobbyID;
        OwnerPlayerID = ownerPlayerID;

        // Chủ phòng cũng là thành viên của lobby.
        players.Add(ownerPlayerID, false);
    }

    public List<string> GetPlayerIDs()
    {
        return new List<string>(players.Keys);
    }

    public bool ContainsPlayer(string playerID)
    {
        return players.ContainsKey(playerID);
    }

    public bool IsPlayerReady(string playerID)
    {
        return players.TryGetValue(playerID, out bool ready) && ready;
    }

    public int GetPlayerCount()
    {
        return players.Count;
    }

    public bool RegisterPlayer(string playerID)
    {
        if (string.IsNullOrEmpty(playerID) ||
            IsStarted ||
            players.ContainsKey(playerID))
            return false;

        players.Add(playerID, false);
        OnLobbyUpdated?.Invoke();

        return true;
    }

    public bool RemovePlayer(string playerID)
    {
        if (!players.ContainsKey(playerID))
            return false;

        // Không được tự xóa Host bằng thao tác RemovePlayer.
        // Host phải rời qua LobbyManager để xóa lobby.
        if (playerID == OwnerPlayerID)
            return false;

        players.Remove(playerID);
        OnLobbyUpdated?.Invoke();

        return true;
    }

    
    public bool SetReady(string playerID, bool ready)
    {
        if (IsStarted || !players.ContainsKey(playerID))
            return false;

        if (players[playerID] == ready)
            return true;

        players[playerID] = ready;
        OnLobbyUpdated?.Invoke();

        TryStartGame();

        return true;
    }

    private void TryStartGame()
    {
        if (IsStarted || !AreAllPlayersReady())
            return;

        IsStarted = true;

        OnLobbyUpdated?.Invoke();
        OnStartGame?.Invoke();
    }


    public bool SetOwner(string newOwnerPlayerID)
    {
        if (IsStarted ||
            !players.ContainsKey(newOwnerPlayerID))
            return false;

        if (OwnerPlayerID == newOwnerPlayerID)
            return true;

        OwnerPlayerID = newOwnerPlayerID;
        OnLobbyUpdated?.Invoke();

        return true;
    }

    public bool AreAllPlayersReady()
    {
        if (players.Count == 0)
            return false;

        foreach (bool ready in players.Values)
        {
            if (!ready)
                return false;
        }

        return true;
    }

    public bool CanStart()
    {
        return !IsStarted && AreAllPlayersReady();
    }

    public bool StartGame(string requesterPlayerID)
    {
        if (requesterPlayerID != OwnerPlayerID || !CanStart())
            return false;

        IsStarted = true;
        OnLobbyUpdated?.Invoke();
        OnStartGame?.Invoke();

        return true;
    }
}