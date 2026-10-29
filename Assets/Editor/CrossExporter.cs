using UnityEngine;
using UnityEngine.SceneManagement;

public class CrossExporter : BaseExporter
{
    public override void Export(string exportFolder) {
        base.Export(exportFolder);
        GameObject[] gameObjects = SceneManager.GetActiveScene().GetRootGameObjects();
    }
}
