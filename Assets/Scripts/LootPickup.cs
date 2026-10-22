using UnityEngine;

public class LootPickup : MonoBehaviour
{
    public LootDrop Loot { get; private set; }
    [SerializeField] private Renderer glowRenderer;

    private MaterialPropertyBlock propertyBlock;
    public void Initialize(LootDrop loot)
    {
        Loot = loot;

        UpdateGlowColor();
    }
    private void UpdateGlowColor()
    {
        if (Loot == null || Loot.item == null || glowRenderer == null)
            return;

        Color glowColor;

        if (Loot.artifact != null)
            glowColor = GetArtifactLevelColor(Loot.artifact.level);
        else
            glowColor = GetRarityColor(Loot.item.rarity);

        propertyBlock ??= new MaterialPropertyBlock();

        glowRenderer.GetPropertyBlock(propertyBlock);
        propertyBlock.SetColor("GlowColor", glowColor);
        glowRenderer.SetPropertyBlock(propertyBlock);
    }
    private Color GetRarityColor(int rarity)
    {
        return rarity switch
        {
            1 => Color.white,
            2 => new Color(0.3f, 1f, 0.3f),
            3 => new Color(0.3f, 0.6f, 1f),
            4 => new Color(0.7f, 0.3f, 1f),
            5 => new Color(1f, 0.8f, 0.2f),
            _ => Color.white
        };
    }
    private Color GetArtifactLevelColor(int level)
    {
        return level switch
        {
            0 => Color.white,
            1 => new Color(0.3f, 0.6f, 1f),
            2 => new Color(0.7f, 0.3f, 1f),
            3 => new Color(1f, 0.8f, 0.2f),
            _ => Color.white
        };
    }
}
