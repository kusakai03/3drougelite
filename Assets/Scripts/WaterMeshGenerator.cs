using UnityEngine;
using UnityEngine.Rendering;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class WaterMeshGenerator : MonoBehaviour
{
    [SerializeField] private float vertexSpacing = 1f;

    private Mesh mesh;

    public void Generate(GeneratedMap map)
    {
        Clear();

        if (map == null || map.biome == null || !map.biome.hasWater)
        {
            gameObject.SetActive(false);
            return;
        }

        gameObject.SetActive(true);

        float mapWidth = map.mapWidth;
        float mapHeight = map.mapHeight;

        int width = Mathf.CeilToInt(mapWidth / vertexSpacing);
        int height = Mathf.CeilToInt(mapHeight / vertexSpacing);

        int vertexWidth = width + 1;
        int vertexHeight = height + 1;

        Vector3[] vertices = new Vector3[vertexWidth * vertexHeight];
        Vector2[] uvs = new Vector2[vertices.Length];
        int[] triangles = new int[width * height * 6];

        int vertexIndex = 0;

        for (int y = 0; y < vertexHeight; y++)
        {
            for (int x = 0; x < vertexWidth; x++)
            {
                float worldX = Mathf.Min(x * vertexSpacing, mapWidth);
                float worldZ = Mathf.Min(y * vertexSpacing, mapHeight);

                vertices[vertexIndex] = new Vector3(worldX, 0f, worldZ);

                uvs[vertexIndex] = new Vector2(
                    worldX / mapWidth,
                    worldZ / mapHeight
                );

                vertexIndex++;
            }
        }

        int triangleIndex = 0;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int i = y * vertexWidth + x;

                triangles[triangleIndex++] = i;
                triangles[triangleIndex++] = i + vertexWidth;
                triangles[triangleIndex++] = i + 1;

                triangles[triangleIndex++] = i + 1;
                triangles[triangleIndex++] = i + vertexWidth;
                triangles[triangleIndex++] = i + vertexWidth + 1;
            }
        }

        mesh = new Mesh();
        mesh.name = "GeneratedWaterMesh";
        mesh.indexFormat = IndexFormat.UInt32;

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uvs;

        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        GetComponent<MeshFilter>().sharedMesh = mesh;

        transform.position = new Vector3(0f, map.biome.waterLevel, 0f);
    }

    private void Clear()
    {
        if (mesh != null)
        {
            Destroy(mesh);
            mesh = null;
        }

        MeshFilter filter = GetComponent<MeshFilter>();

        if (filter != null)
            filter.sharedMesh = null;
    }
}