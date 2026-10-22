using UnityEngine;
using System.Collections.Generic;
public class BlessingManager : MonoBehaviour
{
    public static BlessingManager Instance { get; private set; }
    private readonly List<BlessingData> allBlessings = new();
    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
        allBlessings.AddRange(Resources.LoadAll<BlessingData>("Game/Blessing"));
    }

    public void Trigger(string playerID, BlessingTrigger trigger)
    {
        CharacterData character = StageManager.Instance.GetCharacter(playerID);

        if (character == null)
            return;

        foreach (BlessingInstance blessing in character.blessings)
        {
            if (blessing.data == null)
                continue;

            if (blessing.data.trigger != trigger)
                continue;

            bool conditionMet = CheckCondition(character, blessing.data.condition);

            if (!conditionMet)
            {
                if ((blessing.data.stackReset & BlessingStackReset.OnConditionFalse) != 0)
                    blessing.stacks = 0;

                continue;
            }

            blessing.stacks = Mathf.Min(blessing.stacks + 1, blessing.data.maxStacks);
        }
    }

    private bool CheckCondition(CharacterData character, BlessingCondition condition)
    {
        switch (condition)
        {
            case BlessingCondition.None:
                return true;

            case BlessingCondition.HpBelow50:
                return character.currentHp / character.Hp <= 0.5f;

            case BlessingCondition.HpBelow30:
                return character.currentHp / character.Hp <= 0.3f;
        }

        return false;
    }
    public List<BlessingData> GetRandomBlessings(int count)
    {
        List<BlessingData> pool = new(allBlessings);
        List<BlessingData> result = new();

        count = Mathf.Min(count, pool.Count);

        for (int i = 0; i < count; i++)
        {
            int index = UnityEngine.Random.Range(0, pool.Count);

            result.Add(pool[index]);
            pool.RemoveAt(index);
        }

        return result;
    }
    public BonusStats CalculateTotalBonus(string playerID)
    {
        CharacterData character = StageManager.Instance.GetCharacter(playerID);

        if (character == null)
            return new BonusStats();

        BonusStats total = new BonusStats();

        foreach (BlessingInstance blessing in character.blessings)
        {
            if (blessing == null || blessing.data == null)
                continue;

            if (blessing.stacks <= 0)
                continue;

            AddBlessingBonus(total, blessing.data.bonusPerStack, blessing.stacks);
        }

        return total;
    }
    private void AddBlessingBonus(BonusStats total, BonusStats bonus, int stacks)
    {
        total.atkFlat += bonus.atkFlat * stacks;
        total.hpFlat += bonus.hpFlat * stacks;
        total.accuracyFlat += bonus.accuracyFlat * stacks;
        total.attackSpeedFlat += bonus.attackSpeedFlat * stacks;
        total.moveSpeedFlat += bonus.moveSpeedFlat * stacks;
        total.throwSpeedFlat += bonus.throwSpeedFlat * stacks;
        total.throwPowerFlat += bonus.throwPowerFlat * stacks;

        total.atkPercent += bonus.atkPercent * stacks;
        total.hpPercent += bonus.hpPercent * stacks;
        total.accuracyPercent += bonus.accuracyPercent * stacks;
        total.attackSpeedPercent += bonus.attackSpeedPercent * stacks;
        total.moveSpeedPercent += bonus.moveSpeedPercent * stacks;
        total.throwSpeedPercent += bonus.throwSpeedPercent * stacks;
        total.throwPowerPercent += bonus.throwPowerPercent * stacks;

        total.atkMultiplier *= Mathf.Pow(bonus.atkMultiplier, stacks);
        total.hpMultiplier *= Mathf.Pow(bonus.hpMultiplier, stacks);
        total.accuracyMultiplier *= Mathf.Pow(bonus.accuracyMultiplier, stacks);
        total.attackSpeedMultiplier *= Mathf.Pow(bonus.attackSpeedMultiplier, stacks);
        total.moveSpeedMultiplier *= Mathf.Pow(bonus.moveSpeedMultiplier, stacks);
        total.throwSpeedMultiplier *= Mathf.Pow(bonus.throwSpeedMultiplier, stacks);
        total.throwPowerMultiplier *= Mathf.Pow(bonus.throwPowerMultiplier, stacks);
    }
}