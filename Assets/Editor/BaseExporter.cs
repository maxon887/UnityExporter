using UnityEditor;
using UnityEngine;

public abstract class BaseExporter
{
    public virtual void Export(string exportFolder) {
        ExportAllMeshes();
    }

    private void ExportAllMeshes() {
        string[] assetGuids = AssetDatabase.FindAssets("t:Texture2D");
        foreach(string guid in assetGuids) {

        }
    }
}
