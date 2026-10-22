using System.Collections.Generic;
using UnityEngine;

public class BlessingTabUI : MonoBehaviour
{
    [Header("Blessing Grid")]
    [SerializeField] private Transform blessingContent;
    [SerializeField] private BlessingCardUI blessingCardPrefab;

    [Header("Buff List")]
    [SerializeField] private Transform buffContent;
    [SerializeField] private BlessingBuffRowUI buffRowPrefab;

    private readonly List<GameObject> spawnedBlessingCards = new();
    private readonly List<GameObject> spawnedBuffRows = new();

    private string playerID;

    public void Show(string playerID)
    {
        this.playerID = playerID;
        Refresh();
    }

    public void Refresh()
    {
        CharacterData character = StageManager.Instance.GetCharacter(playerID);

        if (character == null)
            return;

        Clear();

        for (int i = 0; i < character.blessings.Count; i++)
        {
            BlessingInstance blessing = character.blessings[i];

            if (blessing == null || blessing.data == null)
                continue;

            int displayIndex = i + 1;

            SpawnBlessingCard(displayIndex, blessing);
            SpawnBuffRows(displayIndex, blessing);
        }
    }

    private void SpawnBlessingCard(int index, BlessingInstance blessing)
    {
        BlessingCardUI card = Instantiate(blessingCardPrefab, blessingContent);

        card.gameObject.SetActive(true);
        card.Setup(index, blessing);

        spawnedBlessingCards.Add(card.gameObject);
    }

    private void SpawnBuffRows(int index, BlessingInstance blessing)
    {
        BonusStats bonus = blessing.data.bonusPerStack;
        int stacks = blessing.stacks;

        if (bonus.atkFlat != 0f)
            SpawnBuffRow(index, $"ATK {FormatSigned(bonus.atkFlat)} ({stacks})");

        if (bonus.atkPercent != 0f)
            SpawnBuffRow(index, $"ATK {FormatSigned(bonus.atkPercent)}% ({stacks})");

        if (bonus.hpFlat != 0f)
            SpawnBuffRow(index, $"HP {FormatSigned(bonus.hpFlat)} ({stacks})");

        if (bonus.hpPercent != 0f)
            SpawnBuffRow(index, $"HP {FormatSigned(bonus.hpPercent)}% ({stacks})");

        if (bonus.accuracyFlat != 0f)
            SpawnBuffRow(index, $"Accuracy {FormatSigned(bonus.accuracyFlat)} ({stacks})");

        if (bonus.accuracyPercent != 0f)
            SpawnBuffRow(index, $"Accuracy {FormatSigned(bonus.accuracyPercent)}% ({stacks})");

        if (bonus.attackSpeedFlat != 0f)
            SpawnBuffRow(index, $"Throw Delay {FormatSigned(bonus.attackSpeedFlat)} ({stacks})");

        if (bonus.attackSpeedPercent != 0f)
            SpawnBuffRow(index, $"Throw Delay {FormatSigned(bonus.attackSpeedPercent)}% ({stacks})");

        if (bonus.moveSpeedFlat != 0f)
            SpawnBuffRow(index, $"Move Speed {FormatSigned(bonus.moveSpeedFlat)} ({stacks})");

        if (bonus.moveSpeedPercent != 0f)
            SpawnBuffRow(index, $"Move Speed {FormatSigned(bonus.moveSpeedPercent)}% ({stacks})");

        if (bonus.throwSpeedFlat != 0f)
            SpawnBuffRow(index, $"Throw Speed {FormatSigned(bonus.throwSpeedFlat)} ({stacks})");

        if (bonus.throwSpeedPercent != 0f)
            SpawnBuffRow(index, $"Throw Speed {FormatSigned(bonus.throwSpeedPercent)}% ({stacks})");

        if (bonus.throwPowerFlat != 0f)
            SpawnBuffRow(index, $"Throw Power {FormatSigned(bonus.throwPowerFlat)} ({stacks})");

        if (bonus.throwPowerPercent != 0f)
            SpawnBuffRow(index, $"Throw Power {FormatSigned(bonus.throwPowerPercent)}% ({stacks})");
    }

    private void SpawnBuffRow(int index, string text)
    {
        BlessingBuffRowUI row = Instantiate(buffRowPrefab, buffContent);

        row.gameObject.SetActive(true);
        row.Setup(index, text);

        spawnedBuffRows.Add(row.gameObject);
    }

    private string FormatSigned(float value)
    {
        return value >= 0f ? $"+{value:0.##}" : $"{value:0.##}";
    }

    private void Clear()
    {
        foreach (GameObject card in spawnedBlessingCards)
        {
            if (card != null)
                Destroy(card);
        }

        foreach (GameObject row in spawnedBuffRows)
        {
            if (row != null)
                Destroy(row);
        }

        spawnedBlessingCards.Clear();
        spawnedBuffRows.Clear();
    }
}