using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Chest", menuName = "Game/Chest")]
public class ChestData : ScriptableObject
{
    public int minDrops = 1;
    public int maxDrops = 3;

    public List<PoolItemDrop> lootPool = new();
}