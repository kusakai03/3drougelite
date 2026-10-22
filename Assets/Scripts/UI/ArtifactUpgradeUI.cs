using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ArtifactUpgradeUI : MonoBehaviour
{
    [SerializeField] private ItemSlotUI[] slots;

    [Header("Info")]
    [SerializeField] private TMP_Text artifactNameText;
    [SerializeField] private TMP_Text levelText;
    [SerializeField] private TMP_Text costText;
    [SerializeField] private TMP_Text statsText;
    [SerializeField] private Button upgradeButton;
    [SerializeField] private MerchantUI merchantUI;

    private string playerID;
    private StageRoomData room;

    private ItemSlotUI selectedSlot;
    private ArtifactInstance selectedArtifact;

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

    public void Open(string playerID, StageRoomData room)
    {
        this.playerID = playerID;
        this.room = room;

        ClearSelection();
        Refresh();
    }

    private void Refresh()
    {
        CharacterData character = StageManager.Instance.GetCharacter(playerID);

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

        RefreshSelected();
    }

    private void OnSlotClicked(ItemSlotUI slot)
    {
        if (slot == null || slot.Artifact == null)
            return;

        if (selectedSlot != null)
            selectedSlot.SetSelected(false);

        selectedSlot = slot;
        selectedArtifact = slot.Artifact;

        selectedSlot.SetSelected(true);

        RefreshSelected();
    }

    private void RefreshSelected()
    {
        if (selectedArtifact == null)
        {
            artifactNameText.text = "";
            levelText.text = "";
            statsText.text = "";
            costText.text = "";
            upgradeButton.interactable = false;
            return;
        }

        ArtifactData data = ItemManager.Instance.GetItem<ArtifactData>(selectedArtifact.artifactID);

        if (data == null)
            return;

        artifactNameText.text = data.itemName;
        statsText.text = BuildArtifactStats(data, selectedArtifact);

        if (selectedArtifact.level >= 3)
        {
            levelText.text = "Lv.3 MAX";
            costText.text = "MAX";
            upgradeButton.interactable = false;
            return;
        }

        int cost = room.merchant.GetArtifactUpgradeCost(selectedArtifact, StageManager.Instance.currentRoomIndex);
        CharacterData character = StageManager.Instance.GetCharacter(playerID);

        levelText.text = $"Lv.{selectedArtifact.level} → Lv.{selectedArtifact.level + 1}";
        costText.text = $"{cost} TP";
        upgradeButton.interactable = character != null && character.tp >= cost;
    }
    private string BuildArtifactStats(ArtifactData data, ArtifactInstance artifact)
    {
        float effectiveness = ArtifactManager.Instance.GetEffectivenessMultiplier(artifact);

        string text = "";

        text += FormatStat(data.mainStat, effectiveness);

        foreach (ArtifactStat stat in artifact.subStats)
            text += "\n" + FormatStat(stat, effectiveness);

        return text;
    }

    private string FormatStat(ArtifactStat stat, float effectiveness)
    {
        float value = stat.value * effectiveness;

        switch (stat.modifierType)
        {
            case ArtifactModifierType.Flat:
                return $"{stat.statType} {FormatSigned(value)}";

            case ArtifactModifierType.Percent:
                return $"{stat.statType} {FormatSigned(value)}%";

            case ArtifactModifierType.Multiplier:
                return $"{stat.statType} x{value:0.##}";

            default:
                return "";
        }
    }

    private string FormatSigned(float value)
    {
        return value >= 0f ? $"+{value:0.##}" : $"{value:0.##}";
    }
    public void Upgrade()
    {
        if (selectedArtifact == null)
            return;

        if (!StageManager.Instance.UpgradeArtifact(playerID, room, selectedArtifact))
            return;

        Refresh();
        merchantUI.RefreshUI();
    }

    private void ClearSelection()
    {
        if (selectedSlot != null)
            selectedSlot.SetSelected(false);

        selectedSlot = null;
        selectedArtifact = null;
    }
}