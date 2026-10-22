using UnityEngine;

public class EntityInstance : MonoBehaviour
{
    public EntityData Data { get; private set; }
    public int Level { get; private set; }

    public void Initialize(EntityData data, int level)
    {
        Data = data;
        Level = level;
    }
}