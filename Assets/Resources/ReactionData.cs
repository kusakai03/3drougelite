using UnityEngine;

[CreateAssetMenu(fileName = "New Reaction", menuName = "Reaction/Reaction")]
public class ReactionData : ScriptableObject
{
    [Header("Reaction")]
    public ReactionEffectType effectA;
    public ReactionEffectType effectB;

    [Header("Damage")]
    public float damageMultiplier = 1f;
    public DamageType damageType;
    public DamageHitType damageHitType = DamageHitType.Strike;

    [Header("Result Effect")]
    public bool newEffect;
    public ReactionEffectType resultEffect;

    [Header("Source Result")]
    public bool addEffectToSource;
    public ReactionEffectType sourceEffect;

    [Header("Visual")]
    public GameObject reactionPrefab;
    [Header("Consume Objects")]
    public bool destroyReceiver;
    public bool destroySource;
}
public enum ReactionEffectType
{
    Fire,
    Wet,
    Oil,
    Cold,
    Electric,
    Explosive,
    Battery,
    Flammable,
    Burning,
    
    Shatter,
    Biological,
    Bleed,
    Electrocute,
    Frozen,
    Venom,
    Poison,
    Acid,
    Toxic,
    Metal

}

[System.Serializable]
public class ReactionEffect
{
    public ReactionEffectType type;
    public float remainingDuration;
    public bool isBase;

    [System.NonSerialized] public CombatInstance attacker;
    [System.NonSerialized] public float damageMultiplier;
    [System.NonSerialized] public float baseDamageMultiplier;
    [System.NonSerialized] public DamageType damageType;
    [System.NonSerialized] public DamageHitType damageHitType;
    [System.NonSerialized] public float dotTimer;

    public ReactionEffect(ReactionEffectType type, float remainingDuration, bool isBase)
    {
        this.type = type;
        this.remainingDuration = remainingDuration;
        this.isBase = isBase;
    }
}

public static class ReactionEffectConfig
{
    public static float GetDuration(ReactionEffectType type)
    {
        return type switch
        {
            ReactionEffectType.Fire => 5f,
            ReactionEffectType.Wet => 8f,
            ReactionEffectType.Oil => 10f,
            ReactionEffectType.Cold => 5f,
            ReactionEffectType.Electric => 4f,
            ReactionEffectType.Burning => 4f,
            ReactionEffectType.Bleed => 5f,
            ReactionEffectType.Electrocute => 4f,
            ReactionEffectType.Frozen => 2f,
            ReactionEffectType.Poison => 10f,
            ReactionEffectType.Toxic => 6f,
            _ => 5f
        };
    }
}