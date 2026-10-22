using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Throw Object", menuName = "Items/Throw Object")]
public class ThrowObj : ItemData
{

    [Header("Prefab")]
    public GameObject projectilePrefab;
    public GameObject hitProjectilePrefab;
    public enum HitBehavior
    {
        Bounce,
        Destroy,
        StayStill,
        StickToTarget
    }
    [Header("Combat")]
    public DamageType damageType;
    public float weight = 1f;
    public float throwDelay = 0.2f;
    [Header("Hit")]
    public HitBehavior hitBehavior;

    public int bounceCount;
    public float bouncePower;

    [Header("Ammo")]
    public int ammoCost = 1;

    [Header("Effects")]
    //public List<EffectData> effects = new();
    public AudioClip throwSfx;
    public AudioClip hitSfx;
}