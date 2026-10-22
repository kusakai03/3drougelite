using UnityEngine;
using System.Collections.Generic;
using System;
using System.Linq;

public class GrassMesh : MonoBehaviour
{
    public float grassHeight = 1f;
    public float grassWidth = 0.5f;
    [SerializeField] private int chunkSize = 32;
    [SerializeField] private Material grassMaterial;
    
    // LOD Settings
    [Header("LOD Settings (Screen Relative Size 0.0 - 1.0)")]
    // Nhập từ lớn đến nhỏ: 0.6 = 60% màn hình, 0.2 = 20% màn hình, 0.05 = 5% màn hình
    [SerializeField] private float[] lodDistances = new float[] { 0.6f, 0.2f, 0.05f };
    [SerializeField] private float[] lodDensities = new float[] { 1f, 0.5f, 0.2f };
    [SerializeField] private bool enableLOD = true;
    
    private Dictionary<Vector2Int, LODGroup> lodGroups = new();
    private Camera mainCamera;
    
    private void Start()
    {
        mainCamera = Camera.main;
    }
    
    private void Update()
    {
        if (enableLOD)
        {
            UpdateLODs();
        }
    }
    
    public void Generate(GeneratedMap map, float cellSize = 1f)
    {
        ClearChildren();
        
        int chunkCountX = Mathf.CeilToInt((float)map.mapWidth / chunkSize);
        int chunkCountY = Mathf.CeilToInt((float)map.mapHeight / chunkSize);
        
        for (int cy = 0; cy < chunkCountY; cy++)
        {
            for (int cx = 0; cx < chunkCountX; cx++)
            {
                GenerateChunkWithLOD(map, cx, cy);
            }
        }
    }
    
    private void GenerateChunkWithLOD(GeneratedMap map, int chunkX, int chunkY)
    {
        GameObject chunkObject = new($"GrassChunk_{chunkX}_{chunkY}");
        chunkObject.transform.SetParent(transform, false);
        
        LODGroup lodGroup = chunkObject.AddComponent<LODGroup>();
        lodGroup.fadeMode = LODFadeMode.CrossFade;
        
        List<LOD> lods = new List<LOD>();

        // Đảm bảo dữ liệu lodDistances được sắp xếp giảm dần từ lớn đến nhỏ (ví dụ: 0.6 -> 0.2 -> 0.05)
        var sortedLODSpecs = lodDistances
            .Select((dist, index) => new { Distance = dist, Density = lodDensities[index] })
            .OrderByDescending(x => x.Distance)
            .ToArray();
        
        for (int lodLevel = 0; lodLevel < sortedLODSpecs.Length; lodLevel++)
        {
            float currentDensity = sortedLODSpecs[lodLevel].Density;
            float currentScreenSize = sortedLODSpecs[lodLevel].Distance;

            Mesh lodMesh = GenerateLODMesh(map, chunkX, chunkY, lodLevel, currentDensity);
            
            if (lodMesh != null && lodMesh.vertexCount > 0)
            {
                GameObject lodObject = CreateLODObject(chunkObject, lodLevel, lodMesh);
                Renderer renderer = lodObject.GetComponent<Renderer>();
                
                lods.Add(new LOD(currentScreenSize, new Renderer[] { renderer }));
            }
            else
            {
                lods.Add(new LOD(currentScreenSize, new Renderer[0]));
            }
        }
        
        lodGroup.SetLODs(lods.ToArray());
        lodGroup.RecalculateBounds();
        
        lodGroups[new Vector2Int(chunkX, chunkY)] = lodGroup;
    }

