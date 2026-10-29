using System.IO;
using UnityEditor;
using UnityEngine;

public abstract class BaseExporter
{
    public virtual void Export(string exportFolder) {
        ExportAssets(exportFolder, "t:Mesh");
        ExportAssets(exportFolder, "t:Texture2D");
    }

    private void ExportAssets(string exportFolder, string filter) {
        string[] assetGuids = AssetDatabase.FindAssets(filter, new []{ "Assets" });
        foreach(string guid in assetGuids) {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            assetPath = assetPath.Substring("Assets".Length);
            string destinationPath = exportFolder + assetPath;
            string sourcePath = Application.dataPath + assetPath;
            File.Copy(sourcePath, destinationPath, true);
        }
    }
}
