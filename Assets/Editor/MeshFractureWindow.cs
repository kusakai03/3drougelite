using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class MeshFractureWindow : EditorWindow
{
    private GameObject sourceObject;
    private int pieceCount = 6;
    private Material insideMaterial;
    private bool addCollider = true;
    private bool addRigidbody = true;

    [MenuItem("Tools/Mesh Fracture")]
    public static void Open()
    {
        GetWindow<MeshFractureWindow>("Mesh Fracture");
    }

    private void OnGUI()
    {
        GUILayout.Label("Mesh Fracture", EditorStyles.boldLabel);

        sourceObject = (GameObject)EditorGUILayout.ObjectField("Source Object", sourceObject, typeof(GameObject), true);
        pieceCount = EditorGUILayout.IntSlider("Pieces", pieceCount, 2, 20);
        insideMaterial = (Material)EditorGUILayout.ObjectField("Inside Material", insideMaterial, typeof(Material), false);

        addCollider = EditorGUILayout.Toggle("Add Mesh Collider", addCollider);
        addRigidbody = EditorGUILayout.Toggle("Add Rigidbody", addRigidbody);

        GUILayout.Space(10);

        GUI.enabled = sourceObject != null;

        if (GUILayout.Button("Fracture"))
            Fracture();

        GUI.enabled = true;
    }

    private void Fracture()
    {
        MeshFilter sourceFilter = sourceObject.GetComponent<MeshFilter>();

        if (sourceFilter == null || sourceFilter.sharedMesh == null)
        {
            Debug.LogError("Source Object không có MeshFilter/Mesh.");
            return;
        }

        Mesh sourceMesh = sourceFilter.sharedMesh;

        List<FracturePiece> pieces = new()
        {
            new FracturePiece(sourceMesh)
        };

        int safety = 100;

        while (pieces.Count < pieceCount && safety-- > 0)
        {
            int index = FindLargestPiece(pieces);

            if (index < 0)
                break;

            FracturePiece piece = pieces[index];

            Bounds bounds = piece.mesh.bounds;

            Vector3 normal = Random.onUnitSphere;

            Vector3 randomPoint = new Vector3(
                Random.Range(bounds.min.x * 0.4f, bounds.max.x * 0.4f),
                Random.Range(bounds.min.y * 0.4f, bounds.max.y * 0.4f),
                Random.Range(bounds.min.z * 0.4f, bounds.max.z * 0.4f)
            );

            Plane plane = new Plane(normal, randomPoint);

            if (!MeshPlaneCutter.Cut(piece.mesh, plane, out Mesh meshA, out Mesh meshB))
                continue;

            pieces.RemoveAt(index);
            pieces.Add(new FracturePiece(meshA));
            pieces.Add(new FracturePiece(meshB));
        }

        CreateResult(pieces);

        Debug.Log($"Fracture complete: {pieces.Count} pieces.");
    }

    private int FindLargestPiece(List<FracturePiece> pieces)
    {
        int result = -1;
        float largest = 0f;

        for (int i = 0; i < pieces.Count; i++)
        {
            Bounds bounds = pieces[i].mesh.bounds;
            float volume = bounds.size.x * bounds.size.y * bounds.size.z;

            if (volume <= largest)
                continue;

            largest = volume;
            result = i;
        }

        return result;
    }

    private void CreateResult(List<FracturePiece> pieces)
    {
        GameObject root = new GameObject(sourceObject.name + "_Fractured");

        root.transform.SetPositionAndRotation(sourceObject.transform.position, sourceObject.transform.rotation);
        root.transform.localScale = sourceObject.transform.lossyScale;

        MeshRenderer sourceRenderer = sourceObject.GetComponent<MeshRenderer>();
        Material outsideMaterial = sourceRenderer != null && sourceRenderer.sharedMaterials.Length > 0 ? sourceRenderer.sharedMaterials[0] : null;

        string folder = "Assets/GeneratedMeshes";

        if (!AssetDatabase.IsValidFolder(folder))
            AssetDatabase.CreateFolder("Assets", "GeneratedMeshes");

        for (int i = 0; i < pieces.Count; i++)
        {
            GameObject obj = new GameObject($"Piece_{i + 1:00}");
            obj.transform.SetParent(root.transform, false);

            Mesh mesh = pieces[i].mesh;
            mesh.name = $"{sourceObject.name}_Piece_{i + 1:00}";

            string path = AssetDatabase.GenerateUniqueAssetPath($"{folder}/{mesh.name}.asset");
            AssetDatabase.CreateAsset(mesh, path);

            MeshFilter filter = obj.AddComponent<MeshFilter>();
            MeshRenderer renderer = obj.AddComponent<MeshRenderer>();

            filter.sharedMesh = mesh;

            if (insideMaterial != null)
                renderer.sharedMaterials = new[] { outsideMaterial, insideMaterial };
            else if (outsideMaterial != null)
                renderer.sharedMaterial = outsideMaterial;

            if (addCollider)
            {
                MeshCollider collider = obj.AddComponent<MeshCollider>();
                collider.sharedMesh = mesh;
                collider.convex = true;
            }

            if (addRigidbody)
            {
                Rigidbody rb = obj.AddComponent<Rigidbody>();
                rb.mass = 1f / Mathf.Max(1, pieces.Count);
            }
        }

        AssetDatabase.SaveAssets();

        Selection.activeGameObject = root;
        Undo.RegisterCreatedObjectUndo(root, "Create Fractured Mesh");
    }

    private class FracturePiece
    {
        public Mesh mesh;

        public FracturePiece(Mesh mesh)
        {
            this.mesh = mesh;
        }
    }
}

