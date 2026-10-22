using System;
using UnityEngine;
public static class PlayerStatCalculator
{
    public static CombatStats Calculate(CharacterData character, BonusStats bonus)
    {
        int levelOffset = character.currentLevel - 1;
        float levelMultiplier = 1f + 0.05f * levelOffset;

        float levelAtk = character.baseAtk + levelOffset;
        float levelHp = character.baseHp + 4f * levelOffset;
        CombatStats stats = new();
        stats.atk =
            (levelAtk + bonus.atkFlat)
            * (1f + bonus.atkPercent / 100f)
            * bonus.atkMultiplier
            * levelMultiplier;

        stats.hp =
            (levelHp + bonus.hpFlat)
            * (1f + bonus.hpPercent / 100f)
            * bonus.hpMultiplier
            * levelMultiplier;

        stats.accuracy =
            (character.baseAccuracy + bonus.accuracyFlat)
            * (1f + bonus.accuracyPercent / 100f)
            * bonus.accuracyMultiplier;

        stats.attackSpeed =
            (character.baseAttackSpeed + bonus.attackSpeedFlat)
            * (1f + bonus.attackSpeedPercent / 100f)
            * bonus.attackSpeedMultiplier;

        stats.moveSpeed =
            (character.baseMoveSpeed + bonus.moveSpeedFlat)
            * (1f + bonus.moveSpeedPercent / 100f)
            * bonus.moveSpeedMultiplier;

        stats.throwSpeed =
            (character.baseThrowSpeed + bonus.throwSpeedFlat)
            * (1f + bonus.throwSpeedPercent / 100f)
            * bonus.throwSpeedMultiplier;

        stats.throwPower =
            (character.baseThrowPower + bonus.throwPowerFlat)
            * (1f + bonus.throwPowerPercent / 100f)
            * bonus.throwPowerMultiplier;
        stats.hp = Mathf.Max(stats.hp, 1f);
        stats.accuracy = Mathf.Max(stats.accuracy, 1f);
        stats.throwPower = Mathf.Max(stats.throwPower, 1f);
        stats.currentHp = Mathf.Min(character.currentHp, stats.hp);

        return stats;
    }
}