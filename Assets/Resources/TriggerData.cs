using UnityEngine;
using System.Collections.Generic;
[CreateAssetMenu(fileName = "TriggerData", menuName = "Game/Trigger Data")]
public class TriggerData : ScriptableObject
{
    public string id;
    public TriggerType type;
    public TriggerShape shape;
    public float range;

    public List<EntityData> spawnEntities;
    public ChestData chestData;
    public GameObject prefab;
    public GameObject activePrefab;
}
public enum TriggerShape
{
    Circle,
    Box
}
public enum TriggerType
{
    Enemy,
    Boss,
    Chest,
    Merchant,
    Rest,
    Portal
}