public static class MeshPlaneCutter
{
    private struct Vertex
    {
        public Vector3 position;
        public Vector3 normal;
        public Vector2 uv;

        public Vertex(Vector3 position, Vector3 normal, Vector2 uv)
        {
            this.position = position;
            this.normal = normal;
            this.uv = uv;
        }
    }

    private class MeshBuilder
    {
        public readonly List<Vector3> vertices = new();
        public readonly List<Vector3> normals = new();
        public readonly List<Vector2> uvs = new();

        public readonly List<int> outsideTriangles = new();
        public readonly List<int> insideTriangles = new();

        public int AddVertex(Vertex vertex)
        {
            int index = vertices.Count;

            vertices.Add(vertex.position);
            normals.Add(vertex.normal);
            uvs.Add(vertex.uv);

            return index;
        }

        public void AddTriangle(Vertex a, Vertex b, Vertex c, bool inside = false)
        {
            int ia = AddVertex(a);
            int ib = AddVertex(b);
            int ic = AddVertex(c);

            List<int> triangles = inside ? insideTriangles : outsideTriangles;

            triangles.Add(ia);
            triangles.Add(ib);
            triangles.Add(ic);
        }

        public Mesh Build()
        {
            Mesh mesh = new Mesh();

            mesh.SetVertices(vertices);
            mesh.SetNormals(normals);
            mesh.SetUVs(0, uvs);

            mesh.subMeshCount = 2;
            mesh.SetTriangles(outsideTriangles, 0);
            mesh.SetTriangles(insideTriangles, 1);

            mesh.RecalculateBounds();

            return mesh;
        }
    }

    public static bool Cut(Mesh source, Plane plane, out Mesh positiveMesh, out Mesh negativeMesh)
    {
        MeshBuilder positive = new();
        MeshBuilder negative = new();

        Vector3[] positions = source.vertices;
        Vector3[] normals = source.normals;
        Vector2[] uvs = source.uv;

        if (normals == null || normals.Length != positions.Length)
        {
            source.RecalculateNormals();
            normals = source.normals;
        }

        bool hasUV = uvs != null && uvs.Length == positions.Length;

        List<Vector3> cutPoints = new();

        for (int subMesh = 0; subMesh < source.subMeshCount; subMesh++)
        {
            int[] triangles = source.GetTriangles(subMesh);

            for (int i = 0; i < triangles.Length; i += 3)
            {
                Vertex a = GetVertex(triangles[i], positions, normals, uvs, hasUV);
                Vertex b = GetVertex(triangles[i + 1], positions, normals, uvs, hasUV);
                Vertex c = GetVertex(triangles[i + 2], positions, normals, uvs, hasUV);

                CutTriangle(a, b, c, plane, positive, negative, cutPoints);
            }
        }

        if (positive.outsideTriangles.Count == 0 || negative.outsideTriangles.Count == 0)
        {
            positiveMesh = null;
            negativeMesh = null;
            return false;
        }

        CreateCap(cutPoints, plane, positive, negative);

        positiveMesh = positive.Build();
        negativeMesh = negative.Build();

        positiveMesh.name = source.name + "_A";
        negativeMesh.name = source.name + "_B";

        return true;
    }

    private static Vertex GetVertex(int index, Vector3[] positions, Vector3[] normals, Vector2[] uvs, bool hasUV)
    {
        return new Vertex(
            positions[index],
            normals[index],
            hasUV ? uvs[index] : Vector2.zero
        );
    }

