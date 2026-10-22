using System;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
public class RoomManager : MonoBehaviour
{
    public GeneratedMap generatedMap;
    public Action<MapNode> OnPlayerSpawn;
    public Action<GeneratedMap> OnMapGenerated;
    [SerializeField] private TerrainMeshGenerator terrainGenerator;
    [SerializeField] private WaterMeshGenerator waterMeshGenerator;
    [SerializeField] private GrassMesh grass;
    [SerializeField] private float cellSize = 1f;
    [SerializeField] private MeshRenderer terrainRenderer;
    [ContextMenu("Generate Map")]

    public void GenerateMap(StageRoomData roomData)
    {
        RoomGenerator generator = new();
        generatedMap = generator.Generate(roomData.biome, roomData);
        float t = Time.realtimeSinceStartup;
        terrainGenerator.Generate(generatedMap, cellSize);
        waterMeshGenerator.Generate(generatedMap);
        Debug.Log($"Terrain: {(Time.realtimeSinceStartup - t) * 1000f} ms");
        Texture2D mask = TerrainMaskGenerator.Generate(generatedMap);
        mask = BlurMask(mask, 1);
        terrainRenderer.material.SetTexture("_Mask", mask);
        MapNode spawnNode = GetNodeByTriggerID("spawnplayerarea");
        OnPlayerSpawn?.Invoke(spawnNode);
        //terrainRenderer.material.mainTexture = mask;
        OnMapGenerated?.Invoke(generatedMap);
    }
    public MapNode GetNodeByTriggerID(string id)
    {
        return generatedMap.nodes.Find(x => x.triggerData.id == id);
    }
    public MapNode GetNodeByID(string id)
    {
        return generatedMap.nodes.Find(x => x.id == id);
    }
    private Texture2D BlurMask(Texture2D source, int radius)
    {
        int width = source.width;
        int height = source.height;

        Texture2D result = new Texture2D(
            width,
            height,
            TextureFormat.R8,
            false);

        result.filterMode = FilterMode.Bilinear;
        result.wrapMode = TextureWrapMode.Clamp;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float sum = 0;
                int count = 0;

                for (int oy = -radius; oy <= radius; oy++)
                {
                    for (int ox = -radius; ox <= radius; ox++)
                    {
                        int px = Mathf.Clamp(x + ox, 0, width - 1);
                        int py = Mathf.Clamp(y + oy, 0, height - 1);

                        sum += source.GetPixel(px, py).r;
                        count++;
                    }
                }

                float value = sum / count;

                result.SetPixel(
                    x,
                    y,
                    new Color(value, value, value, 1));
            }
        }

        result.Apply();

        return result;
    }
    public bool IsWalkable(Vector3 worldPosition)
    {
        if (generatedMap == null)
            return false;

        float gridX = worldPosition.x - 0.5f;
        float gridZ = worldPosition.z - 0.5f;

        int x0 = Mathf.FloorToInt(gridX);
        int z0 = Mathf.FloorToInt(gridZ);
        int x1 = x0 + 1;
        int z1 = z0 + 1;

        if (x0 < 0 || z0 < 0 || x1 >= generatedMap.mapWidth || z1 >= generatedMap.mapHeight)
            return false;

        float tx = gridX - x0;
        float tz = gridZ - z0;

        float v00 = generatedMap.walkable[x0, z0];
        float v10 = generatedMap.walkable[x1, z0];
        float v01 = generatedMap.walkable[x0, z1];
        float v11 = generatedMap.walkable[x1, z1];

        float bottom = Mathf.Lerp(v00, v10, tx);
        float top = Mathf.Lerp(v01, v11, tx);
        float value = Mathf.Lerp(bottom, top, tz);

        return value >= 0.5f;
    }
}
public class RoomGenerator
{
    public GeneratedMap Generate(BiomeData biome, StageRoomData roomData)
    {
        GeneratedMap map = new();

        map.biome = biome;

        // Map size lấy từ BiomeData
        map.mapWidth = UnityEngine.Random.Range(biome.mapWidth, biome.mapWidth + 200);
        map.mapHeight = UnityEngine.Random.Range(biome.mapHeight, biome.mapHeight + 200);

        map.borderSize = Mathf.RoundToInt(Mathf.Min(map.mapWidth, map.mapHeight) * 0.15f );

        GenerateNodes(map, roomData);
        GenerateBranches(map);

        map.walkable = new int[
            map.mapWidth,
            map.mapHeight
        ];

        GenerateRoad(map);

        GenerateSpawnPoints(map);
        AnalyzeSpawnPoints(map);

        return map;
    }