    private Mesh GenerateLODMesh(GeneratedMap map, int chunkX, int chunkY, int lodLevel, float density)
    {
        Mesh mesh = new Mesh();
        mesh.name = $"Grass_LOD{lodLevel}_{chunkX}_{chunkY}";
        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        
        List<Vector3> vertices = new();
        List<int> triangles = new();
        List<Vector2> uvs = new();
        
        int startX = chunkX * chunkSize;
        int startY = chunkY * chunkSize;
        
        System.Random random = new System.Random(chunkX * 1000 + chunkY * 100 + lodLevel);
        
        for (int y = 0; y < chunkSize; y++)
        {
            for (int x = 0; x < chunkSize; x++)
            {
                int mapX = startX + x;
                int mapY = startY + y;
                
                if (mapX >= map.mapWidth || mapY >= map.mapHeight)
                    continue;
                    
                if (map.walkable[mapX, mapY] == 1)
                    continue;
                
                if (random.NextDouble() > density)
                    continue;
                
                int patchCount = lodLevel == 0 ? 
                    random.Next(2, 5) : 
                    Mathf.Max(1, random.Next(2, 5) - lodLevel);
                
                for (int i = 0; i < patchCount; i++)
                {
                    Vector3 center = new(
                        mapX + (float)random.NextDouble(),
                        0,
                        mapY + (float)random.NextDouble());
                    
                    float widthMultiplier = Mathf.Lerp(1f, 0.5f, lodLevel / (float)lodDistances.Length);
                    float heightMultiplier = Mathf.Lerp(1f, 0.7f, lodLevel / (float)lodDistances.Length);
                    
                    AddGrassCross(center, 
                        grassWidth * widthMultiplier, 
                        grassHeight * heightMultiplier, 
                        (float)random.NextDouble() * 360f, 
                        vertices, triangles, uvs);
                }
            }
        }
        
        if (vertices.Count == 0)
            return null;
            
        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles, 0);
        mesh.SetUVs(0, uvs);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        
        return mesh;
    }

    private void UpdateLODs()
    {
        if (mainCamera == null)
            return;
            
        Vector3 cameraPos = mainCamera.transform.position;
        
        foreach (var kvp in lodGroups)
        {
            Vector2Int chunkPos = kvp.Key;
            Vector3 chunkCenter = new Vector3(
                chunkPos.x * chunkSize + chunkSize * 0.5f,
                0,
                chunkPos.y * chunkSize + chunkSize * 0.5f
            );
            
            float distance = Vector3.Distance(cameraPos, chunkCenter);
            
            // Xử lý culling dựa trên khoảng cách mét thủ công
            if (distance > 150f) 
            {
                kvp.Value.gameObject.SetActive(false);
            }
            else
            {
                kvp.Value.gameObject.SetActive(true);
            }
        }
    }
    
    private GameObject CreateLODObject(GameObject parent, int lodLevel, Mesh mesh)
    {
        GameObject lodObject = new($"LOD_{lodLevel}");
        lodObject.transform.SetParent(parent.transform, false);
        
        MeshFilter meshFilter = lodObject.AddComponent<MeshFilter>();
        meshFilter.sharedMesh = mesh;
        
        MeshRenderer meshRenderer = lodObject.AddComponent<MeshRenderer>();
        meshRenderer.sharedMaterial = grassMaterial;
        meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        
        return lodObject;
    }
    
    private void ClearChildren()
    {
        foreach (Transform child in transform)
        {
            Destroy(child.gameObject);
        }
        lodGroups.Clear();
    }
    private void AddGrassCross(Vector3 center, float width, float height, float angle, List<Vector3> vertices, List<int> triangles, List<Vector2> uvs)
    {
        int bladeCount = UnityEngine.Random.Range(7, 14);

        for (int i = 0; i < bladeCount; i++)
        {

            float bladeWidth = width * UnityEngine.Random.Range(0.3f, 0.7f);
            float bladeHeight = height * UnityEngine.Random.Range(0.7f, 1.2f);

            Vector2 circle = UnityEngine.Random.insideUnitCircle * width * 0.8f;

            Vector3 offset = new(circle.x, 0, circle.y);
            Vector3 bladeCenter = center + offset;

            float bladeAngle = Mathf.Atan2(offset.x, offset.z) * Mathf.Rad2Deg;
            bladeAngle += UnityEngine.Random.Range(-40f, 40f);

            AddBlade(bladeCenter, bladeWidth, bladeHeight, bladeAngle, vertices, triangles, uvs);
        }
    }
    private void AddBlade(Vector3 center, float width, float height, float angle, List<Vector3> vertices, List<int> triangles, List<Vector2> uvs)
    {
        int segmentCount = 1;

        Vector3 forward = Quaternion.Euler(0, angle, 0) * Vector3.forward;
        Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;

        float bendAmount = UnityEngine.Random.Range(-0.25f, 0.25f);

        for (int i = 0; i < segmentCount; i++)
        {
            float t0 = (float)i / segmentCount;
            float t1 = (float)(i + 1) / segmentCount;

            float h0 = t0 * height;
            float h1 = t1 * height;

            float w0 = Mathf.Lerp(width, 0.02f, t0);
            float w1 = Mathf.Lerp(width, 0f, t1);

            float b0 = Mathf.Sin(t0 * Mathf.PI * 0.5f) * bendAmount;
            float b1 = Mathf.Sin(t1 * Mathf.PI * 0.5f) * bendAmount;

            Vector3 c0 = center + forward * (b0 * 0.3f) + Vector3.up * h0 + right * b0;
            Vector3 c1 = center + forward * (b1 * 0.3f) + Vector3.up * h1 + right * b1;

            AddSegmentQuad(c0, c1, right, w0, w1, vertices, triangles, uvs);
        }
    }
    private void AddSegmentQuad(Vector3 bottom, Vector3 top, Vector3 right, float bottomWidth, float topWidth, List<Vector3> vertices, List<int> triangles, List<Vector2> uvs)
    {
        int start = vertices.Count;

        Vector3 bl = bottom - right * bottomWidth * 0.5f;
        Vector3 br = bottom + right * bottomWidth * 0.5f;

        Vector3 tl = top - right * topWidth * 0.5f;
        Vector3 tr = top + right * topWidth * 0.5f;

        vertices.Add(bl);
        vertices.Add(br);
        vertices.Add(tr);
        vertices.Add(tl);

        uvs.Add(new Vector2(0, 0));
        uvs.Add(new Vector2(1, 0));
        uvs.Add(new Vector2(1, 1));
        uvs.Add(new Vector2(0, 1));

        triangles.Add(start);
        triangles.Add(start + 2);
        triangles.Add(start + 1);

        triangles.Add(start);
        triangles.Add(start + 3);
        triangles.Add(start + 2);
    }
}
[Serializable]
public class GrassChunk
{
    public int chunkX;
    public int chunkY;

    public GameObject gameObject;

    public Mesh mesh;
}
