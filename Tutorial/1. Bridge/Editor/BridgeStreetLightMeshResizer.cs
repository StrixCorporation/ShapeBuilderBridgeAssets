using UnityEditor;
using UnityEngine;

public static class BridgeStreetLightMeshResizer
{
    private const string SourcePrefabPath = "Assets/Tutorial/1. Bridge/Street_Light_A_08.prefab";
    private const string OutputFolder = "Assets/Tutorial/1. Bridge/Generated";
    private const string OutputMeshPath = OutputFolder + "/Street_Light_A_08_BridgeScale_Pole.asset";
    private const string OutputPrefabPath = OutputFolder + "/Street_Light_A_08_BridgeScale.prefab";
    private const float BridgeScale = 3.8f;

    [MenuItem("Tools/ShapeBuilder/Bridge/Create Bridge Street Light Mesh")]
    private static void CreateBridgeStreetLight()
    {
        var sourcePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(SourcePrefabPath);
        if (sourcePrefab == null)
        {
            Debug.LogError($"Source prefab not found: {SourcePrefabPath}");
            return;
        }

            EnsureFolder(OutputFolder);
            DeleteGeneratedAssetIfExists(OutputMeshPath);
            DeleteGeneratedAssetIfExists(OutputPrefabPath);

        var instance = PrefabUtility.InstantiatePrefab(sourcePrefab) as GameObject;
        if (instance == null)
        {
            Debug.LogError("Could not instantiate source street light prefab.");
            return;
        }

        try
        {
            instance.name = "Street_Light_A_08_BridgeScale";
            instance.transform.localScale = Vector3.one;

            var poleFilter = instance.GetComponent<MeshFilter>();
            if (poleFilter == null || poleFilter.sharedMesh == null)
            {
                Debug.LogError("Root pole MeshFilter was not found.");
                return;
            }

            var lamp = instance.transform.Find("Light_15");
            if (lamp == null)
            {
                Debug.LogError("Child object 'Light_15' was not found.");
                return;
            }

            lamp.localPosition *= BridgeScale;
            lamp.localScale = Vector3.one * BridgeScale;

            var bridgePole = CreateBridgePoleMesh(poleFilter.sharedMesh);
            AssetDatabase.CreateAsset(bridgePole, OutputMeshPath);
            poleFilter.sharedMesh = bridgePole;

            PrefabUtility.SaveAsPrefabAsset(instance, OutputPrefabPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"Created bridge street light prefab at {OutputPrefabPath}. Lamp/head is scaled by {BridgeScale:0.###}x, while the pole mesh keeps its original X/Z thickness and is only extended in Y.");
        }
        finally
        {
            Object.DestroyImmediate(instance);
        }
    }

    private static Mesh CreateBridgePoleMesh(Mesh source)
    {
        var mesh = Object.Instantiate(source);
        mesh.name = source.name + "_BridgeScale";

        var sourceVertices = source.vertices;
        var vertices = new Vector3[sourceVertices.Length];

        for (int i = 0; i < sourceVertices.Length; i++)
        {
            Vector3 original = sourceVertices[i];
            vertices[i] = new Vector3(original.x, original.y * BridgeScale, original.z);
        }

        mesh.vertices = vertices;
        mesh.RecalculateBounds();
        mesh.RecalculateNormals();
        mesh.RecalculateTangents();
        return mesh;
    }

    private static void EnsureFolder(string assetFolder)
    {
        var parts = assetFolder.Split('/');
        string current = parts[0];

        for (int i = 1; i < parts.Length; i++)
        {
            string next = current + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next))
            {
                AssetDatabase.CreateFolder(current, parts[i]);
            }

            current = next;
        }
    }

    private static void DeleteGeneratedAssetIfExists(string assetPath)
    {
        if (AssetDatabase.LoadAssetAtPath<Object>(assetPath) != null)
        {
            AssetDatabase.DeleteAsset(assetPath);
        }
    }
}
