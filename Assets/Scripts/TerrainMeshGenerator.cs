using UnityEngine;
using System.Collections.Generic;

public class TerrainMeshGenerator : MonoBehaviour
{
    [SerializeField] private float terrainNoiseScale = 0.04f;
    [SerializeField] private int chunkSize = 50;
    [SerializeField] private Material terrainMaterial;
    private Texture2D terrainMask;
    private readonly Dictionary<Vector2Int, GameObject> terrainChunks = new();
    [SerializeField] private int chunkRenderRadius = 2;
    private Vector2Int lastPlayerChunk = new Vector2Int(int.MinValue, int.MinValue);
    [SerializeField] private Light directionalLight;
    [SerializeField] private PhysicsMaterial terrainColliderMaterial;
    private void Update()
    {
        UpdatePlayerChunk();
    }
    private void UpdatePlayerChunk()
    {
        GameObject player = PlayerManager.Instance?.LocalPlayer;
        if (player == null)
            return;

        Vector2Int playerChunk = GetPlayerChunk(player.transform);

        if (playerChunk != lastPlayerChunk)
        {
            lastPlayerChunk = playerChunk;
            UpdateVisibleChunks(playerChunk);
        }
    }
    private Vector2Int GetPlayerChunk(Transform player)
    {
        int chunkX = Mathf.FloorToInt(
            player.position.x / chunkSize
        );

        int chunkY = Mathf.FloorToInt(
            player.position.z / chunkSize
        );

        return new Vector2Int(chunkX, chunkY);
    }
    public void Generate(GeneratedMap map, float cellSize = 1f)
    {
        ClearChunks();
        terrainMask = TerrainMaskGenerator.Generate(map);
        terrainMaterial = map.biome.terrainMaterial;
        terrainMaterial.SetTexture("_Mask", terrainMask);

        int chunkCountX = Mathf.CeilToInt((float)map.mapWidth / chunkSize);
        int chunkCountY = Mathf.CeilToInt((float)map.mapHeight / chunkSize);

        for (int cy = 0; cy < chunkCountY; cy++)
        {
            for (int cx = 0; cx < chunkCountX; cx++)
            {   
                GenerateChunk(map, cx, cy, cellSize);
            }
        }
        UpdatePlayerChunk();
        ApplyBiomeEnvironment(map.biome);
    }
    
