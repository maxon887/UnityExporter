using System.IO;
using UnityEditor;
using UnityEngine;

public abstract class BaseExporter
{
    public virtual void Export(string exportFolder) {
        ExportAllMeshes(exportFolder);
    }

    private void ExportAllMeshes(string exportFolder) {
        string[] assetGuids = AssetDatabase.FindAssets("t:Mesh", new []{ "Assets" });
        foreach(string guid in assetGuids) {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            assetPath = assetPath.Substring("Assets".Length);
            string destinationPath = exportFolder + assetPath;
            string sourcePath = Application.dataPath + assetPath;
            File.Copy(sourcePath, destinationPath, true);
        }
    }
}
