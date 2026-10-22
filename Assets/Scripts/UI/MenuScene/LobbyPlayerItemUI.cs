
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LobbyPlayerItemUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI playerNameText;
    [SerializeField] private TextMeshProUGUI hostText;
    [SerializeField] private TextMeshProUGUI readyText;

    public void Setup(string playerID, bool isHost, bool isReady)
    {
        playerNameText.text = playerID;
        hostText.text = isHost ? "Host" : "";
        readyText.text = isReady ? "Ready" : "Not Ready";
        readyText.color = isReady ? Color.green : Color.red;
    }
}
