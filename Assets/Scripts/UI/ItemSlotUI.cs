using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

public class ItemSlotUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [SerializeField] private Image icon;
    [SerializeField] private TextMeshProUGUI quantityText;
    [SerializeField] private GameObject selected;

    public event Action<ItemSlotUI> OnClicked;

    public string PlayerID { get; private set; }
    public int SlotIndex { get; private set; }
    public ItemData Item { get; private set; }
    public int Quantity { get; private set; }
    public ArtifactInstance Artifact { get; private set; }

    public void SetItem(string playerID, int slotIndex, ItemData item, int quantity = 1, ArtifactInstance artifact = null, bool isInfinite = false)
    {
        PlayerID = playerID;
        SlotIndex = slotIndex;
        Item = item;
        Quantity = quantity;
        Artifact = artifact;

        if (item == null)
        {
            Clear();
            return;
        }

        icon.sprite = item.icon;
        icon.gameObject.SetActive(true);
        quantityText.text = isInfinite? "∞" : quantity >= 1 ? $"x{quantity}" : string.Empty;
    }

    public void Clear()
    {
        Item = null;
        Quantity = 0;
        Artifact = null;

        icon.sprite = null;
        icon.gameObject.SetActive(false);

        SetSelected(false);
    }

    public void SetSelected(bool value)
    {
        if (selected != null)
            selected.SetActive(value);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (Item == null)
            return;

        UIManager.Instance.ShowItemInfo(PlayerID, Item, Quantity, Artifact, eventData.position);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        UIManager.Instance.HideItemInfo(PlayerID);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (Item == null)
            return;

        OnClicked?.Invoke(this);
    }
}