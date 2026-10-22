using UnityEngine;
using System.Collections.Generic;
[CreateAssetMenu(fileName = "GlobalDropPool", menuName = "Items/Global Drop Pool")]
public class GlobalDropPool : ScriptableObject
{
    public List<PoolItemDrop> drops = new();

    [Min(0)]
    public int minDrops = 0;

    [Min(0)]
    public int maxDrops = 2;
}