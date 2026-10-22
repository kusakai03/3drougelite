using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using System;
public class MapSpawner : MonoBehaviour
{
    [SerializeField] private Transform mapObstacleParent;
    [SerializeField] private TerrainMeshGenerator terrainGenerator;
    private const int MaxInstancePerBatch = 1023;
    private Matrix4x4[] batch = new Matrix4x4[1023];
    private const int DecorationChunkSize = 50;
    private readonly Dictionary<Vector2Int,Dictionary<DecorationData, List<Matrix4x4>>> decorationChunks = new();
    [SerializeField] private float obstacleFadeStart = 40f;
    [SerializeField] private float obstacleFadeEnd = 60f;
    private const int ObstacleChunkSize = 50;
    private readonly Dictionary<Vector2Int, ObstacleChunk> obstacleChunks = new();
    private readonly HashSet<Vector2Int> activeObstacleChunks = new();
    private class ObstacleChunk
    {
        public List<Transform> obstacles = new();
    }
    private readonly Dictionary<Transform, Renderer[]> obstacleRenderers = new();
    private readonly Dictionary<Transform, Collider[]> obstacleColliders = new();
    [SerializeField] private float decorationDensity = 0.35f;
    private MaterialPropertyBlock propertyBlock;
    private readonly List<Dictionary<DecorationData, List<Matrix4x4>>> activeDecorationChunks = new();
    private Vector2Int lastDecorationChunk;
    private bool hasDecorationChunk = false;
    [SerializeField] private int borderChunkSize = 50;
    [SerializeField] private int borderChunkRadius = 2;
    private readonly Dictionary<Vector2Int, Dictionary<ObstacleData, List<Matrix4x4>>> borderObstacleChunks = new();
    private readonly List<KeyValuePair<Vector2Int, Dictionary<ObstacleData, List<Matrix4x4>>>> activeBorderChunks = new();
    private Vector2Int lastBorderChunk;
    private bool hasBorderChunk;
    [SerializeField] private float borderObstacleDensity = 0.08f;
    private static readonly int PlayerPositionID = Shader.PropertyToID("_PlayerPos");
    [SerializeField] private Transform triggerParent;
    private const int TriggerChunkSize = 50;
    private readonly Dictionary<Vector2Int, Transform> triggerChunks = new();

    private Vector2Int lastTriggerChunk;
    private bool hasTriggerChunk;
    