    private void GenerateNodes(GeneratedMap map, StageRoomData roomData)
    {
        map.nodes.Clear();

        foreach (StageTriggerConfig config in roomData.nodes)
        {
            CreateNode(map, $"node_{map.nodes.Count}", config.triggerData, config.width, config.height);
        }
    }

    private void CreateNode(GeneratedMap map, string id, TriggerData triggerData, int width, int height)
    {
        const int margin = 20;
        const int maxTry = 200;

        for (int i = 0; i < maxTry; i++)
        {
            RectInt area = map.PlayableArea;

            Vector2Int center = new Vector2Int(
                UnityEngine.Random.Range(
                    area.xMin + margin + width / 2,
                    area.xMax - margin - width / 2
                ),
                UnityEngine.Random.Range(
                    area.yMin + margin + height / 2,
                    area.yMax - margin - height / 2
                )
            );

            MapNode node = new()
            {
                id = id,
                triggerData = triggerData,
                center = center,
                width = width,
                height = height
            };

            if (IsNodeOverlap(map, node))
                continue;

            map.nodes.Add(node);
            return;
        }

        Debug.LogWarning($"Cannot place node {id}");
    }

    private bool IsNodeOverlap(GeneratedMap map, MapNode newNode)
    {
        const int spacing = 10;

        foreach (MapNode other in map.nodes)
        {
            bool overlap =
                newNode.Left < other.Right + spacing &&
                newNode.Right > other.Left - spacing &&
                newNode.Bottom < other.Top + spacing &&
                newNode.Top > other.Bottom - spacing;

            if (overlap)
                return true;
        }

        return false;
    }
    private void GenerateBranches(GeneratedMap map)
    {
        map.branches.Clear();

        // Đường chính
        for (int i = 0; i < map.nodes.Count - 1; i++)
        {
            CreateBranch(map, map.nodes[i].id, map.nodes[i + 1].id);
        }

        // Random branch
        foreach (MapNode from in map.nodes)
        {
            if (UnityEngine.Random.value > 0.5f)
                continue;

            List<MapNode> candidates = new();

            foreach (MapNode to in map.nodes)
            {
                if (to == from)
                    continue;

                if (HasBranch(
                    map,
                    from.id,
                    to.id))
                    continue;

                candidates.Add(to);
            }

            if (candidates.Count == 0)
                continue;

            MapNode random =
                candidates[
                    UnityEngine.Random.Range(
                        0,
                        candidates.Count)];

            CreateBranch(
                map,
                from.id,
                random.id);
        }
    }
    private void CreateBranch(GeneratedMap map, string from, string to)
    {
        if (HasBranch(map, from, to))
            return;

        map.branches.Add(new MapBranch()
        {
            fromNodeID = from,
            toNodeID = to,
            curveAmount = UnityEngine.Random.Range(-50f, 50f),
            width = UnityEngine.Random.Range(7, 13)
        });
    }
    private bool HasBranch(GeneratedMap map, string from, string to)
    {
        foreach (MapBranch branch in map.branches)
        {
            if (branch.fromNodeID == from &&
                branch.toNodeID == to)
                return true;

            if (branch.fromNodeID == to &&
                branch.toNodeID == from)
                return true;
        }

        return false;
    }
    private void GenerateRoad(GeneratedMap map)
    {
        PaintNodes(map);

        foreach (MapBranch branch in map.branches)
        {
            PaintBranch(map, branch);
        }
        SmoothRoad(map);
    }
    private void PaintNodes(GeneratedMap map)
    {
        int count = 0;
        foreach (MapNode node in map.nodes)
        {
            for (int x = node.Left; x <= node.Right; x++)
            {
                for (int y = node.Bottom; y <= node.Top; y++)
                {
                    if (x < 0 || x >= map.mapWidth)
                        continue;

                    if (y < 0 || y >= map.mapHeight)
                        continue;

                    map.walkable[x, y] = 1;
                    count++;
                }
            }
        }
    }
    private void PaintBranch(GeneratedMap map, MapBranch branch)
    {
        MapNode from = map.nodes.Find(x => x.id == branch.fromNodeID);
        MapNode to = map.nodes.Find(x => x.id == branch.toNodeID);

        if (from == null || to == null)
            return;

        Vector2 a = from.center;
        Vector2 b = to.center;

        Vector2 mid = (a + b) * 0.5f;

        Vector2 dir = (b - a).normalized;
        Vector2 normal = new Vector2(-dir.y, dir.x);

        Vector2 control = mid + normal * branch.curveAmount;

        const int segments = 40;

        Vector2 previous = a;

        for (int i = 1; i <= segments; i++)
        {
            float t = i / (float)segments;

            Vector2 current = GetBezierPoint(a, control, b, t);

            PaintSegment(map, previous, current, branch.width);

            previous = current;
        }
    }
    private float DistancePointToSegment(Vector2 p, Vector2 a, Vector2 b)
    {
        Vector2 ab = b - a;
        float t = Vector2.Dot(p - a, ab) / ab.sqrMagnitude;

        t = Mathf.Clamp01(t);

        Vector2 closest = a + ab * t;

        return Vector2.Distance(p, closest);
    }
    private Vector2 GetBezierPoint(Vector2 a, Vector2 control, Vector2 b, float t)
    {
        float u = 1f - t;

        return
            u * u * a +
            2f * u * t * control +
            t * t * b;
    }
    private void PaintSegment(GeneratedMap map, Vector2 a, Vector2 b, float width)
    {
        float radius = width * 0.5f;

        int minX = Mathf.FloorToInt(Mathf.Min(a.x, b.x) - radius);
        int maxX = Mathf.CeilToInt(Mathf.Max(a.x, b.x) + radius);

        int minY = Mathf.FloorToInt(Mathf.Min(a.y, b.y) - radius);
        int maxY = Mathf.CeilToInt(Mathf.Max(a.y, b.y) + radius);

        minX = Mathf.Max(0, minX);
        minY = Mathf.Max(0, minY);

        maxX = Mathf.Min(map.mapWidth - 1, maxX);
        maxY = Mathf.Min(map.mapHeight - 1, maxY);

        for (int x = minX; x <= maxX; x++)
        {
            for (int y = minY; y <= maxY; y++)
            {
                Vector2 p = new Vector2(x + 0.5f, y + 0.5f);

                if (DistancePointToSegment(p, a, b) <= radius)
                {
                    map.walkable[x, y] = 1;
                }
            }
        }
    }
    private void SmoothRoad(GeneratedMap map)
    {
        int[,] copy = (int[,])map.walkable.Clone();

        for (int x = 1; x < map.mapWidth - 1; x++)
        {
            for (int y = 1; y < map.mapHeight - 1; y++)
            {
                if (copy[x, y] == 1)
                    continue;

                bool left = copy[x - 1, y] == 1;
                bool right = copy[x + 1, y] == 1;
                bool up = copy[x, y + 1] == 1;
                bool down = copy[x, y - 1] == 1;

                bool upLeft = copy[x - 1, y + 1] == 1;
                bool upRight = copy[x + 1, y + 1] == 1;
                bool downLeft = copy[x - 1, y - 1] == 1;
                bool downRight = copy[x + 1, y - 1] == 1;

                if (left && up && upLeft)
                {
                    map.walkable[x, y] = 1;
                    continue;
                }

                if (right && up && upRight)
                {
                    map.walkable[x, y] = 1;
                    continue;
                }

                if (left && down && downLeft)
                {
                    map.walkable[x, y] = 1;
                    continue;
                }

                if (right && down && downRight)
                {
                    map.walkable[x, y] = 1;
                }
            }
        }
    }
    private void GenerateSpawnPoints(GeneratedMap map)
    {
        map.spawnPoints.Clear();

        int targetCount = map.PlayableArea.width * map.PlayableArea.height / 150;
        const int maxTry = 5000;
        const float minDistance = 8f;

        RectInt area = map.PlayableArea;

        for (int i = 0; i < maxTry && map.spawnPoints.Count < targetCount; i++)
        {
            Vector2Int position = new(
                UnityEngine.Random.Range(area.xMin, area.xMax),
                UnityEngine.Random.Range(area.yMin, area.yMax));

            if (map.walkable[position.x, position.y] == 1)
                continue;

            if (IsInsideNode(map, position))
                continue;

            if (IsNearSpawnPoint(map, position, minDistance))
                continue;

            map.spawnPoints.Add(new MapSpawnPoint()
            {
                position = position,
                radius = minDistance
            });
        }

        Debug.Log($"Spawn Points: {map.spawnPoints.Count}");
    }
    private void AnalyzeSpawnPoints(GeneratedMap map)
    {
        foreach (MapSpawnPoint point in map.spawnPoints)
        {
            SpawnTag tags = map.biome.biomeTags;

            if (IsNearRoad(map, point.position, 4))
                tags |= SpawnTag.NearRoad;

            if (IsNearNode(map, point.position, 8))
                tags |= SpawnTag.NearNode;

            if (HasLargeArea(map, point.position, 3))
                tags |= SpawnTag.LargeArea;
            else
                tags |= SpawnTag.SmallArea;

            point.tags = tags;
        }
    }
    private bool IsInsideNode(GeneratedMap map, Vector2Int position)
    {
        foreach (MapNode node in map.nodes)
        {
            if (position.x >= node.Left &&
                position.x <= node.Right &&
                position.y >= node.Bottom &&
                position.y <= node.Top)
            {
                return true;
            }
        }

        return false;
    }
    private bool IsNearNode(GeneratedMap map, Vector2Int position, float minDistance)
    {
        foreach (MapNode node in map.nodes)
        {
            Vector2 nodeCenter = node.center;

            if (Vector2.Distance(position, nodeCenter) < minDistance)
                return true;
        }

        return false;
    }
    private bool IsNearRoad(GeneratedMap map, Vector2Int position, float minDistance)
    {
        int radius = Mathf.CeilToInt(minDistance);

        for (int x = position.x - radius; x <= position.x + radius; x++)
        {
            for (int y = position.y - radius; y <= position.y + radius; y++)
            {
                if (x < 0 || x >= map.mapWidth)
                    continue;

                if (y < 0 || y >= map.mapHeight)
                    continue;

                if (map.walkable[x, y] == 1)
                    return true;
            }
        }

        return false;
    }
    private bool HasLargeArea(GeneratedMap map, Vector2Int position, int radius)
    {
        int count = 0;

        for (int x = position.x - radius; x <= position.x + radius; x++)
        {
            for (int y = position.y - radius; y <= position.y + radius; y++)
            {
                if (x < 0 || x >= map.mapWidth)
                    continue;

                if (y < 0 || y >= map.mapHeight)
                    continue;

                if (map.walkable[x, y] == 1)
                    count++;
            }
        }

        return count > (radius * radius) / 2;
    }
    private bool IsNearSpawnPoint(GeneratedMap map, Vector2Int position, float minDistance)
    {
        foreach (MapSpawnPoint point in map.spawnPoints)
        {
            if (Vector2Int.Distance(position, point.position) < minDistance)
                return true;
        }

        return false;
    }
}
[Serializable]
public class GeneratedMap
{
    public int mapWidth;
    public int mapHeight;
    
