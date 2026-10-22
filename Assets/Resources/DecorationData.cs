using UnityEngine;

[CreateAssetMenu(menuName = "Map/Decoration")]
public class DecorationData : ScriptableObject
{
    public string id;

    public GameObject prefab;

    public float weight = 1f;

    public SpawnTag requiredTags;

    public SpawnTag forbiddenTags;

    public Vector2Int size = Vector2Int.one;
}