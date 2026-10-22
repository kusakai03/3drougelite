using UnityEngine;

public class PlayerInstance : MonoBehaviour
{
    public string PlayerID { get; private set; }

    public void Initialize(string playerID)
    {
        PlayerID = playerID;
    }
}