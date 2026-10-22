using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LootPickupUI : MonoBehaviour
{
    [SerializeField] private GameObject panel;
    [SerializeField] private RectTransform panelRect;
    [SerializeField] private Image icon;
    [SerializeField] private TMP_Text itemName;
    [SerializeField] private TMP_Text quantityText;
    [SerializeField] private TMP_Text artifactStatsText;

    public void Show(LootPickup pickup)
    {
        if (pickup == null || pickup.Loot == null || pickup.Loot.item == null)
        {
            Hide();
            return;
        }

        Show(pickup.Loot.item, pickup.Loot.quantity, pickup.Loot.artifact);
    }

    public void Show(ItemData item, int quantity = 1, ArtifactInstance artifact = null)
    {
        if (item == null)
        {
            Hide();
            return;
        }

        panel.SetActive(true);

        icon.sprite = item.icon;
        itemName.text = item.itemName + (artifact != null ? $" (Lv.{artifact.level})" : "");

        if (artifact != null)
        {
            quantityText.gameObject.SetActive(false);
            artifactStatsText.gameObject.SetActive(true);

            ShowArtifactStats(item, artifact);
        }
        else
        {
            quantityText.gameObject.SetActive(true);
            artifactStatsText.gameObject.SetActive(false);

            quantityText.text = $"{quantity}";
        }
    }

    public void SetPosition(Vector2 screenPosition)
    {
        panelRect.position = screenPosition;
    }

    private void ShowArtifactStats(ItemData item, ArtifactInstance artifact)
    {
        ArtifactData data = item as ArtifactData;

        if (data == null)
        {
            artifactStatsText.text = "";
            return;
        }

        float effectiveness = ArtifactManager.Instance.GetEffectivenessMultiplier(artifact);

        string text = "";
        text += FormatStat(data.mainStat, effectiveness);

        foreach (ArtifactStat stat in artifact.subStats)
            text += "\n" + FormatStat(stat, effectiveness);

        artifactStatsText.text = text;
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

    public void Hide()
    {
        panel.SetActive(false);
    }
}