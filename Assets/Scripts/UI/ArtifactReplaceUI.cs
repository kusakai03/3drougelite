using UnityEngine;

public class ArtifactReplaceUI : MonoBehaviour
{
    [SerializeField] private ItemSlotUI[] slots;
    private LootDrop pendingLoot;
    private LootPickup sourcePickup;
    private ItemSlotUI currentSlot;
    private CharacterData character;

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

    private string playerID;

    public void Open(string playerID, LootPickup pickup)
    {
        if (pickup == null || pickup.Loot == null)
            return;

        this.playerID = playerID;
        pendingLoot = pickup.Loot;
        sourcePickup = pickup;

        ResetSelection();
        Refresh();
    }
    public void Open(string playerID, LootDrop loot)
    {
        if (loot == null)
            return;

        this.playerID = playerID;
        pendingLoot = loot;
        sourcePickup = null;

        ResetSelection();
        Refresh();
    }
    private void Refresh()
    {
        character = StageManager.Instance.GetCharacter(playerID);

        if (character == null)
            return;

        for (int i = 0; i < slots.Length; i++)
        {
            if (i >= character.artifacts.Count)
            {
                slots[i].Clear();
                continue;
            }

            ArtifactInstance artifact = character.artifacts[i];
            ArtifactData data = ItemManager.Instance.GetItem<ArtifactData>(artifact.artifactID);

            slots[i].SetItem(playerID, i, data, 1, artifact);
        }
    }

    private void OnSlotClicked(ItemSlotUI slot)
    {
        if (pendingLoot == null)
        {
            UIManager.Instance.HideItemInfo(playerID);
            UIManager.Instance.CloseUI(playerID);
            return;
        }

        if (currentSlot != slot)
        {
            SelectSlot(slot);
            return;
        }

        ConfirmReplace(slot);
    }

    private void SelectSlot(ItemSlotUI slot)
    {
        if (currentSlot != null)
            currentSlot.SetSelected(false);

        currentSlot = slot;
        currentSlot.SetSelected(true);
    }

    private void ConfirmReplace(ItemSlotUI slot)
    {
        if (pendingLoot == null || pendingLoot.artifact == null)
            return;

        ArtifactInstance newArtifact = pendingLoot.artifact;

        ArtifactInstance oldArtifact = StageManager.Instance.ReplaceArtifact(playerID, slot.SlotIndex, newArtifact);

        if (oldArtifact == null)
            return;

        ArtifactData oldData = ItemManager.Instance.GetItem<ArtifactData>(oldArtifact.artifactID);

        StageManager.Instance.DropItem(oldData, 1, PlayerManager.Instance.LocalPlayer.transform.position, oldArtifact);

        if (sourcePickup != null)
            Destroy(sourcePickup.gameObject);

        pendingLoot = null;
        sourcePickup = null;

        ResetSelection();
        UIManager.Instance.HideItemInfo(playerID);
        UIManager.Instance.CloseUI(playerID);
    }
    private void ResetSelection()
    {
        if (currentSlot != null)
            currentSlot.SetSelected(false);

        currentSlot = null;
    }
}