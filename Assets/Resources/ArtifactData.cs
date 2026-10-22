using UnityEngine;
using System;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "New Artifact", menuName = "Items/Artifact")]
public class ArtifactData : ItemData
{
    [Range(0, 3)]
    public int startingLevel = 0;

    public ArtifactStat mainStat;

    private void OnValidate()
    {
        itemType = ItemType.Artifact;
        startingLevel = Mathf.Clamp(startingLevel, 0, 3);
    }
}

[System.Serializable]
public class ArtifactInstance
{
    public string artifactID;
    public int level;
    public List<ArtifactStat> subStats = new();
}

[System.Serializable]
public class ArtifactStat
{
    public ArtifactStatType statType;
    public ArtifactModifierType modifierType;
    public float value;

    public void ApplyToBonus(BonusStats bonus, float effectiveness = 1f)
    {
        float finalValue = value * effectiveness;

        switch (statType)
        {
            case ArtifactStatType.Attack:
                Apply(ref bonus.atkFlat, ref bonus.atkPercent, ref bonus.atkMultiplier, finalValue);
                break;

            case ArtifactStatType.Hp:
                Apply(ref bonus.hpFlat, ref bonus.hpPercent, ref bonus.hpMultiplier, finalValue);
                break;

            case ArtifactStatType.Accuracy:
                Apply(ref bonus.accuracyFlat, ref bonus.accuracyPercent, ref bonus.accuracyMultiplier, finalValue);
                break;

            case ArtifactStatType.AttackSpeed:
                Apply(ref bonus.attackSpeedFlat, ref bonus.attackSpeedPercent, ref bonus.attackSpeedMultiplier, finalValue);
                break;

            case ArtifactStatType.MoveSpeed:
                Apply(ref bonus.moveSpeedFlat, ref bonus.moveSpeedPercent, ref bonus.moveSpeedMultiplier, finalValue);
                break;

            case ArtifactStatType.ThrowSpeed:
                Apply(ref bonus.throwSpeedFlat, ref bonus.throwSpeedPercent, ref bonus.throwSpeedMultiplier, finalValue);
                break;

            case ArtifactStatType.ThrowPower:
                Apply(ref bonus.throwPowerFlat, ref bonus.throwPowerPercent, ref bonus.throwPowerMultiplier, finalValue);
                break;

            case ArtifactStatType.PhysicalDamage:
                bonus.physicalDamagePercent += finalValue;
                break;

            case ArtifactStatType.FireDamage:
                bonus.fireDamagePercent += finalValue;
                break;

            case ArtifactStatType.WaterDamage:
                bonus.waterDamagePercent += finalValue;
                break;

            case ArtifactStatType.ElectricDamage:
                bonus.electricDamagePercent += finalValue;
                break;

            case ArtifactStatType.PoisonDamage:
                bonus.poisonDamagePercent += finalValue;
                break;

            case ArtifactStatType.ExpBonus:
                bonus.expBonusPercent += finalValue;
                break;
        }
    }

    private void Apply(ref float flat, ref float percent, ref float multiplier, float finalValue)
    {
        switch (modifierType)
        {
            case ArtifactModifierType.Flat:
                flat += finalValue;
                break;

            case ArtifactModifierType.Percent:
                percent += finalValue;
                break;

            case ArtifactModifierType.Multiplier:
                multiplier *= finalValue;
                break;
        }
    }

    public ArtifactStat Clone()
    {
        return new ArtifactStat
        {
            statType = statType,
            modifierType = modifierType,
            value = value
        };
    }
}

public enum ArtifactStatType
{
    Attack,
    Hp,
    Accuracy,
    AttackSpeed,
    MoveSpeed,
    ThrowSpeed,
    ThrowPower,
    PhysicalDamage,
    FireDamage,
    WaterDamage,
    ElectricDamage,
    PoisonDamage,
    ExpBonus
}

public enum ArtifactModifierType
{
    Flat,
    Percent,
    Multiplier
}

[CreateAssetMenu(fileName = "ArtifactSubStatPool", menuName = "Items/Artifact Sub Stat Pool")]
public class ArtifactSubStatPool : ScriptableObject
{
    public List<ArtifactStat> subStats = new();
}