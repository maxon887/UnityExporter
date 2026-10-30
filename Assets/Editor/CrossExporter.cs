using System.Xml;
using UnityEngine;
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
		XmlElement objectsXml = doc.CreateElement("Objects");
		sceneXml.AppendChild(objectsXml);

		GameObject[] rootObjects = scene.GetRootGameObjects();
		foreach(GameObject rootObject in rootObjects) {
			AddChild(doc, objectsXml, rootObject);
		}
		
		doc.Save(exportFolder + scenePath);
	}

	private void AddChild(XmlDocument doc, XmlElement parent, GameObject gameObject) {
		if(!gameObject.GetComponent<MeshRenderer>()) return;
		
		XmlElement objectXml = doc.CreateElement("Object");
		objectXml.SetAttribute("name", gameObject.name);
		XmlElement componentsXml = doc.CreateElement("Components");
		objectXml.AppendChild(componentsXml);
		//Transform Component
		XmlElement transformXml = doc.CreateElement("Transform");
		componentsXml.AppendChild(transformXml);
		XmlElement positionXml = doc.CreateElement("Position");
		transformXml.AppendChild(positionXml);
		Vector3 position = gameObject.transform.localPosition;
		positionXml.SetAttribute("type", "Vector3D");
		positionXml.SetAttribute("x", position.x.ToString());
		positionXml.SetAttribute("y", position.y.ToString());
		positionXml.SetAttribute("z", position.z.ToString());
		XmlElement rotationXml = doc.CreateElement("Rotation");
		transformXml.AppendChild(rotationXml);
		Quaternion rotation = gameObject.transform.localRotation;
		rotationXml.SetAttribute("type", "Quaternion");
		rotationXml.SetAttribute("x", rotation.x.ToString());
		rotationXml.SetAttribute("y", rotation.y.ToString());
		rotationXml.SetAttribute("z", rotation.z.ToString());
		rotationXml.SetAttribute("w", rotation.w.ToString());
		XmlElement scaleXml = doc.CreateElement("Scale");
		transformXml.AppendChild(scaleXml);
		Vector3 scale = gameObject.transform.localScale;
		scaleXml.SetAttribute("type", "Vector3D");
		scaleXml.SetAttribute("x", scale.x.ToString());
		scaleXml.SetAttribute("y", scale.y.ToString());
		scaleXml.SetAttribute("z", scale.z.ToString());
		parent.AppendChild(objectXml);
	}
}
