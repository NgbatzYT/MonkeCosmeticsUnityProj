using MonkeCosmetics.Cosmetic;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public class MCmatCreator : EditorWindow
{
    public enum CreatorType
    {
        Material,
        Cosmetic
    }

    [Serializable]
    public class MaterialThingy
    {
        public Material material;
        public Texture2D thumbnail;
        public string materialId = "creator.matname";
        public bool customColours;
        public bool moddedOnly;
    }

    [Serializable]
    public class CosmeticThingy
    {
        public GameObject obj;
        public Texture2D thumbnail;
        public string cosmeticName = "New Cosmetic";
        public string cosmeticId = "creator.cosmeticname";
        public CosmeticPosition pos = CosmeticPosition.head;
    }

    private CreatorType creatorType = CreatorType.Material;

    private readonly List<MaterialThingy> materials = new();

    private readonly List<CosmeticThingy> cosmetics = new();

    private Vector2 scroll;

    private const string OutputFolder = "Assets/MCcosmetics";
    private const string TempFolder = "Assets/TempStuff";

    [MenuItem("Tools/MC Creator")]
    public static void ShowWindow()
    {
        GetWindow<MCmatCreator>("MC Creator");
    }

    private void OnEnable()
    {
        if (materials.Count == 0)
            materials.Add(new MaterialThingy());

        if (cosmetics.Count == 0)
            cosmetics.Add(new CosmeticThingy());
    }

    private void OnGUI()
    {
        GUILayout.Label("MC Creator", EditorStyles.boldLabel);

        GUILayout.Space(8);

        creatorType = (CreatorType)EditorGUILayout.EnumPopup("Creator Type", creatorType);

        EditorGUILayout.HelpBox(creatorType == CreatorType.Material ? "Create MonkeMaterial assets and build them into an MCcosmetic bundle." : "Create MonkeCosmetic assets and build them into an MCcosmetic bundle.", MessageType.Info);

        GUILayout.Space(10);

        scroll = EditorGUILayout.BeginScrollView(scroll);

        if (creatorType == CreatorType.Material)
        {
            for (int i = 0; i < materials.Count; i++)
                DrawMaterialThingy(i);

            GUILayout.Space(5);

            if (GUILayout.Button("Add Material"))
                materials.Add(new MaterialThingy());
        }
        else
        {
            for (int i = 0; i < cosmetics.Count; i++)
                DrawCosmeticThingy(i);

            GUILayout.Space(5);

            if (GUILayout.Button("Add Cosmetic"))
                cosmetics.Add(new CosmeticThingy());
        }

        EditorGUILayout.EndScrollView();

        GUILayout.Space(15);

        if (GUILayout.Button(creatorType == CreatorType.Material ? "Create MC Material" : "Create MC Cosmetic", GUILayout.Height(35)))
        {
            if (creatorType == CreatorType.Material)
                CreateMaterial();
            else
                CreateCosmetic();
        }
    }

    private void DrawMaterialThingy(int index)
    {
        MaterialThingy entry = materials[index];

        EditorGUILayout.BeginVertical("box");

        EditorGUILayout.BeginHorizontal();

        GUILayout.Label($"Material #{index + 1}", EditorStyles.boldLabel);

        GUI.backgroundColor = Color.red;

        if (GUILayout.Button("Remove", GUILayout.Width(80)))
        {
            materials.RemoveAt(index);
            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
            return;
        }

        GUI.backgroundColor = Color.white;

        EditorGUILayout.EndHorizontal();

        entry.material = (Material)EditorGUILayout.ObjectField("Material", entry.material, typeof(Material), false);

        entry.thumbnail = (Texture2D)EditorGUILayout.ObjectField("Thumbnail", entry.thumbnail, typeof(Texture2D), false);

        entry.materialId = EditorGUILayout.TextField("Material ID", entry.materialId);

        entry.customColours = EditorGUILayout.Toggle("Custom Colours", entry.customColours);

        EditorGUILayout.EndVertical();

        GUILayout.Space(8);
    }

    private void DrawCosmeticThingy(int index)
    {
        CosmeticThingy entry = cosmetics[index];

        EditorGUILayout.BeginVertical("box");

        EditorGUILayout.BeginHorizontal();

        GUILayout.Label($"Cosmetic #{index + 1}", EditorStyles.boldLabel);

        GUI.backgroundColor = Color.red;

        if (GUILayout.Button("Remove", GUILayout.Width(80)))
        {
            cosmetics.RemoveAt(index);
            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
            return;
        }

        GUI.backgroundColor = Color.white;

        EditorGUILayout.EndHorizontal();

        GUILayout.Space(5);

        entry.obj = (GameObject)EditorGUILayout.ObjectField("Cosmetic Object", entry.obj, typeof(GameObject), false);

        entry.thumbnail = (Texture2D)EditorGUILayout.ObjectField("Thumbnail", entry.thumbnail, typeof(Texture2D), false);

        entry.cosmeticName = EditorGUILayout.TextField("Cosmetic Name", entry.cosmeticName);

        entry.cosmeticId = EditorGUILayout.TextField("Cosmetic ID", entry.cosmeticId);

        entry.pos = (CosmeticPosition)EditorGUILayout.EnumPopup("Position", entry.pos);

        EditorGUILayout.EndVertical();

        GUILayout.Space(8);
    }

    private void CreateMaterial()
    {
        if (materials.Count == 0)
        {
            Debug.LogError("No materials added.");
            return;
        }

        List<string> assetPaths = new();

        EnsureFolders();

        for (int i = 0; i < materials.Count; i++)
        {
            MaterialThingy entry = materials[i];

            if (entry.material == null)
            {
                Debug.LogError($"Material missing at entry #{i + 1}.");
                return;
            }

            if (entry.thumbnail == null)
            {
                Debug.LogError($"Thumbnail missing at entry #{i + 1}.");
                return;
            }

            MonkeMaterial monkeMaterial = ScriptableObject.CreateInstance<MonkeMaterial>();

            monkeMaterial.material = entry.material;
            monkeMaterial.Thumbnail = entry.thumbnail;
            monkeMaterial.materialName = entry.material.name;
            monkeMaterial.id = entry.materialId;
            monkeMaterial.customColours = entry.customColours;
            monkeMaterial.moddedOnly = entry.moddedOnly;

            string assetPath = AssetDatabase.GenerateUniqueAssetPath($"{TempFolder}/{entry.material.name}_MonkeMaterial.asset");

            AssetDatabase.CreateAsset(monkeMaterial, assetPath);

            assetPaths.Add(assetPath);
            AddDependencyPath(assetPaths, entry.material);
            AddDependencyPath(assetPaths, entry.thumbnail);
        }

        BuildBundle(materials[0].materialId, assetPaths);
    }

    private void CreateCosmetic()
    {
        if (cosmetics.Count == 0)
        {
            Debug.LogError("No cosmetics added.");
            return;
        }

        List<string> assetPaths = new();

        EnsureFolders();

        for (int i = 0; i < cosmetics.Count; i++)
        {
            CosmeticThingy entry = cosmetics[i];

            if (entry.obj == null)
            {
                Debug.LogError($"Cosmetic object missing at entry #{i + 1}.");
                return;
            }

            if (entry.thumbnail == null)
            {
                Debug.LogError($"Thumbnail missing at entry #{i + 1}.");
                return;
            }

            MonkeCosmetic monkeCosmetic = ScriptableObject.CreateInstance<MonkeCosmetic>();

            monkeCosmetic.obj = entry.obj;
            monkeCosmetic.Thumbnail = entry.thumbnail;
            monkeCosmetic.cosmeticName = entry.cosmeticName;
            monkeCosmetic.id = entry.cosmeticId;
            monkeCosmetic.pos = entry.pos;

            string safeName = MakeFileNameGood(entry.cosmeticName);

            string assetPath = AssetDatabase.GenerateUniqueAssetPath($"{TempFolder}/{safeName}_MonkeCosmetic.asset");

            AssetDatabase.CreateAsset(monkeCosmetic, assetPath);

            assetPaths.Add(assetPath);
            AddDependencyPath(assetPaths, entry.obj);
            AddDependencyPath(assetPaths, entry.thumbnail);
        }

        BuildBundle(cosmetics[0].cosmeticId, assetPaths);
    }

    private void BuildBundle(string id, List<string> assetPaths)
    {
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        string bundleName = MakeFileNameGood(string.IsNullOrWhiteSpace(id) ? "creator" : id.ToLowerInvariant());

        AssetBundleBuild build = new()
        {
            assetBundleName = bundleName,
            assetNames = assetPaths.ToArray()
        };

        string outputFolder = Path.GetFullPath(OutputFolder);

        if (!Directory.Exists(outputFolder))
            Directory.CreateDirectory(outputFolder);

        BuildPipeline.BuildAssetBundles(OutputFolder, new[] { build }, BuildAssetBundleOptions.None, EditorUserBuildSettings.activeBuildTarget);

        string bundlePath = Path.Combine(OutputFolder, bundleName);
        string outputPath = Path.Combine(OutputFolder, bundleName + ".MCcosmetic");

        if (!File.Exists(bundlePath))
        {
            Debug.LogError("AssetBundle build failed. Check the Unity Console for errors.");

            return;
        }

        if (File.Exists(outputPath))
            File.Delete(outputPath);

        File.Move(bundlePath, outputPath);

        DeleteIfExists(bundlePath + ".manifest");

        DeleteIfExists(Path.Combine(OutputFolder, Path.GetFileName(OutputFolder)));

        DeleteIfExists(Path.Combine(OutputFolder, Path.GetFileName(OutputFolder) + ".manifest"));

        AssetDatabase.DeleteAsset(TempFolder);
        AssetDatabase.RemoveUnusedAssetBundleNames();
        AssetDatabase.Refresh();

        Debug.Log($"MCcosmetic created successfully:\n{outputPath}");
    }

    private void EnsureFolders()
    {
        if (!AssetDatabase.IsValidFolder("Assets/TempStuff"))
            AssetDatabase.CreateFolder("Assets", "TempStuff");

        if (!AssetDatabase.IsValidFolder(OutputFolder))
            AssetDatabase.CreateFolder("Assets", "MCcosmetics");
    }

    private void AddDependencyPath(List<string> assetPaths, UnityEngine.Object asset)
    {
        if (asset == null)
            return;

        string path = AssetDatabase.GetAssetPath(asset);

        if (!string.IsNullOrEmpty(path) && !assetPaths.Contains(path))
            assetPaths.Add(path);
    }

    private void DeleteIfExists(string path)
    {
        if (File.Exists(path))
            File.Delete(path);
    }

    private string MakeFileNameGood(string fileName)
    {
        foreach (char c in Path.GetInvalidFileNameChars())
            fileName = fileName.Replace(c.ToString(), "");

        return fileName;
    }
}