    public int borderSize;
    public BiomeData biome;
    public List<MapSpawnPoint> spawnPoints = new();
    public RectInt PlayableArea =>
        new RectInt(
            borderSize,
            borderSize,
            mapWidth - borderSize * 2,
            mapHeight - borderSize * 2);

    public int[,] walkable;

    public List<MapNode> nodes = new();
    public List<MapBranch> branches = new();
}

[Serializable]
public class MapNode
{
    public string id;
    public TriggerData triggerData;

    // Tâm của node
    public Vector2Int center;

    // Kích thước
    public int width;
    public int height;
    public int Left => center.x - width / 2;
    public int Right => center.x + width / 2;
    public int Bottom => center.y - height / 2;
    public int Top => center.y + height / 2;
}

[Serializable]
public class MapBranch
{
    public string fromNodeID;
    public string toNodeID;
    public float curveAmount;
    public int width;
}
[Serializable]
public class MapSpawnPoint
{
    public Vector2Int position;

    public float radius = 4f;

    public SpawnTag tags;
}
[Flags]
public enum SpawnTag
{
    None = 0,

    Forest = 1 << 0,
    Desert = 1 << 1,
    Snow = 1 << 2,

    NearRoad = 1 << 3,
    NearNode = 1 << 4,

    LargeArea = 1 << 5,
    SmallArea = 1 << 6,

    Rocky = 1 << 7,
    Grass = 1 << 8,
    Water = 1 << 9
}