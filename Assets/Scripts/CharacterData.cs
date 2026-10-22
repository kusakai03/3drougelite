using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class CharacterData
{
    public string playerID;

    [Header("Level")]
    public int currentLevel = 1;
    public int currentXP = 0;
    public int currentXPCap = 10;
    public int highestBlessingLevelQueued = 0;

    [Header("Base Combat")]
    public float baseAtk = 10f;
    public float baseHp = 100f;

    [Header("Base Utility")]
    public float baseMoveSpeed = 20f;
    public float baseAccuracy = 10f;
    public float baseAttackSpeed = 2f;
    public float baseThrowSpeed = 40f;
    public float baseThrowPower = 2f;

    [Header("Current")]
    public float currentHp;
    public float currentShield;

    [Header("Inventory")]
    public int tp = 50000;
    public List<InventoryItem> inventory = new();
    [Header("Blessings")]
    public List<BlessingInstance> blessings = new();
    public List<BlessingChoiceGroup> queueBlessings = new();
    [Header("Artifacts")]
    public List<ArtifactInstance> artifacts = new();
    [Header("Throw")]
    public string currentThrowObjectID;

    [Header("Runtime Combat")]
    public CombatStats combat = new();

    public float Atk => combat.Atk;
    public float Hp => combat.Hp;
    public float Accuracy => combat.accuracy;
    public float AttackSpeed => combat.attackSpeed;
    public float MoveSpeed => combat.moveSpeed;
    public float ThrowSpeed => combat.throwSpeed;
    public float ThrowPower => combat.throwPower;
}
[Serializable]
public class InventoryItem
{
    public string itemID;
    public int quantity;
}
[Serializable]
public class CombatStats
{
    public float atk;
    public float hp;

    public float accuracy;
    public float attackSpeed;
    public float moveSpeed;

    public float throwSpeed;
    public float throwPower;

    public float currentHp;

    public float Atk => atk;
    public float Hp => hp;

    public CombatStats Clone()
    {
        return new CombatStats
        {
            atk = atk,
            hp = hp,
            accuracy = accuracy,
            attackSpeed = attackSpeed,
            moveSpeed = moveSpeed,
            throwSpeed = throwSpeed,
            throwPower = throwPower,
            currentHp = currentHp
        };
    }
}
[Serializable]
public class BonusStats
{
    [Header("Flat")]
    public float atkFlat;
    public float hpFlat;
    public float accuracyFlat;
    public float attackSpeedFlat;
    public float moveSpeedFlat;
    public float throwSpeedFlat;
    public float throwPowerFlat;

    [Header("Percent")]
    public float atkPercent;
    public float hpPercent;
    public float accuracyPercent;
    public float attackSpeedPercent;
    public float moveSpeedPercent;
    public float throwSpeedPercent;
    public float throwPowerPercent;

    [Header("Multiplier")]
    public float atkMultiplier = 1f;
    public float hpMultiplier = 1f;
    public float accuracyMultiplier = 1f;
    public float attackSpeedMultiplier = 1f;
    public float moveSpeedMultiplier = 1f;
    public float throwSpeedMultiplier = 1f;
    public float throwPowerMultiplier = 1f;

    [Header("Element Damage %")]
    public float physicalDamagePercent;
    public float fireDamagePercent;
    public float waterDamagePercent;
    public float electricDamagePercent;
    public float poisonDamagePercent;

    [Header("EXP")]
    public float expBonusPercent;

    public void Add(BonusStats other)
    {
        if (other == null)
            return;

        atkFlat += other.atkFlat;
        hpFlat += other.hpFlat;
        accuracyFlat += other.accuracyFlat;
        attackSpeedFlat += other.attackSpeedFlat;
        moveSpeedFlat += other.moveSpeedFlat;
        throwSpeedFlat += other.throwSpeedFlat;
        throwPowerFlat += other.throwPowerFlat;

        atkPercent += other.atkPercent;
        hpPercent += other.hpPercent;
        accuracyPercent += other.accuracyPercent;
        attackSpeedPercent += other.attackSpeedPercent;
        moveSpeedPercent += other.moveSpeedPercent;
        throwSpeedPercent += other.throwSpeedPercent;
        throwPowerPercent += other.throwPowerPercent;

        atkMultiplier *= other.atkMultiplier;
        hpMultiplier *= other.hpMultiplier;
        accuracyMultiplier *= other.accuracyMultiplier;
        attackSpeedMultiplier *= other.attackSpeedMultiplier;
        moveSpeedMultiplier *= other.moveSpeedMultiplier;
        throwSpeedMultiplier *= other.throwSpeedMultiplier;
        throwPowerMultiplier *= other.throwPowerMultiplier;

        physicalDamagePercent += other.physicalDamagePercent;
        fireDamagePercent += other.fireDamagePercent;
        waterDamagePercent += other.waterDamagePercent;
        electricDamagePercent += other.electricDamagePercent;
        poisonDamagePercent += other.poisonDamagePercent;

        expBonusPercent += other.expBonusPercent;
    }
}
[Serializable]
public class BlessingChoiceGroup
{
    public List<BlessingData> choices = new();
}