    private void Awake()
    {
        propertyBlock = new MaterialPropertyBlock();

        InvokeRepeating(
            nameof(UpdateObstacleVisibility),
            0f,
            0.1f
        );
    }
    private void UpdateObstacleVisibility()
    {
        GameObject player = PlayerManager.Instance.LocalPlayer;

        if (player == null)
            return;

        Vector3 playerPosition = player.transform.position;

        Vector2Int currentChunk = new Vector2Int(
            Mathf.FloorToInt(playerPosition.x / ObstacleChunkSize),
            Mathf.FloorToInt(playerPosition.z / ObstacleChunkSize)
        );

        // FadeEnd = 60, Chunk = 50
        // => cần kiểm tra tối thiểu 2 chunk xung quanh
        int chunkRadius = Mathf.CeilToInt(
            obstacleFadeEnd / ObstacleChunkSize
        );

        HashSet<Vector2Int> newActiveChunks = new();

        for (int cy = -chunkRadius; cy <= chunkRadius; cy++)
        {
            for (int cx = -chunkRadius; cx <= chunkRadius; cx++)
            {
                Vector2Int coord =
                    currentChunk + new Vector2Int(cx, cy);

                newActiveChunks.Add(coord);
            }
        }

        // Tắt các chunk không còn nằm trong vùng active
        foreach (Vector2Int oldChunk in activeObstacleChunks)
        {
            if (newActiveChunks.Contains(oldChunk))
                continue;

            if (!obstacleChunks.TryGetValue(
                oldChunk,
                out ObstacleChunk chunk))
                continue;

            DisableChunk(chunk);
        }

        // Update các chunk hiện tại
        foreach (Vector2Int coord in newActiveChunks)
        {
            if (!obstacleChunks.TryGetValue(
                coord,
                out ObstacleChunk chunk))
                continue;

            UpdateChunkVisibility(
                chunk,
                playerPosition
            );
        }

        activeObstacleChunks.Clear();

        foreach (Vector2Int coord in newActiveChunks)
        {
            activeObstacleChunks.Add(coord);
        }
    }
    private void DisableChunk(ObstacleChunk chunk)
    {
        foreach (Transform obstacle in chunk.obstacles)
        {
            if (obstacleRenderers.TryGetValue(
                obstacle,
                out Renderer[] renderers))
            {
                foreach (Renderer renderer in renderers)
                {
                    renderer.enabled = false;
                }
            }

            if (obstacleColliders.TryGetValue(
                obstacle,
                out Collider[] colliders))
            {
                foreach (Collider collider in colliders)
                {
                    collider.enabled = false;
                }
            }
        }
    }
    private void UpdateChunkVisibility(ObstacleChunk chunk, Vector3 playerPosition)
    {
        float fadeStartSqr = obstacleFadeStart * obstacleFadeStart;

        float fadeEndSqr = obstacleFadeEnd * obstacleFadeEnd;

        foreach (Transform obstacle in chunk.obstacles)
        {
            float sqrDistance = (playerPosition - obstacle.position).sqrMagnitude;

            if (!obstacleRenderers.TryGetValue(obstacle, out Renderer[] renderers))
                continue;

            obstacleColliders.TryGetValue(obstacle, out Collider[] colliders);

            if (sqrDistance >= fadeEndSqr)
            {
                foreach (Renderer renderer in renderers)
                    renderer.enabled = false;

                if (colliders != null)
                {
                    foreach (Collider collider in colliders)
                        collider.enabled = false;
                }

                continue;
            }

            float fade = 1f;

            if (sqrDistance > fadeStartSqr)
            {
                float distance = Mathf.Sqrt(sqrDistance);

                fade = 1f - Mathf.InverseLerp(
                    obstacleFadeStart,
                    obstacleFadeEnd,
                    distance);
            }

            foreach (Renderer renderer in renderers)
            {
                renderer.enabled = true;

                for (int i = 0;
                    i < renderer.sharedMaterials.Length;
                    i++)
                {
                    renderer.GetPropertyBlock(
                        propertyBlock,
                        i);

                    propertyBlock.SetFloat(
                        "_Fade",
                        fade);

                    renderer.SetPropertyBlock(
                        propertyBlock,
                        i);
                }
            }

            if (colliders != null)
            {
                foreach (Collider collider in colliders)
                    collider.enabled = true;
            }
        }
    }
    public IEnumerator Spawn(GeneratedMap map)
    {
        SpawnObstacles(map);
        yield return null;

        float t = Time.realtimeSinceStartup;
        yield return SpawnDecorations(map);
        Debug.Log($"Decorations: {(Time.realtimeSinceStartup - t) * 1000f} ms");
        yield return null;
        SpawnBorderObstacles(map);
        yield return null;
        SpawnTriggers(map);

    }
    private void LateUpdate()
    {
        GameObject player = PlayerManager.Instance.LocalPlayer;
        if (player == null)
            return;
        Shader.SetGlobalVector(PlayerPositionID, player.transform.position);
        Vector2Int currentChunk = new Vector2Int(
            Mathf.FloorToInt(player.transform.position.x / DecorationChunkSize),
            Mathf.FloorToInt(player.transform.position.z / DecorationChunkSize));

        if (!hasDecorationChunk || currentChunk != lastDecorationChunk)
        {
            lastDecorationChunk = currentChunk;
            hasDecorationChunk = true;
            UpdateActiveDecorationChunks(currentChunk);
        }

        RenderActiveDecorations();
        RenderBorderObstacles();
        UpdateTriggerChunks();
    }
    private void SpawnObstacles(GeneratedMap map)
    {
        foreach (Transform child in mapObstacleParent)
        {
            Destroy(child.gameObject);
        }
        obstacleRenderers.Clear();
        obstacleColliders.Clear();
        obstacleChunks.Clear();
        activeObstacleChunks.Clear();
        foreach (MapSpawnPoint point in map.spawnPoints)
        {
            ObstacleData obstacle = FindObstacle(point, map.biome);

            if (obstacle == null)
                continue;
            float worldY = terrainGenerator.GetHeightAt(map, point.position.x, point.position.y);
            Quaternion rotation = Quaternion.Euler(0f, UnityEngine.Random.Range(0f, 360f), 0f);
            GameObject obj = Instantiate(obstacle.prefab,
            new Vector3(point.position.x + 0.5f, worldY, point.position.y + 0.5f),
            rotation, mapObstacleParent);
            Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();

            Collider[] colliders = obj.GetComponentsInChildren<Collider>();

            bool needCollider =
                (point.tags & SpawnTag.NearNode) != 0 ||
                (point.tags & SpawnTag.NearRoad) != 0;

            if (needCollider)
            {
                MeshFilter meshFilter = obj.GetComponentInChildren<MeshFilter>();

                if (meshFilter != null && obstacle.colliderMesh != null)
                {
                    MeshCollider collider = meshFilter.GetComponent<MeshCollider>();

                    if (collider == null)
                        collider = meshFilter.gameObject.AddComponent<MeshCollider>();

                    collider.sharedMesh = obstacle.colliderMesh;
                }

                colliders = obj.GetComponentsInChildren<Collider>();
            }
            else
            {
                colliders = System.Array.Empty<Collider>();
            }

            // Ban đầu tắt hết
            foreach (Renderer renderer in renderers)
            {
                renderer.enabled = false;
            }

            foreach (Collider collider in colliders)
            {
                collider.enabled = false;
            }

            obstacleRenderers[obj.transform] = renderers;
            obstacleColliders[obj.transform] = colliders;
            Vector2Int chunkCoord = new Vector2Int(
                Mathf.FloorToInt(point.position.x / ObstacleChunkSize),
                Mathf.FloorToInt(point.position.y / ObstacleChunkSize)
            );
            if (!obstacleChunks.TryGetValue(chunkCoord, out ObstacleChunk chunk))
            {
                chunk = new ObstacleChunk();
                obstacleChunks.Add(chunkCoord, chunk);
            }

            chunk.obstacles.Add(obj.transform);
        }
    }
    private ObstacleData FindObstacle(MapSpawnPoint point, BiomeData biome)
    {
        List<ObstacleData> candidates = new();

        foreach (ObstacleData obstacle in biome.obstacles)
        {
            if ((point.tags & obstacle.requiredTags) != obstacle.requiredTags)
                continue;

            if ((point.tags & obstacle.forbiddenTags) != 0)
                continue;

            candidates.Add(obstacle);
        }

        if (candidates.Count == 0)
            return null;

        return candidates[UnityEngine.Random.Range(0, candidates.Count)];
    }
    private IEnumerator SpawnDecorations(GeneratedMap map)
    {
        decorationChunks.Clear();

        int attempts = Mathf.RoundToInt(map.mapWidth * map.mapHeight * decorationDensity);
        const int batchSize = 5000;

        for (int i = 0; i < attempts; i++)
        {
            float x = UnityEngine.Random.Range(0f, map.mapWidth);
            float y = UnityEngine.Random.Range(0f, map.mapHeight);

            int cellX = Mathf.FloorToInt(x);
            int cellY = Mathf.FloorToInt(y);

            if (cellX >= 0 && cellX < map.mapWidth && cellY >= 0 && cellY < map.mapHeight)
            {
                int groundType = map.walkable[cellX, cellY];
                SpawnDecorationPoint(map, x, y, groundType);
            }

            if ((i + 1) % batchSize == 0)
            {
                hasDecorationChunk = false;
                yield return null;
            }
        }
    }
    private void SpawnDecorationPoint(GeneratedMap map, float x, float y, int groundType)
    {
        List<BiomeDecoration> decorations = groundType == 1
            ? map.biome.walkableDecorations
            : map.biome.decorations;

        List<BiomeDecoration> candidates = new();

        foreach (BiomeDecoration decor in decorations)
        {
            float noise = Mathf.PerlinNoise(x * decor.noiseScale + decor.noiseOffset.x, y * decor.noiseScale + decor.noiseOffset.y);

            if (noise < decor.minNoise || noise > decor.maxNoise)
                continue;

            if (UnityEngine.Random.value > decor.density)
                continue;

            candidates.Add(decor);
        }

        if (candidates.Count == 0)
            return;

        BiomeDecoration decorData = candidates[UnityEngine.Random.Range(0, candidates.Count)];

        Quaternion rotation = Quaternion.Euler(0f, UnityEngine.Random.Range(0f, 360f), 0f);
        float worldY = terrainGenerator.GetHeightAt(map, x, y);

        Matrix4x4 matrix = Matrix4x4.TRS(new Vector3(x, worldY, y), rotation, Vector3.one);

        Vector2Int chunkCoord = new Vector2Int(Mathf.FloorToInt(x / DecorationChunkSize), Mathf.FloorToInt(y / DecorationChunkSize));

        if (!decorationChunks.TryGetValue(chunkCoord, out Dictionary<DecorationData, List<Matrix4x4>> chunk))
        {
            chunk = new Dictionary<DecorationData, List<Matrix4x4>>();
            decorationChunks.Add(chunkCoord, chunk);
        }

        if (!chunk.TryGetValue(decorData.decoration, out List<Matrix4x4> list))
        {
            list = new List<Matrix4x4>();
            chunk.Add(decorData.decoration, list);
        }

        list.Add(matrix);
    }
    private void RenderActiveDecorations()
    {
        foreach (Dictionary<DecorationData, List<Matrix4x4>> chunk in activeDecorationChunks)
        {
            foreach (var pair in chunk)
            {
                DecorationData decoration = pair.Key;
                List<Matrix4x4> rootMatrices = pair.Value;

                MeshFilter[] meshFilters = decoration.prefab.GetComponentsInChildren<MeshFilter>(true);

                foreach (MeshFilter meshFilter in meshFilters)
                {
                    MeshRenderer meshRenderer = meshFilter.GetComponent<MeshRenderer>();

                    if (meshRenderer == null || meshFilter.sharedMesh == null)
                        continue;

                    Mesh mesh = meshFilter.sharedMesh;
                    Material[] materials = meshRenderer.sharedMaterials;

                    Matrix4x4 childLocalMatrix = decoration.prefab.transform.worldToLocalMatrix * meshFilter.transform.localToWorldMatrix;

                    for (int subMesh = 0; subMesh < mesh.subMeshCount; subMesh++)
                    {
                        if (subMesh >= materials.Length || materials[subMesh] == null)
                            continue;

                        RenderParams renderParams = new(materials[subMesh]);
                        renderParams.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
                        renderParams.receiveShadows = true;

                        for (int i = 0; i < rootMatrices.Count; i += MaxInstancePerBatch)
                        {
                            int count = Mathf.Min(MaxInstancePerBatch, rootMatrices.Count - i);

                            for (int j = 0; j < count; j++)
                                batch[j] = rootMatrices[i + j] * childLocalMatrix;

                            Graphics.RenderMeshInstanced(renderParams, mesh, subMesh, batch, count);
                        }
                    }
                }
            }
        }
    }

