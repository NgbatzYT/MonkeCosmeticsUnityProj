using UnityEngine;
using UnityEditor;
using System.IO;
using System;
using System.Collections.Generic;
using MonkeCosmetics.Editor.Cosmetic;

public class MCmatCreator : EditorWindow
{
    [Serializable]
    public class MaterialThingy
    {
        public Material material;
        public Texture2D thumbnail;
        public string materialId = "creator.matname";
        public bool customColours;
        public bool moddedOnly;
    }

    private List<MaterialEntry> materials = new List<MaterialEntry>();
    private Vector2 scroll;

    [MenuItem("Tools/Material Creator")]
    public static void ShowWindow()
    {
        GetWindow<MCmatCreator>("Material Creator");
    }

    private void OnEnable()
    {
        if (materials.Count == 0) materials.Add(new MaterialEntry());
    }

    private void OnGUI()
    {
        GUILayout.Label("MCmat Creator", EditorStyles.boldLabel);

        GUILayout.Space(10);

        scroll = EditorGUILayout.BeginScrollView(scroll);

        for (int i = 0; i < materials.Count; i++) DrawMaterialThingy(i);

        EditorGUILayout.EndScrollView();

        GUILayout.Space(10);

        if (GUILayout.Button("Add Material")) materials.Add(new MaterialThingy());

        GUILayout.Space(20);

        if (GUILayout.Button("Create MCmat")) CreateMaterial();
    }

    private void DrawMaterialThingy(int index)
    {
        MaterialEntry entry = materials[index];

        EditorGUILayout.BeginVertical("box");

        GUILayout.BeginHorizontal();

        GUILayout.Label($"Material #{index + 1}", EditorStyles.boldLabel);

        GUI.backgroundColor = Color.red;

        if (GUILayout.Button("Remove", GUILayout.Width(80)))
        {
            materials.RemoveAt(index);
            GUI.backgroundColor = Color.white;
            return;
        }

        GUI.backgroundColor = Color.white;

        GUILayout.EndHorizontal();

        GUILayout.Space(5);

        entry.material = (Material)EditorGUILayout.ObjectField("Material", entry.material, typeof(Material), false);
        entry.thumbnail = (Texture2D)EditorGUILayout.ObjectField("Thumbnail", entry.thumbnail, typeof(Texture2D), false);

        GUILayout.Space(5);

        entry.materialId = EditorGUILayout.TextField("Material ID", entry.materialId);
        entry.customColours = EditorGUILayout.Toggle("Custom Colours", entry.customColours);
        entry.moddedOnly = EditorGUILayout.Toggle("Modded Only", entry.moddedOnly);

        EditorGUILayout.EndVertical();

        GUILayout.Space(10);
    }

    private void CreateMaterial()
    {
        if (materials.Count == 0)
        {
            Debug.LogError("No materials added.");
            return;
        }

        List<string> assetPaths = new List<string>();

        string tempFolder = "Assets/TempStuff";

        if (!AssetDatabase.IsValidFolder(tempFolder)) AssetDatabase.CreateFolder("Assets", "TempStuff");

        for (int i = 0; i < materials.Count; i++)
        {
            MaterialEntry entry = materials[i];

            if (entry.material == null)
            {
                Debug.LogError($"Material missing #{i + 1}");
                return;
            }

            if (entry.thumbnail == null)
            {
                Debug.LogError($"Thumbnail missing #{i + 1}");
                return;
            }

            MonkeMaterial monkeMaterial = ScriptableObject.CreateInstance<MonkeMaterial>();

            monkeMaterial.material = entry.material;
            monkeMaterial.Thumbnail = entry.thumbnail;
            monkeMaterial.materialName = entry.material.name;
            monkeMaterial.id = entry.materialId;
            monkeMaterial.customColours = entry.customColours;
            monkeMaterial.moddedOnly = entry.moddedOnly;

            string assetPath = $"{tempFolder}/{entry.material.name}_MonkeMaterial.asset";

            AssetDatabase.CreateAsset(monkeMaterial, assetPath);

            assetPaths.Add(assetPath);
            assetPaths.Add(AssetDatabase.GetAssetPath(entry.material));
            assetPaths.Add(AssetDatabase.GetAssetPath(entry.thumbnail));
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        string bundleName = MakeFileNameGood(materials[0].materialId.ToLower());

        AssetBundleBuild build = new AssetBundleBuild
        {
            assetBundleName = bundleName,
            assetNames = assetPaths.ToArray()
        };

        string buildFolder = "Assets/McMats";+

        if (!Directory.Exists(buildFolder)) Directory.CreateDirectory(buildFolder);

        BuildPipeline.BuildAssetBundles(buildFolder, new[] { build }, BuildAssetBundleOptions.None, EditorUserBuildSettings.activeBuildTarget);

        string originalBundlePath = Path.Combine(buildFolder, bundleName);
        string mcMatPath = Path.Combine(buildFolder, bundleName + ".MCmat");

        if (File.Exists(mcMatPath)) File.Delete(mcMatPath);

        if (File.Exists(originalBundlePath)) File.Move(originalBundlePath, mcMatPath);

        DeleteIfExists(originalBundlePath + ".manifest");
        DeleteIfExists(Path.Combine(buildFolder, Path.GetFileName(buildFolder)));
        DeleteIfExists(Path.Combine(buildFolder, Path.GetFileName(buildFolder) + ".manifest"));

        AssetDatabase.DeleteAsset(tempFolder);
        AssetDatabase.RemoveUnusedAssetBundleNames();

        Debug.Log($"MCmat created successfully:\n{mcMatPath}");
    }

    private void DeleteIfExists(string path)
    {
        if (File.Exists(path)) File.Delete(path);
    }

    private string MakeFileNameGood(string fileName)
    {
        foreach (char c in Path.GetInvalidFileNameChars()) fileName = fileName.Replace(c.ToString(), "");
        return fileName;
    }
}
