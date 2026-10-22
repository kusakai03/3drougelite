using UnityEngine;
using System;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "EntityData", menuName = "Game/Entity Data")]
public class EntityData : ScriptableObject
{
    public string id;
    public GameObject prefab;
    [Header("Drops")]
    public List<EntityItemDrop> itemDrops = new();
    public float baseAtk;
    public float baseHp;
}

[Serializable]
public class LootDrop
{
    public ItemData item;
    public int quantity;
    public ArtifactInstance artifact;
}