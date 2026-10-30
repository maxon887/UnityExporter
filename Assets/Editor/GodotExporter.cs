using System.IO;
using System.Text;
using UnityEngine.SceneManagement;

public class GodotExporter : BaseExporter
{
	
	public override void Export(string exportFolder) {
		base.Export(exportFolder);

		Scene scene = SceneManager.GetActiveScene();
		string scenePath = scene.path;
		scenePath = scenePath.Substring("Assets".Length);
		scenePath = scenePath.Replace(".unity", ".tscn");

		StringBuilder godotScene = new StringBuilder();
		AddSceneHeader(godotScene);
		AddNode(godotScene, "Root", "Node3D");
		File.WriteAllText(exportFolder + scenePath, godotScene.ToString());
	}

	private void AddSceneHeader(StringBuilder godotScene) {
		godotScene.AppendLine("[gd_scene format=3]");
		godotScene.AppendLine();
	}

	private void AddNode(StringBuilder godotScene, string name, string type) {
		godotScene.AppendLine($"[node name=\"{name}\" type=\"{type}\"]");
	}
}