    private void UpdateActiveDecorationChunks(Vector2Int currentChunk)
    {
        activeDecorationChunks.Clear();

        int radius = 2;

        for (int cy = -radius; cy <= radius; cy++)
        {
            for (int cx = -radius; cx <= radius; cx++)
            {
                Vector2Int coord = currentChunk + new Vector2Int(cx, cy);

                if (decorationChunks.TryGetValue(coord, out Dictionary<DecorationData, List<Matrix4x4>> chunk))
                    activeDecorationChunks.Add(chunk);
            }
        }
    }
    
    private void SpawnBorderObstacles(GeneratedMap map)
    {
        borderObstacleChunks.Clear();
        RectInt playable = map.PlayableArea;

        for (int z = 0; z < map.mapHeight; z++)
        {
            for (int x = 0; x < map.mapWidth; x++)
            {
                if (playable.Contains(new Vector2Int(x, z)))
                    continue;
                if (UnityEngine.Random.value > borderObstacleDensity)
                    continue;
                SpawnBorderObstaclePoint(map, x, z);
            }
        }

        hasBorderChunk = false;
        activeBorderChunks.Clear();
    }
    private void SpawnBorderObstaclePoint(GeneratedMap map, int x, int z)
    {
        if (map.biome.obstacles == null || map.biome.obstacles.Count == 0)
            return;

        ObstacleData obstacle = map.biome.obstacles[UnityEngine.Random.Range(0, map.biome.obstacles.Count)];

        if (obstacle == null || obstacle.prefab == null)
            return;

        float worldX = x + UnityEngine.Random.Range(0.1f, 0.9f);
        float worldZ = z + UnityEngine.Random.Range(0.1f, 0.9f);
        float worldY = terrainGenerator.GetHeightAt(map, worldX, worldZ);

        Matrix4x4 matrix = Matrix4x4.TRS(
            new Vector3(worldX, worldY, worldZ),
            Quaternion.Euler(0f, UnityEngine.Random.Range(0f, 360f), 0f),
            Vector3.one);

        Vector2Int chunkCoord = new Vector2Int(
            x / borderChunkSize,
            z / borderChunkSize);

        if (!borderObstacleChunks.TryGetValue(chunkCoord, out var chunk))
        {
            chunk = new Dictionary<ObstacleData, List<Matrix4x4>>();
            borderObstacleChunks.Add(chunkCoord, chunk);
        }

        if (!chunk.TryGetValue(obstacle, out var matrices))
        {
            matrices = new List<Matrix4x4>();
            chunk.Add(obstacle, matrices);
        }

        matrices.Add(matrix);
    }
    private void UpdateActiveBorderChunks(Vector2Int center)
    {
        activeBorderChunks.Clear();

        for (int z = -borderChunkRadius; z <= borderChunkRadius; z++)
        {
            for (int x = -borderChunkRadius; x <= borderChunkRadius; x++)
            {
                Vector2Int coord = center + new Vector2Int(x, z);

                if (borderObstacleChunks.TryGetValue(coord, out var chunk))
                {
                    activeBorderChunks.Add(
                        new KeyValuePair<Vector2Int, Dictionary<ObstacleData, List<Matrix4x4>>>(coord, chunk));
                }
            }
        }
    }
    private void RenderBorderObstacles()
    {
        GameObject player = PlayerManager.Instance.LocalPlayer;

        if (player == null)
            return;

        Vector2Int currentChunk = new Vector2Int(
            Mathf.FloorToInt(player.transform.position.x / borderChunkSize),
            Mathf.FloorToInt(player.transform.position.z / borderChunkSize));

        if (!hasBorderChunk || currentChunk != lastBorderChunk)
        {
            lastBorderChunk = currentChunk;
            hasBorderChunk = true;
            UpdateActiveBorderChunks(currentChunk);
        }

        foreach (var activeChunk in activeBorderChunks)
        {
            Vector2Int chunkCoord = activeChunk.Key;
            Dictionary<ObstacleData, List<Matrix4x4>> chunk = activeChunk.Value;

            float distance = Vector3.Distance(
                player.transform.position,
                GetChunkCenter(chunkCoord, borderChunkSize));

            float fade = 1f - Mathf.InverseLerp(
                obstacleFadeStart,
                obstacleFadeEnd,
                distance);

            if (distance >= obstacleFadeEnd)
                continue;

            foreach (var pair in chunk)
            {
                ObstacleData obstacle = pair.Key;
                List<Matrix4x4> matrices = pair.Value;

                MeshFilter meshFilter = obstacle.prefab.GetComponentInChildren<MeshFilter>();

                MeshRenderer meshRenderer = obstacle.prefab.GetComponentInChildren<MeshRenderer>();

                if (meshFilter == null || meshRenderer == null)
                    continue;

                Mesh mesh = meshFilter.sharedMesh;
                Material[] materials = meshRenderer.sharedMaterials;

                for (int subMesh = 0; subMesh < mesh.subMeshCount; subMesh++)
                {
                    if (subMesh >= materials.Length)
                        continue;

                    RenderParams renderParams = new RenderParams(materials[subMesh]);

                    renderParams.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;

                    renderParams.receiveShadows = true;

                    MaterialPropertyBlock propertyBlock = new MaterialPropertyBlock();

                    propertyBlock.SetFloat("_Fade", fade);

                    renderParams.matProps = propertyBlock;

                    for (int i = 0; i < matrices.Count; i += MaxInstancePerBatch)
                    {
                        int count = Mathf.Min(MaxInstancePerBatch, matrices.Count - i);

                        for (int j = 0; j < count; j++)
                        {
                            batch[j] = matrices[i + j];
                        }

                        Graphics.RenderMeshInstanced(renderParams, mesh, subMesh, batch, count);
                    }
                }
            }
        }
    }
    private Vector3 GetChunkCenter(Vector2Int chunkCoord, int chunkSize)
    {
        float centerX = chunkCoord.x * chunkSize + chunkSize * 0.5f;
        float centerZ = chunkCoord.y * chunkSize + chunkSize * 0.5f;

        return new Vector3(centerX, 0f, centerZ);
    }
    private void SpawnTriggers(GeneratedMap map)
    {
        foreach (Transform child in triggerParent)
            Destroy(child.gameObject);

        triggerChunks.Clear();
        hasTriggerChunk = false;

        foreach (MapNode node in map.nodes)
        {
            if (node.triggerData == null)
                continue;

            float worldY = terrainGenerator.GetHeightAt(map, node.center.x, node.center.y);
            Vector3 pos = new Vector3(node.center.x, worldY + 1f, node.center.y);

            Vector2Int chunkCoord = new Vector2Int(Mathf.FloorToInt(node.center.x / TriggerChunkSize), Mathf.FloorToInt(node.center.y / TriggerChunkSize));

            Transform chunkParent = GetOrCreateTriggerChunk(chunkCoord);

            GameObject obj = new GameObject($"Trigger_{node.id}");
            obj.transform.SetParent(chunkParent);
            obj.transform.position = new Vector3(node.center.x, worldY, node.center.y);

            TriggerInstance instance = obj.AddComponent<TriggerInstance>();

            instance.Initialize(node.id, node.triggerData, pos, obj.transform.rotation, chunkParent);
        }
    }

