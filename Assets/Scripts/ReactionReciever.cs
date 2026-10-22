using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ReactionReceiver : MonoBehaviour
{
    [SerializeField] private List<ReactionEffectType> baseEffects = new();
    [SerializeField] private List<ReactionEffectType> immuneEffects = new();
    [SerializeField] private float baseEffectRestoreDelay = 1f;

    private readonly List<ReactionEffect> currentEffects = new();

    public IReadOnlyList<ReactionEffect> CurrentEffects => currentEffects;
    private const float DotInterval = 0.25f;
    private const float EffectAddCooldown = 0.3f;
    private float lastEffectAddTime = -999f;

    private bool CanAddNewEffect()
    {
        return Time.time >= lastEffectAddTime + EffectAddCooldown;
    }

    private void Awake()
    {
        foreach (ReactionEffectType type in baseEffects)
            AddBaseEffect(type);
    }

    private void Update()
    {
        for (int i = currentEffects.Count - 1; i >= 0; i--)
        {
            ReactionEffect effect = currentEffects[i];

            if (!effect.isBase)
            {
                effect.remainingDuration -= Time.deltaTime;

                if (effect.remainingDuration <= 0f)
                {
                    RemoveEffect(effect.type);
                    continue;
                }
            }

            if (effect.damageHitType != DamageHitType.Dot || effect.damageMultiplier <= 0f)
                continue;

            effect.dotTimer += Time.deltaTime;

            while (effect.dotTimer >= DotInterval)
            {
                effect.dotTimer -= DotInterval;
                ApplyDotDamage(effect);
            }
        }
    }

    private void ApplyDotDamage(ReactionEffect effect)
    {
        if (effect.attacker == null)
            return;

        CombatInstance defender = GetComponent<CombatInstance>();

        if (defender == null)
            return;

        float finalDamageMultiplier = effect.baseDamageMultiplier * effect.damageMultiplier;

        StageManager.Instance.DamageHit(effect.attacker, defender, finalDamageMultiplier, effect.damageType, DamageHitType.Dot);
    }

    public void ApplyEffect(ReactionEffectType incomingEffect, CombatInstance attacker, float baseDamageMultiplier, GameObject sourceObject)
    {
        if (IsImmune(incomingEffect))
            return;
        ReactionData reaction = FindReaction(incomingEffect);

        if (reaction == null)
        {
            AddEffect(incomingEffect);
            return;
        }
        ReactionEffect existingEffect = FindEffect(reaction.effectB);

        if (existingEffect == null)
            return;

        RemoveEffect(existingEffect.type);

        if (reaction.newEffect)
            AddReactionEffect(reaction, attacker, baseDamageMultiplier);

        if (reaction.addEffectToSource)
        {
            ReactionSource source = GetComponent<ReactionSource>();

            if (source != null)
                source.AddEffect(reaction.sourceEffect);
        }

        if (reaction.reactionPrefab != null)
        {
            GameObject obj = Instantiate(reaction.reactionPrefab, transform.position, Quaternion.identity);
        }

        CombatInstance defender = GetComponent<CombatInstance>();

        if (attacker != null && reaction.damageMultiplier > 0f && reaction.damageHitType == DamageHitType.Strike)
        {
            float reactionDamageMultiplier = baseDamageMultiplier * reaction.damageMultiplier;
            StageManager.Instance.DamageEffect(attacker, defender, transform.position, reactionDamageMultiplier, reaction.damageType, DamageHitType.Strike);
        }
        if (reaction.destroyReceiver)
            Destroy(gameObject);

        if (reaction.destroySource && sourceObject != null)
            Destroy(sourceObject);
    }

    public bool IsImmune(ReactionEffectType type)
    {
        return immuneEffects.Contains(type);
    }

    public void AddEffect(ReactionEffectType type)
    {
        if (IsImmune(type))
            return;

        ReactionEffect existing = FindEffect(type);

        if (existing != null)
        {
            if (!existing.isBase)
                existing.remainingDuration = ReactionEffectConfig.GetDuration(type);

            return;
        }

        if (!CanAddNewEffect())
            return;

        currentEffects.Add(new ReactionEffect(type, ReactionEffectConfig.GetDuration(type), false));
        lastEffectAddTime = Time.time;
    }

    public void AddBaseEffect(ReactionEffectType type)
    {
        ReactionEffect existing = FindEffect(type);

        if (existing != null)
        {
            existing.isBase = true;
            existing.remainingDuration = -1f;
            return;
        }

        currentEffects.Add(new ReactionEffect(type, -1f, true));
    }

    private void AddReactionEffect(ReactionData reaction, CombatInstance attacker, float baseDamageMultiplier)
    {
        if (IsImmune(reaction.resultEffect))
            return;

        ReactionEffect effect = FindEffect(reaction.resultEffect);

        if (effect == null)
        {

            effect = new ReactionEffect(reaction.resultEffect, ReactionEffectConfig.GetDuration(reaction.resultEffect), false);
            currentEffects.Add(effect);
        }
        else if (!effect.isBase)
        {
            effect.remainingDuration = ReactionEffectConfig.GetDuration(reaction.resultEffect);
        }

        effect.attacker = attacker;
        effect.baseDamageMultiplier = baseDamageMultiplier;
        effect.damageMultiplier = reaction.damageMultiplier;
        effect.damageType = reaction.damageType;
        effect.damageHitType = reaction.damageHitType;
        effect.dotTimer = 0f;
    }

    public void RemoveEffect(ReactionEffectType type)
    {
        ReactionEffect effect = FindEffect(type);

        if (effect == null)
            return;

        bool wasBase = effect.isBase;
        currentEffects.Remove(effect);

        ReactionSource source = GetComponent<ReactionSource>();

        if (source != null)
            source.RemoveEffect(type);

        if (wasBase && baseEffects.Contains(type))
            StartCoroutine(RestoreBaseEffect(type));
    }

    private ReactionData FindReaction(ReactionEffectType incomingEffect)
    {
        if (IsImmune(incomingEffect))
            return null;

        foreach (ReactionEffect existingEffect in currentEffects)
        {
            ReactionData reaction = ReactionManager.Instance.FindReaction(incomingEffect, existingEffect.type);

            if (reaction == null)
                continue;

            if (IsImmune(reaction.effectA) || IsImmune(reaction.effectB))
                continue;

            return reaction;
        }

        return null;
    }

    private ReactionEffect FindEffect(ReactionEffectType type)
    {
        return currentEffects.Find(x => x.type == type);
    }

    private IEnumerator RestoreBaseEffect(ReactionEffectType type)
    {
        yield return new WaitForSeconds(baseEffectRestoreDelay);

        if (!HasEffect(type))
            AddBaseEffect(type);
    }

    public bool HasEffect(ReactionEffectType type)
    {
        return FindEffect(type) != null;
    }

    public void ClearTemporaryEffects()
    {
        for (int i = currentEffects.Count - 1; i >= 0; i--)
        {
            ReactionEffect effect = currentEffects[i];

            if (!effect.isBase)
                RemoveEffect(effect.type);
        }
    }
}