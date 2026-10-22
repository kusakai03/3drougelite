using TMPro;
using UnityEngine;

public class StageDataUI : MonoBehaviour
{
    [SerializeField] private TMP_Text roomText;
    [SerializeField] private TMP_Text threatLevelText;
    [SerializeField] private TMP_Text threatXPText;
    [SerializeField] private TMP_Text playerLevelText;
    [SerializeField] private TMP_Text playerXPText;

    private string localPlayerID;

    private void Start()
    {
        localPlayerID = LocalPlayerContext.Instance.PlayerID;

        InvokeRepeating(nameof(Refresh), 0f, 0.1f);
    }

    private void Refresh()
    {
        StageManager stageManager = StageManager.Instance;

        if (stageManager == null)
            return;

        roomText.text = $"Room {stageManager.currentRoomIndex + 1} / 3";

        threatLevelText.text = $"Threat {stageManager.threatLevel} / 10";
        threatXPText.text = $"{stageManager.threatXP} / {stageManager.threatXPCap}";

        CharacterData character = stageManager.GetCharacter(localPlayerID);

        if (character == null)
            return;

        playerLevelText.text = $"Lv. {character.currentLevel}";
        playerXPText.text = $"{character.currentXP} / {character.currentXPCap}";
    }
}