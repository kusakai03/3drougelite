using TMPro;
using UnityEngine;

public class InventoryTabUI : MonoBehaviour
{
    [Header("Inventory")]
    [SerializeField] private ItemSlotUI[] inventorySlots;

    [Header("Artifacts")]
    [SerializeField] private ItemSlotUI[] artifactSlots;

    [Header("Current Throw")]
    [SerializeField] private ItemSlotUI currentThrowSlot;

    [Header("Stats")]
    [SerializeField] private TMP_Text levelValue;
    [SerializeField] private TMP_Text expValue;
    [SerializeField] private TMP_Text hpValue;
    [SerializeField] private TMP_Text shieldValue;
    [SerializeField] private TMP_Text damageValue;
    [SerializeField] private TMP_Text throwSpeedValue;
    [SerializeField] private TMP_Text throwPowerValue;
    [SerializeField] private TMP_Text throwDelayValue;
    [SerializeField] private TMP_Text accuracyValue;

    private string playerID;

    private CombatInstance combatInstance;

    public void Show(string playerID, CombatInstance combatInstance)
    {
        this.playerID = playerID;
        this.combatInstance = combatInstance;
        Refresh();
    }

    public void Refresh()
    {
        CharacterData character = StageManager.Instance.GetCharacter(playerID);

        if (character == null)
            return;

        RefreshInventory(character);
        RefreshArtifacts(character);
        RefreshCurrentThrow(character);
        RefreshStats(character);
    }

    private void RefreshInventory(CharacterData character)
    {
        for (int i = 0; i < inventorySlots.Length; i++)
        {
            if (i >= character.inventory.Count)
            {
                inventorySlots[i].Clear();
                continue;
            }

            InventoryItem inventoryItem = character.inventory[i];
            ItemData itemData = ItemManager.Instance.GetItem(inventoryItem.itemID);

            inventorySlots[i].SetItem(playerID, i, itemData, inventoryItem.quantity);
        }
    }

    private void RefreshArtifacts(CharacterData character)
    {
        for (int i = 0; i < artifactSlots.Length; i++)
        {
            if (i >= character.artifacts.Count)
            {
                artifactSlots[i].Clear();
                continue;
            }

            ArtifactInstance artifact = character.artifacts[i];
            ArtifactData artifactData = ItemManager.Instance.GetItem<ArtifactData>(artifact.artifactID);

            artifactSlots[i].SetItem(playerID, i, artifactData, 1, artifact);
        }
    }

    private void RefreshCurrentThrow(CharacterData character)
    {
        if (string.IsNullOrEmpty(character.currentThrowObjectID))
        {
            currentThrowSlot.Clear();
            return;
        }

        ItemData itemData = ItemManager.Instance.GetItem(character.currentThrowObjectID);

        currentThrowSlot.SetItem(playerID, -1, itemData);
    }

    private void RefreshStats(CharacterData character)
    {
        if (combatInstance == null || combatInstance.state == null)
            return;

        CombatStats stats = combatInstance.state.stats;

        levelValue.text = character.currentLevel.ToString();
        expValue.text = $"{character.currentXP} / {character.currentXPCap}";
        hpValue.text = $"{stats.currentHp:0.#} / {stats.Hp:0.#}";
        shieldValue.text = character.currentShield.ToString("0.#");
        damageValue.text = stats.Atk.ToString("0.#");
        throwSpeedValue.text = stats.throwSpeed.ToString("0.#");
        throwPowerValue.text = stats.throwPower.ToString("0.#");
        throwDelayValue.text = $"{stats.attackSpeed:0.##}s";
        accuracyValue.text = stats.accuracy.ToString("0.#");
    }
}