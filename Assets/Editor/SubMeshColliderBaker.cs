using UnityEngine;
using UnityEditor;
using Unity.Collections;
using UnityEngine.Rendering;

public static class SubMeshColliderBaker
{
    [MenuItem("Tools/Bake Collider/SubMesh 0 From Selected")]
    private static void BakeSelected()
    {
        GameObject selected = Selection.activeGameObject;

        if (selected == null)
        {
            Debug.LogWarning("Select một prefab/model trước.");
            return;
        }

        MeshFilter meshFilter = selected.GetComponentInChildren<MeshFilter>();

        if (meshFilter == null || meshFilter.sharedMesh == null)
        {
            Debug.LogWarning("Không tìm thấy MeshFilter.");
            return;
        }

        Mesh source = meshFilter.sharedMesh;

        using Mesh.MeshDataArray meshDataArray = MeshUtility.AcquireReadOnlyMeshData(source);

        Mesh.MeshData meshData = meshDataArray[0];

        NativeArray<Vector3> vertices = new NativeArray<Vector3>(meshData.vertexCount, Allocator.Temp);
        meshData.GetVertices(vertices);

        SubMeshDescriptor subMesh = meshData.GetSubMesh(0);

        NativeArray<int> indices = new NativeArray<int>((int)subMesh.indexCount, Allocator.Temp);
        meshData.GetIndices(indices, 0, true);

        Mesh colliderMesh = new Mesh();
        colliderMesh.name = source.name + "_Collider";
        colliderMesh.indexFormat = source.indexFormat;
        colliderMesh.vertices = vertices.ToArray();
        colliderMesh.triangles = indices.ToArray();
        colliderMesh.RecalculateBounds();

        vertices.Dispose();
        indices.Dispose();

        if (!AssetDatabase.IsValidFolder("Assets/GeneratedColliders"))
            AssetDatabase.CreateFolder("Assets", "GeneratedColliders");

        string path = AssetDatabase.GenerateUniqueAssetPath($"Assets/GeneratedColliders/{source.name}_Collider.asset");

        AssetDatabase.CreateAsset(colliderMesh, path);
        AssetDatabase.SaveAssets();

        Selection.activeObject = colliderMesh;

        Debug.Log($"Collider mesh baked: {path}");
    }
}