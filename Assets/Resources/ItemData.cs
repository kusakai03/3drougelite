using UnityEngine;
using System;
using System.Collections.Generic;

public abstract class ItemData : ScriptableObject
{
    public string itemID;
    public string itemName;
    [TextArea]
    public string description;
    public Sprite icon;

    public ItemType itemType;
    [Min(1)]
    public int rarity = 1;
}

public enum ItemType
{
    ThrowObject,
    Equipment,
    Currency,
    Material,
    Blessing,
    Artifact
}
[Serializable]
public class EntityItemDrop
{
    public ItemData item;

    [Range(0f, 1f)]
    public float dropChance = 0.1f;

    public int minQuantity = 1;
    public int maxQuantity = 1;
}
[Serializable]
public class PoolItemDrop
{
    public ItemData item;

    [Min(0f)]
    public float weight = 1f;

    public int minQuantity = 1;
    public int maxQuantity = 1;
}
