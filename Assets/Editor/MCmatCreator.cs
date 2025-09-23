using UnityEngine;
using UnityEditor;
using System.IO;

public class MCmatCreator
{
    [MenuItem("Tools/Build All Materials")]
    public static void MakeMcMatfile()
    {
        string dir = "Assets/McMats";

        Directory.Delete(dir, true);
        Directory.CreateDirectory(dir);
        
        
        BuildPipeline.BuildAssetBundles(dir, BuildAssetBundleOptions.None, BuildTarget.StandaloneWindows);

        ChangeExtension(dir, ".MCmat");
    }

    private static void ChangeExtension(string directory, string ext)
    {
        foreach (string file in Directory.GetFiles(directory))
        {
            if (Path.GetExtension(file) == ".manifest" || Path.GetExtension(file) == ".meta" || Path.GetFileNameWithoutExtension(file) == "McMats") continue;

            string newFile = Path.ChangeExtension(file, ext);

            File.Copy(file, newFile, true); 
        }
    }
}
