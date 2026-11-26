using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public class CrossExporter : BaseExporter {
    
    private HashSet<string> exported_materials = new HashSet<string>();

	protected override void Export(GameObject[] rootObjects) {
		project_folder = DetectProjectFolder(export_folder);
		if(project_folder == "") {
			throw new Exception("Exporter can not detect project folder");
		}
	
		Scene scene = SceneManager.GetActiveScene();
		string scenePath = scene.path;
		scenePath = scenePath.Replace(".unity", ".scn");
		
		XmlDocument doc = new XmlDocument();
		XmlElement sceneXml = doc.CreateElement("Scene");
		sceneXml.SetAttribute("version", "20");
		doc.AppendChild(sceneXml);
		XmlElement objectsXml = doc.CreateElement("Objects");
		sceneXml.AppendChild(objectsXml);
		
		foreach(GameObject rootObject in rootObjects) {
			AddChild(doc, objectsXml, rootObject);
		}

        scenePath = BuildExportPath(scenePath);
		string parentDirectory = Path.GetDirectoryName(scenePath);
		Directory.CreateDirectory(parentDirectory);
		doc.Save(scenePath);
		
		base.Export();
	}

	private void AddChild(XmlDocument doc, XmlElement parent, GameObject gameObject) {
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
		//Mesh Component
		MeshFilter meshFilter = gameObject.GetComponent<MeshFilter>();
		MeshRenderer meshRenderer = gameObject.GetComponent<MeshRenderer>();
		if(meshFilter && meshRenderer) {
			XmlElement meshXml = doc.CreateElement("Mesh");
			componentsXml.AppendChild(meshXml);
			XmlElement groupIDXml = doc.CreateElement("GroupID");
			meshXml.AppendChild(groupIDXml);
			groupIDXml.SetAttribute("type", "String");
			Mesh mesh = meshFilter.sharedMesh;
			groupIDXml.SetAttribute("value", mesh.name);

			string modelPath = AssetDatabase.GetAssetPath(mesh);
			resources_to_copy.Add(modelPath);
			XmlElement modelXml = doc.CreateElement("Model");
			meshXml.AppendChild(modelXml);
			modelXml.SetAttribute("type", "String");
			modelXml.SetAttribute("value", BuildCrossPath(modelPath));
            
            XmlElement materialXml = doc.CreateElement("Material");
            meshXml.AppendChild(materialXml);
            materialXml.SetAttribute("type",  "String");
            string crossMaterialPath = CreateCrossMaterial(meshRenderer.sharedMaterial);
            materialXml.SetAttribute("value", crossMaterialPath);
        }

		XmlElement childrenXml = doc.CreateElement("Children");
		objectXml.AppendChild(childrenXml);
        
		foreach(GameObject child in GetChildren(gameObject)) {
			AddChild(doc, childrenXml, child);
		}
	}

	private string DetectProjectFolder(string startPath) {
		DirectoryInfo parent = Directory.GetParent(startPath);
		if(parent.Name == "Assets") {
			return parent.FullName.Replace("\\", "/");
		}

		return DetectProjectFolder(parent.FullName);
	}

    private string BuildCrossPath(string unityPath) {
        return BuildExportPath(unityPath).Replace(project_folder, "");
    }

    private string BuildExportPath(string unityPath) {
        return export_folder + unityPath.Substring("Assets".Length);
    }

    private string CreateCrossMaterial(Material unityMaterial) {
        string unityMaterialPath = AssetDatabase.GetAssetPath(unityMaterial);
        string crossMaterialPath = BuildCrossPath(unityMaterialPath);
        if(exported_materials.Contains(unityMaterialPath)) {
            return crossMaterialPath;
        }
        XmlDocument doc = new XmlDocument();
        XmlElement materialXml = doc.CreateElement("Material");
        doc.AppendChild(materialXml);
        materialXml.SetAttribute("shader", "Engine/Shaders/Texture.sha");
        XmlElement propertyXml = doc.CreateElement("Property");
        materialXml.AppendChild(propertyXml);
        propertyXml.SetAttribute("name", "Texture");
        Texture texture = FindMainTexture(unityMaterial);
        string unityAlbedoTexturePath = AssetDatabase.GetAssetPath(texture);
        resources_to_copy.Add(unityAlbedoTexturePath);
        string crossTexturePath = BuildCrossPath(unityAlbedoTexturePath);
        propertyXml.SetAttribute("value", crossTexturePath);
        string exportMaterialPath = BuildExportPath(unityMaterialPath);
        string parentDirectory = Path.GetDirectoryName(exportMaterialPath);
        Directory.CreateDirectory(parentDirectory);
        doc.Save(exportMaterialPath);
        return crossMaterialPath;
    }
}