    private static void CutTriangle(Vertex a, Vertex b, Vertex c, Plane plane, MeshBuilder positive, MeshBuilder negative, List<Vector3> cutPoints)
    {
        bool sideA = plane.GetSide(a.position);
        bool sideB = plane.GetSide(b.position);
        bool sideC = plane.GetSide(c.position);

        if (sideA == sideB && sideB == sideC)
        {
            if (sideA)
                positive.AddTriangle(a, b, c);
            else
                negative.AddTriangle(a, b, c);

            return;
        }

        List<Vertex> pos = new();
        List<Vertex> neg = new();

        SplitEdge(a, b, sideA, sideB, pos, neg, plane, cutPoints);
        SplitEdge(b, c, sideB, sideC, pos, neg, plane, cutPoints);
        SplitEdge(c, a, sideC, sideA, pos, neg, plane, cutPoints);

        TriangulatePolygon(pos, positive);
        TriangulatePolygon(neg, negative);
    }

    private static void SplitEdge(Vertex a, Vertex b, bool sideA, bool sideB, List<Vertex> pos, List<Vertex> neg, Plane plane, List<Vector3> cutPoints)
    {
        AddUnique(sideA ? pos : neg, a);

        if (sideA == sideB)
            return;

        float distanceA = plane.GetDistanceToPoint(a.position);
        float distanceB = plane.GetDistanceToPoint(b.position);

        float t = distanceA / (distanceA - distanceB);

        Vertex intersection = new Vertex(
            Vector3.Lerp(a.position, b.position, t),
            Vector3.Lerp(a.normal, b.normal, t).normalized,
            Vector2.Lerp(a.uv, b.uv, t)
        );

        AddUnique(pos, intersection);
        AddUnique(neg, intersection);

        cutPoints.Add(intersection.position);
    }

    private static void AddUnique(List<Vertex> list, Vertex vertex)
    {
        for (int i = 0; i < list.Count; i++)
        {
            if ((list[i].position - vertex.position).sqrMagnitude < 0.000001f)
                return;
        }

        list.Add(vertex);
    }

    private static void TriangulatePolygon(List<Vertex> polygon, MeshBuilder builder)
    {
        if (polygon.Count < 3)
            return;

        for (int i = 1; i < polygon.Count - 1; i++)
            builder.AddTriangle(polygon[0], polygon[i], polygon[i + 1]);
    }

    private static void CreateCap(List<Vector3> points, Plane plane, MeshBuilder positive, MeshBuilder negative)
    {
        RemoveDuplicatePoints(points);

        if (points.Count < 3)
            return;

        Vector3 center = Vector3.zero;

        foreach (Vector3 point in points)
            center += point;

        center /= points.Count;

        Vector3 axisX = Vector3.Cross(plane.normal, Vector3.up);

        if (axisX.sqrMagnitude < 0.01f)
            axisX = Vector3.Cross(plane.normal, Vector3.right);

        axisX.Normalize();

        Vector3 axisY = Vector3.Cross(plane.normal, axisX).normalized;

        points.Sort((a, b) =>
        {
            Vector3 da = a - center;
            Vector3 db = b - center;

            float angleA = Mathf.Atan2(Vector3.Dot(da, axisY), Vector3.Dot(da, axisX));
            float angleB = Mathf.Atan2(Vector3.Dot(db, axisY), Vector3.Dot(db, axisX));

            return angleA.CompareTo(angleB);
        });

        float scale = 1f;

        for (int i = 0; i < points.Count; i++)
        {
            Vector3 p1 = points[i];
            Vector3 p2 = points[(i + 1) % points.Count];

            Vector2 uvCenter = new Vector2(Vector3.Dot(center, axisX), Vector3.Dot(center, axisY)) * scale;
            Vector2 uv1 = new Vector2(Vector3.Dot(p1, axisX), Vector3.Dot(p1, axisY)) * scale;
            Vector2 uv2 = new Vector2(Vector3.Dot(p2, axisX), Vector3.Dot(p2, axisY)) * scale;

            Vertex positiveCenter = new Vertex(center, -plane.normal, uvCenter);
            Vertex positiveA = new Vertex(p1, -plane.normal, uv1);
            Vertex positiveB = new Vertex(p2, -plane.normal, uv2);

            Vertex negativeCenter = new Vertex(center, plane.normal, uvCenter);
            Vertex negativeA = new Vertex(p1, plane.normal, uv1);
            Vertex negativeB = new Vertex(p2, plane.normal, uv2);

            positive.AddTriangle(positiveCenter, positiveB, positiveA, true);
            negative.AddTriangle(negativeCenter, negativeA, negativeB, true);
        }
    }

    private static void RemoveDuplicatePoints(List<Vector3> points)
    {
        for (int i = points.Count - 1; i >= 0; i--)
        {
            for (int j = 0; j < i; j++)
            {
                if ((points[i] - points[j]).sqrMagnitude < 0.000001f)
                {
                    points.RemoveAt(i);
                    break;
                }
            }
        }
    }
}