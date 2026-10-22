using System.Collections.Generic;
using UnityEngine;

public class ReactionSource : MonoBehaviour
{
    [Header("Effects")]
    [SerializeField] private List<ReactionEffectType> baseEffects = new();
    [SerializeField] private List<ReactionEffectType> addableEffects = new();

    private readonly List<ReactionEffectType> dynamicEffects = new();

    public bool CanAddEffect(ReactionEffectType type)
    {
        return addableEffects.Contains(type);
    }

    public bool HasEffect(ReactionEffectType type)
    {
        return baseEffects.Contains(type) || dynamicEffects.Contains(type);
    }

    public bool AddEffect(ReactionEffectType type)
    {
        if (!CanAddEffect(type))
            return false;

        if (HasEffect(type))
            return false;

        dynamicEffects.Add(type);
        return true;
    }

    public bool RemoveEffect(ReactionEffectType type)
    {
        return dynamicEffects.Remove(type);
    }

    public void Apply(ReactionReceiver receiver, CombatInstance attacker, float baseDamageMultiplier, GameObject sourceObject)
    {
        if (receiver == null)
            return;

        CombatInstance defender = receiver.GetComponent<CombatInstance>();
        List<ReactionEffectType> effectsToApply = new(baseEffects);
        effectsToApply.AddRange(dynamicEffects);

        foreach (ReactionEffectType effect in effectsToApply)
            receiver.ApplyEffect(effect, attacker, baseDamageMultiplier, sourceObject);
    }
}
public enum DamageHitType
{
    Strike,
    Dot
}
public enum DamageType
{
    Physical,
    Fire,
    Cold,
    Electro,
    Water,
    Explosion,
    Heal

}