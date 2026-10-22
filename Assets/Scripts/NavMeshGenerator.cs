using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using Unity.AI.Navigation;
using UnityEngine.Rendering;

public class NavMeshGenerator : MonoBehaviour
{
    [SerializeField] private NavMeshSurface navMeshSurface;
    [SerializeField] private TerrainMeshGenerator terrainGenerator;
    private GameObject walkableObject;
    private Mesh walkableMesh;

    private NavMeshData navMeshData;
    private NavMeshDataInstance navMeshInstance;

    public IEnumerator BakeNavMesh(GeneratedMap map)
    {
        walkableMesh = CreateWalkableMesh(map);

        if (navMeshInstance.valid)
            navMeshInstance.Remove();

        if (navMeshData != null)
            Destroy(navMeshData);

        NavMeshBuildSettings settings = navMeshSurface.GetBuildSettings();

        navMeshData = new NavMeshData(settings.agentTypeID);

        List<NavMeshBuildSource> sources = new();

        NavMeshBuildSource source = new NavMeshBuildSource
        {
            shape = NavMeshBuildSourceShape.Mesh,
            sourceObject = walkableMesh,
            transform = Matrix4x4.identity,
            area = 0
        };

        sources.Add(source);

        Bounds bounds = walkableMesh.bounds;
        bounds.Expand(new Vector3(0f, 20f, 0f));

        AsyncOperation operation = NavMeshBuilder.UpdateNavMeshDataAsync(navMeshData, settings, sources, bounds);

        yield return operation;

        navMeshInstance = NavMesh.AddNavMeshData(navMeshData);

        Destroy(walkableMesh);
        walkableMesh = null;
    }

    private Mesh CreateWalkableMesh(GeneratedMap map)
    {
        List<Vector3> vertices = new();
        List<int> triangles = new();

        for (int x = 0; x < map.mapWidth - 1; x++)
        {
            for (int z = 0; z < map.mapHeight - 1; z++)
            {
                if (map.walkable[x, z] != 1)
                    continue;

                int index = vertices.Count;

                float h00 = terrainGenerator.GetHeightAt(map, x, z);
                float h10 = terrainGenerator.GetHeightAt(map, x + 1, z);
                float h01 = terrainGenerator.GetHeightAt(map, x, z + 1);
                float h11 = terrainGenerator.GetHeightAt(map, x + 1, z + 1);

                vertices.Add(new Vector3(x, h00, z));
                vertices.Add(new Vector3(x + 1, h10, z));
                vertices.Add(new Vector3(x, h01, z + 1));
                vertices.Add(new Vector3(x + 1, h11, z + 1));

                triangles.Add(index);
                triangles.Add(index + 2);
                triangles.Add(index + 1);

                triangles.Add(index + 2);
                triangles.Add(index + 3);
                triangles.Add(index + 1);
            }
        }

        Mesh mesh = new Mesh();
        mesh.indexFormat = IndexFormat.UInt32;
        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateBounds();
        mesh.RecalculateNormals();

        return mesh;
    }
}