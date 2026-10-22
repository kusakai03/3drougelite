using UnityEngine;

[CreateAssetMenu(menuName = "Map/Obstacle")]
public class ObstacleData : ScriptableObject
{
    public string id;

    public GameObject prefab;
    public Mesh colliderMesh;

    public float weight = 1f;

    public SpawnTag requiredTags;

    public SpawnTag forbiddenTags;

    public Vector2Int size = Vector2Int.one;
}