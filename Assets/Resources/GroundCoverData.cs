using UnityEngine;

[CreateAssetMenu(menuName = "Map/Ground Cover")]
public class GroundCoverData : ScriptableObject
{
    public string id;

    public GameObject prefab;

    public float spawnChance = 0.3f;

    public float weight = 1f;

    public Vector2 randomScale = Vector2.one;
}