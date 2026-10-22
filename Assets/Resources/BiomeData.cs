using UnityEngine;
using System.Collections.Generic;
[CreateAssetMenu(menuName = "Map/Biome")]
public class BiomeData : ScriptableObject
{
    public string biomeID;

    public Material terrainMaterial;

    public SpawnTag biomeTags;

    public List<ObstacleData> obstacles = new();
    public List<BiomeDecoration> decorations = new();
    public List<BiomeDecoration> walkableDecorations = new();
    public int mapWidth;
    public int mapHeight;
    [Header("Terrain")]
    public float terrainHeight = 0.3f;
    public float nonWalkableHeight = 0.4f;
    [Header("Water")]
    public bool hasWater;
    public float waterLevel = 0f;
    [Header("Environment")]
    public Material skyboxMaterial;
    public float lightIntensity = 1f;
    public float lightTemperature = 6500f;
}
[System.Serializable]
public class BiomeDecoration
{
    public DecorationData decoration;

    [Range(0f, 1f)]
    public float minNoise = 0f;

    [Range(0f, 1f)]
    public float maxNoise = 1f;

    [Range(0f, 1f)]
    public float density = 1f;

    public float noiseScale = 0.05f;
    public Vector2 noiseOffset;
}