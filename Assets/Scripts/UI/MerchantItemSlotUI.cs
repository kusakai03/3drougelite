using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class MerchantItemSlotUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private Image icon;
    [SerializeField] private TMP_Text quantityText;
    [SerializeField] private TMP_Text costText;
    [SerializeField] private GameObject soldOutPanel;
    [SerializeField] private Button buyButton;

    private MerchantItem merchantItem;
    private MerchantUI merchantUI;
    private string playerID;

    public void Setup(string playerID, MerchantItem merchantItem, MerchantUI merchantUI)
    {
        this.playerID = playerID;
        this.merchantItem = merchantItem;
        this.merchantUI = merchantUI;

        icon.sprite = merchantItem.item.icon;

        quantityText.text = merchantItem.quantity > 1
            ? $"x{merchantItem.quantity}"
            : "";

        costText.text = $"{merchantItem.cost} TP";

        soldOutPanel.SetActive(merchantItem.soldOut);
        buyButton.interactable = !merchantItem.soldOut;

        buyButton.onClick.RemoveAllListeners();
        buyButton.onClick.AddListener(Buy);
    }

    private void Buy()
    {
        if (merchantItem == null)
            return;

        merchantUI.BuyItem(merchantItem);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (merchantItem == null || merchantItem.item == null)
            return;

        UIManager.Instance.ShowItemInfo(playerID, merchantItem.item, merchantItem.quantity, merchantItem.artifact, eventData.position);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        UIManager.Instance.HideItemInfo(playerID);
    }
}