    private Transform GetOrCreateTriggerChunk(Vector2Int coord)
    {
        if (triggerChunks.TryGetValue(coord, out Transform chunk))
            return chunk;

        GameObject obj = new GameObject($"TriggerChunk_{coord.x}_{coord.y}");
        obj.transform.SetParent(triggerParent);

        chunk = obj.transform;
        triggerChunks.Add(coord, chunk);

        return chunk;
    }
    
    private void UpdateTriggerChunks()
    {
        GameObject player = PlayerManager.Instance.LocalPlayer;

        if (player == null)
            return;

        Vector2Int currentChunk = new Vector2Int(Mathf.FloorToInt(player.transform.position.x / TriggerChunkSize), Mathf.FloorToInt(player.transform.position.z / TriggerChunkSize));

        if (hasTriggerChunk && currentChunk == lastTriggerChunk)
            return;

        lastTriggerChunk = currentChunk;
        hasTriggerChunk = true;

        int radius = Mathf.CeilToInt(obstacleFadeEnd / TriggerChunkSize);

        foreach (var pair in triggerChunks)
        {
            Vector2Int coord = pair.Key;
            Transform chunk = pair.Value;

            bool visible = Mathf.Abs(coord.x - currentChunk.x) <= radius && Mathf.Abs(coord.y - currentChunk.y) <= radius;

            Renderer[] renderers = chunk.GetComponentsInChildren<Renderer>(true);

            foreach (Renderer renderer in renderers)
                renderer.enabled = visible;
        }
    }

