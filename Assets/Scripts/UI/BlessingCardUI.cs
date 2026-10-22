using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BlessingCardUI : MonoBehaviour
{
    [SerializeField] private TMP_Text indexText;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private Button button;
    [SerializeField] private GameObject selected;

    private BlessingData blessing;
    private BlessingUIManager manager;

    public void Setup(BlessingData data, BlessingUIManager uiManager)
    {
        blessing = data;
        manager = uiManager;

        indexText?.gameObject.SetActive(false);

        nameText.text = data.itemName;
        descriptionText.text = data.description;

        selected?.SetActive(false);

        button.interactable = true;
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(Select);
    }

    public void Setup(int index, BlessingInstance instance)
    {
        blessing = instance.data;
        manager = null;
        if (indexText != null)
        {
            indexText.gameObject.SetActive(true);
            indexText.text = $"[{index}]";
        }

        nameText.text = instance.data.itemName;
        descriptionText.text = instance.data.description;

        button.interactable = false;
        button.onClick.RemoveAllListeners();
    }

    private void Select()
    {
        if (manager == null || blessing == null)
            return;

        manager.SelectBlessing(blessing, this);
    }
    public void SetSelected(bool value)
    {
        selected?.SetActive(value);
    }
}