using UnityEngine;

public class BlessingUIManager : MonoBehaviour
{
    [SerializeField] private GameObject blessingPanel;
    [SerializeField] private GameObject pendingIcon;
    [SerializeField] private Transform content;
    [SerializeField] private BlessingCardUI cardPrefab;
    private BlessingData selectedBlessing;
    private BlessingCardUI selectedCard;
    private string localPlayerID;

    private void Start()
    {
        localPlayerID = LocalPlayerContext.Instance.PlayerID;


        blessingPanel.SetActive(false);

        UpdatePendingIcon();
    }

    private void ShowCurrentBlessingGroup(CharacterData character)
    {
        foreach (Transform child in content)
            Destroy(child.gameObject);

        BlessingChoiceGroup group = character.queueBlessings[0];

        foreach (BlessingData blessing in group.choices)
        {
            BlessingCardUI card = Instantiate(cardPrefab, content);
            card.gameObject.SetActive(true);
            card.Setup(blessing, this);
        }
    }

    public void UpdatePendingIcon()
    {
        CharacterData character = StageManager.Instance.GetCharacter(localPlayerID);

        bool hasPending = character != null && character.queueBlessings.Count > 0;

        pendingIcon.SetActive(hasPending);
    }

    public void SelectBlessing(BlessingData blessing, BlessingCardUI card)
    {
        if (selectedCard != null)
            selectedCard.SetSelected(false);

        selectedBlessing = blessing;
        selectedCard = card;

        selectedCard.SetSelected(true);
    }

    public void ConfirmBlessing()
    {
        if (selectedBlessing == null)
            return;

        CharacterData character = StageManager.Instance.GetCharacter(localPlayerID);

        if (character == null || character.queueBlessings.Count == 0)
            return;

        StageManager.Instance.SelectBlessing(localPlayerID, selectedBlessing);

        selectedBlessing = null;

        if (selectedCard != null)
        {
            selectedCard.SetSelected(false);
            selectedCard = null;
        }

        if (character.queueBlessings.Count > 0)
        {
            ShowCurrentBlessingGroup(character);
        }
        else
        {
            UIManager.Instance.CloseBlessingUI(localPlayerID);
        }

        UpdatePendingIcon();
    }
    public bool CanOpen(string playerID)
    {
        CharacterData character = StageManager.Instance.GetCharacter(playerID);

        return character != null && character.queueBlessings.Count > 0;
    }

    public void Open(string playerID)
    {
        localPlayerID = playerID;

        CharacterData character = StageManager.Instance.GetCharacter(localPlayerID);

        if (character == null || character.queueBlessings.Count == 0)
            return;

        ShowCurrentBlessingGroup(character);
    }
}