    private void ApplyBiomeEnvironment(BiomeData biome)
    {
        RenderSettings.skybox = biome.skyboxMaterial;

        directionalLight.intensity = biome.lightIntensity;
        directionalLight.useColorTemperature = true;
        directionalLight.colorTemperature = biome.lightTemperature;

        DynamicGI.UpdateEnvironment();
    }
    private void GenerateChunk(GeneratedMap map, int chunkX, int chunkY, float cellSize)
    {
        int startX = chunkX * chunkSize;
        int startY = chunkY * chunkSize;

        int width = Mathf.Min(chunkSize, map.mapWidth - startX);
        int height = Mathf.Min(chunkSize, map.mapHeight - startY);

        GameObject chunk = new GameObject($"TerrainChunk_{chunkX}_{chunkY}");
        chunk.transform.SetParent(transform, true);
        chunk.layer = gameObject.layer;
        MeshFilter filter = chunk.AddComponent<MeshFilter>();
        MeshRenderer renderer = chunk.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = terrainMaterial;
        MeshCollider collider = chunk.AddComponent<MeshCollider>();
        collider.material = terrainColliderMaterial;
        Mesh mesh = new Mesh();
        mesh.name = $"TerrainChunk_{chunkX}_{chunkY}";

        List<Vector3> vertices = new();
        List<int> triangles = new();
        List<Vector2> uv0 = new();
        List<Vector2> uv1 = new();
        List<Color> colors = new();

        int vertexWidth = width + 1;

        for (int y = 0; y <= height; y++)
        {
            for (int x = 0; x <= width; x++)
            {
                int worldX = startX + x;
                int worldY = startY + y;

                worldX = Mathf.Clamp(worldX, 0, map.mapWidth);
                worldY = Mathf.Clamp(worldY, 0, map.mapHeight);

                float h = GetVertexHeight(map, worldX, worldY);

                vertices.Add(new Vector3(
                    worldX * cellSize,
                    h,
                    worldY * cellSize
                ));

                uv0.Add(new Vector2(worldX, worldY));

                uv1.Add(new Vector2(
                    Mathf.Clamp01((float)worldX / (map.mapWidth - 1)),
                    Mathf.Clamp01((float)worldY / (map.mapHeight - 1))
                ));

                int cellX = Mathf.Clamp(worldX, 0, map.mapWidth - 1);
                int cellY = Mathf.Clamp(worldY, 0, map.mapHeight - 1);

                Color color = map.walkable[cellX, cellY] == 1
                    ? new Color(0.45f, 0.32f, 0.18f)
                    : new Color(0.2f, 0.65f, 0.2f);

                colors.Add(color);
            }
        }

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int start = y * vertexWidth + x;

                triangles.Add(start);
                triangles.Add(start + vertexWidth + 1);
                triangles.Add(start + 1);

                triangles.Add(start);
                triangles.Add(start + vertexWidth);
                triangles.Add(start + vertexWidth + 1);
            }
        }

        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles, 0);
        mesh.SetUVs(0, uv0);
        mesh.SetUVs(1, uv1);
        mesh.SetColors(colors);

        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        filter.sharedMesh = mesh;
        collider.sharedMesh = mesh;

        terrainChunks.Add(new Vector2Int(chunkX, chunkY), chunk);
    }

    private void ClearChunks()
    {
        foreach (GameObject chunk in terrainChunks.Values)
        {
            if (chunk != null)
                Destroy(chunk);
        }

        terrainChunks.Clear();
    }

    private float GetVertexHeight(GeneratedMap map, int x, int y)
    {
        float noise = Mathf.PerlinNoise(x * terrainNoiseScale, y * terrainNoiseScale);

        float baseHeight = noise * map.biome.terrainHeight;
        float nonWalkableFactor = GetNonWalkableFactor(map, x, y);

        return baseHeight + nonWalkableFactor * map.biome.nonWalkableHeight;
    }

    private float GetNonWalkableFactor(GeneratedMap map, int x, int y)
    {
        float total = 0f;
        int count = 0;

        for (int oy = -1; oy <= 0; oy++)
        {
            for (int ox = -1; ox <= 0; ox++)
            {
                int cellX = x + ox;
                int cellY = y + oy;

                if (cellX < 0 || cellX >= map.mapWidth ||
                    cellY < 0 || cellY >= map.mapHeight)
                    continue;

                if (map.walkable[cellX, cellY] == 0)
                    total += 1f;

                count++;
            }
        }

        return count == 0 ? 0f : total / count;
    }

    public float GetHeightAt(GeneratedMap map, float x, float y)
    {
        int cellX = Mathf.FloorToInt(x);
        int cellY = Mathf.FloorToInt(y);

        cellX = Mathf.Clamp(cellX, 0, map.mapWidth - 1);
        cellY = Mathf.Clamp(cellY, 0, map.mapHeight - 1);

        float localX = x - cellX;
        float localY = y - cellY;

        float h00 = GetVertexHeight(map, cellX, cellY);
        float h10 = GetVertexHeight(map, cellX + 1, cellY);
        float h11 = GetVertexHeight(map, cellX + 1, cellY + 1);
        float h01 = GetVertexHeight(map, cellX, cellY + 1);

        float bottom = Mathf.Lerp(h00, h10, localX);
        float top = Mathf.Lerp(h01, h11, localX);

        return Mathf.Lerp(bottom, top, localY);
    }
    private void UpdateVisibleChunks(Vector2Int playerChunk)
    {
        foreach (var pair in terrainChunks)
        {
            Vector2Int chunkCoord = pair.Key;
            GameObject chunk = pair.Value;

            int distanceX = Mathf.Abs(chunkCoord.x - playerChunk.x);
            int distanceY = Mathf.Abs(chunkCoord.y - playerChunk.y);

            bool visible =
                distanceX <= chunkRenderRadius &&
                distanceY <= chunkRenderRadius;

            if (chunk.activeSelf != visible)
                chunk.SetActive(visible);
        }
    }
}
public static class TerrainMaskGenerator
{
    public static Texture2D Generate(GeneratedMap map)
    {
        Texture2D texture = new Texture2D(map.mapWidth, map.mapHeight, TextureFormat.R8, false)
        {
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };

        for (int y = 0; y < map.mapHeight; y++)
        {
            for (int x = 0; x < map.mapWidth; x++)
            {
                float total = 0f;
                int count = 0;

                for (int oy = -1; oy <= 1; oy++)
                {
                    for (int ox = -1; ox <= 1; ox++)
                    {
                        int nx = x + ox;
                        int ny = y + oy;

                        if (nx < 0 || nx >= map.mapWidth ||
                            ny < 0 || ny >= map.mapHeight)
                            continue;

                        if (map.walkable[nx, ny] == 1)
                            total += 1f;

                        count++;
                    }
                }

                float value = count > 0 ? total / count : 0f;

                texture.SetPixel(x, y, new Color(value, value, value, 1f));
            }
        }

        texture.Apply();

        return texture;
    }
}