using System.Xml;
using UnityEngine.SceneManagement;

public class CrossExporter : BaseExporter {

	public override void Export(string exportFolder) {
		base.Export(exportFolder);

		Scene scene = SceneManager.GetActiveScene();
		string scenePath = scene.path;
		scenePath = scenePath.Substring("Assets".Length);
		scenePath = scenePath.Replace(".unity", ".scn");
		
		XmlDocument doc = new XmlDocument();
		XmlElement sceneXml = doc.CreateElement("Scene");
		sceneXml.SetAttribute("version", "20");
		doc.AppendChild(sceneXml);
		doc.Save(exportFolder + scenePath);
	}
}