    public void ActivateAllTriggers()
    {
        TriggerInstance[] triggers = triggerParent.GetComponentsInChildren<TriggerInstance>();

        foreach (TriggerInstance trigger in triggers)
        {
            if (trigger == null)
                continue;
            if (trigger.triggerData.type == TriggerType.Portal)
                continue;
            trigger.Activate();
        }
    }
    
    public List<GameObject> SpawnEntities(List<EntityData> entities, MapNode node, GeneratedMap map)
    {
        List<GameObject> spawned = new();

        foreach (EntityData entityData in entities)
        {
            if (entityData == null || entityData.prefab == null)
                continue;

            float x = UnityEngine.Random.Range(node.Left + 0.5f, node.Right - 0.5f);
            float z = UnityEngine.Random.Range(node.Bottom + 0.5f, node.Top - 0.5f);
            float y = terrainGenerator.GetHeightAt(map, x, z);

            GameObject obj = Instantiate(entityData.prefab, new Vector3(x, y + 1, z), Quaternion.Euler(0f, UnityEngine.Random.Range(0f, 360f), 0f));

            int entityLevel = StageManager.Instance.CalculateEntityLevel();
            CombatStats stats = StageManager.Instance.CalculateEntityStats(entityData, entityLevel);

            EntityInstance entityInstance = obj.GetComponent<EntityInstance>();

            if (entityInstance == null)
                entityInstance = obj.AddComponent<EntityInstance>();

            entityInstance.Initialize(entityData, entityLevel);

            CombatInstance combatInstance = obj.GetComponent<CombatInstance>();

            if (combatInstance == null)
                combatInstance = obj.AddComponent<CombatInstance>();
            string instanceID = $"{entityData.id}_{Guid.NewGuid()}";
            string instanceRace = "entity";
            combatInstance.Initialize(instanceID, instanceRace, stats);

            spawned.Add(obj);
        }

        return spawned;
    }
    
}
