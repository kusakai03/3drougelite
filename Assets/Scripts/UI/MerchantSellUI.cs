using UnityEngine;

public class MerchantSellUI : MonoBehaviour
{
    [SerializeField] private ItemSlotUI[] slots;

    private string playerID;
    private MerchantUI merchantUI;
    private ItemSlotUI selectedSlot;

    private void Awake()
    {
        foreach (ItemSlotUI slot in slots)
            slot.OnClicked += OnSlotClicked;
    }

    private void OnDestroy()
    {
        foreach (ItemSlotUI slot in slots)
            slot.OnClicked -= OnSlotClicked;
    }

    public void Open(string playerID, MerchantUI merchantUI)
    {
        this.playerID = playerID;
        this.merchantUI = merchantUI;

        ClearSelection();
        Refresh();
    }

    public void Refresh()
    {
        CharacterData character = StageManager.Instance.GetCharacter(playerID);

        if (character == null)
            return;

        for (int i = 0; i < slots.Length; i++)
        {
            if (i >= character.inventory.Count)
            {
                slots[i].Clear();
                continue;
            }

            InventoryItem inventoryItem = character.inventory[i];
            ItemData data = ItemManager.Instance.GetItem(inventoryItem.itemID);

            if (data == null || data.itemType != ItemType.Currency)
            {
                slots[i].Clear();
                continue;
            }

            slots[i].SetItem(playerID, i, data, inventoryItem.quantity);
        }

        ClearSelection();
    }

    private void OnSlotClicked(ItemSlotUI slot)
    {
        if (slot == null || slot.Item == null)
            return;

        // Click lại item đang chọn -> bán
        if (selectedSlot == slot)
        {
            Sell(slot);
            return;
        }

        // Chọn item khác
        if (selectedSlot != null)
            selectedSlot.SetSelected(false);

        selectedSlot = slot;
        selectedSlot.SetSelected(true);
    }

    private void Sell(ItemSlotUI slot)
    {
        if (!StageManager.Instance.SellInventoryItem(playerID, slot.SlotIndex))
            return;

        UIManager.Instance.HideItemInfo(playerID);

        selectedSlot = null;

        Refresh();
        merchantUI.RefreshUI();
    }

    private void ClearSelection()
    {
        if (selectedSlot != null)
            selectedSlot.SetSelected(false);

        selectedSlot = null;
    }
}