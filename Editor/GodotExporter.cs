using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

public class GodotExporter : BaseExporter {

	private class GodotScene {

		public class Node {
			public enum Type {
				Unknown,
				Node3D,
				StaticBody3D,
				CollisionShape3D
			}
			public string name;
			public Type type = Type.Unknown;
			public int instance = -1;
			public bool is_editable = false;
			public List<string> properties = new List<string>();
			public List<Node> children =  new List<Node>();
			
			public Node(string name) {
				this.name = name;
			}
		}
	
		public struct ExternalResource {
			public string type;
			public string path;
		}
		
		public List<ExternalResource> ext_resources = new List<ExternalResource>();
		public List<string> sub_resources = new List<string>();
		
		private Node root;
		private StringBuilder header = new StringBuilder();
		private HashSet<string> editables = new HashSet<string>();

		public GodotScene(Node root) {
			this.root = root;
			header.AppendLine("[gd_scene format=3]");
			header.AppendLine();
		}

		public string AssembleTSCN() {
			StringBuilder hierarchyBuilder = new StringBuilder();
			BuildHierarchyBlock(root, "", hierarchyBuilder);
			return header + BuildExtResourcesBlock() + BuildSubResourcesBlock() + hierarchyBuilder + BuildEditablePaths();
		}

		private string BuildExtResourcesBlock() {
			StringBuilder extResourcesBuilder = new StringBuilder();
			for(int i = 0; i < ext_resources.Count; i++) {
				string type = ext_resources[i].type;
				string path = ext_resources[i].path;
				extResourcesBuilder.AppendLine($"[ext_resource type=\"{type}\" path=\"{path}\" id=\"{i}\"]");
			}
			extResourcesBuilder.AppendLine();
			return extResourcesBuilder.ToString();
		}

		private string BuildSubResourcesBlock() {
			StringBuilder subResourcesBuilder = new StringBuilder();
			foreach(string subResource in sub_resources) {
				subResourcesBuilder.AppendLine(subResource);
			}
			return subResourcesBuilder.ToString();
		}

		private void BuildHierarchyBlock(Node node, string parentPath, StringBuilder hierarchyBuilder) {
			hierarchyBuilder.Append("[node name=\"" + node.name + "\"");
			if(node.instance != -1) {
				hierarchyBuilder.Append(" instance=ExtResource(\"" + node.instance + "\")");
			} else {
				if(node.type != Node.Type.Unknown) {
					hierarchyBuilder.Append(" type=\"" + node.type + "\"");
				}
			}
			if(parentPath != "") {
				hierarchyBuilder.Append(" parent=\"" + parentPath + "\"");
			}
			hierarchyBuilder.AppendLine("]");

			foreach(string property in node.properties) {
				hierarchyBuilder.AppendLine(property);
			}
			
			hierarchyBuilder.AppendLine();
			
			string childrenParentPath = BuildChildrenParentPath(node, parentPath);

			if(node.is_editable) {
				editables.Add(childrenParentPath);
			}
			
			foreach(Node child in node.children) {
				BuildHierarchyBlock(child, childrenParentPath, hierarchyBuilder);
			}
		}
		
		private string BuildChildrenParentPath(Node node, string parentPath) {
			if(parentPath == "") {
				return ".";
			}
			if(parentPath == ".") {
				return node.name;
			}
			return parentPath + "/" + node.name;
		}

		private string BuildEditablePaths() {
			StringBuilder editablesBuilder = new StringBuilder();
			foreach(string editable in editables) {
				if(editable != "") {
					string editablePath = editable.StartsWith("./") ? editable.Substring(2) : editable;
					editablesBuilder.AppendLine($"[editable path=\"{editablePath}\"]");
				}
			}
			return editablesBuilder.ToString();
		}
	}

	private static Dictionary<string, string> UnityToGotodShaders = new Dictionary<string, string>();
	
	private HashSet<string> exported_prefabs = new HashSet<string>();
	private HashSet<string> created_materials = new HashSet<string>();
	private HashSet<string> created_shapes = new HashSet<string>();
	
	protected override void Export(GameObject[] rootObjects) {
		project_folder = DetectProjectFolder(export_folder);
		if(project_folder == "") {
			throw new Exception("Exporter can not detect project folder");
		}
		UnityToGotodShaders["Synty/Generic_Basic"] = "StandardMaterial3D";
		UnityToGotodShaders["Synty/Generic_Basic_Bloody"] = "ShaderMaterial";
		UnityToGotodShaders["Unlit/Color"] = "StandardMaterial3D";
	
		Scene scene = SceneManager.GetActiveScene();

		GodotScene.Node root = new GodotScene.Node("Root");
		root.type = GodotScene.Node.Type.Node3D;
		GodotScene godotScene = new GodotScene(root);
		
		foreach(GameObject rootObject in rootObjects) {
			AddNode(godotScene, rootObject, root, false, null);
		}

		SaveScene(godotScene, scene.path, ".unity");
		base.Export();
	}

	private void CreateSceneFromPrefab(string prefabPath) {
		if(!exported_prefabs.Add(prefabPath)) {
			return;
		}
	
		GameObject prefabRoot = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
		MeshFilter meshFilter = prefabRoot.GetComponent<MeshFilter>();
		if(!meshFilter) {
			throw new Exception("Unpredicted");
		}

		GodotScene.Node rootNode = new GodotScene.Node(prefabRoot.name);
		GodotScene godotScene = new GodotScene(rootNode);
		//prefab should always contain only 1 root object
		string fbxPath = AssetDatabase.GetAssetPath(meshFilter.sharedMesh);
		CreateModelImportFile(fbxPath);
		int fbxId = AddExtResource(godotScene, fbxPath, "PackedScene");
		rootNode.instance = fbxId;
		AddNode(godotScene, prefabRoot, rootNode, true, null);
		
		SaveScene(godotScene, prefabPath, ".prefab");
	}
	
	private string CreateMaterial(Material unityMaterial) {
		string materialPath = AssetDatabase.GetAssetPath(unityMaterial);
		materialPath = materialPath.Replace(".mat", ".tres");
		if(!created_materials.Add(materialPath)) {
			return materialPath;
		}
		string unityShader = unityMaterial.shader.name;
		if(!UnityToGotodShaders.ContainsKey(unityShader)) {
			Debug.LogWarning("Can not create Material " + unityMaterial + ". Godot shader analog for " + unityShader + " not found");
			return "";
		}
		string godotShader = UnityToGotodShaders[unityShader];
		StringBuilder materialBuilder = new StringBuilder(); 
		materialBuilder.AppendLine("[gd_resource type=\"" + godotShader + "\" format=3]");
		materialBuilder.AppendLine();

		if(unityShader == "Synty/Generic_Basic") {
			Texture mainTexture = FindMainTexture(unityMaterial);
			if(mainTexture) {
				string unityTexturePath = AssetDatabase.GetAssetPath(mainTexture);
				resources_to_copy.Add(unityTexturePath);
				string godotTexturePath = BuildGodotPath(unityTexturePath);
				int mainTextureID = 0;
				materialBuilder.AppendLine("[ext_resource type=\"Texture2D\" path=\"" + godotTexturePath + "\" id=\"" + mainTextureID + "\"]");
				materialBuilder.AppendLine();

				materialBuilder.AppendLine("[resource]");
				materialBuilder.AppendLine("albedo_texture = ExtResource(\"" + mainTextureID + "\")");
			} else {
				Color color = unityMaterial.GetColor("_BaseColor");
				materialBuilder.AppendLine("[resource]");
				materialBuilder.AppendLine($"albedo_color = Color({color.r}, {color.g}, {color.b}, {color.a})");
			}
		}

		if(unityShader == "Synty/Generic_Basic_Bloody") {
			const int shaderID = 0;
			materialBuilder.AppendLine($"[ext_resource type=\"Shader\" path=\"res://Synty/Custom/DetailsMap.gdshader\" id=\"{shaderID}\"]");
			Texture mainTexture = FindMainTexture(unityMaterial);
			Texture detailsTexture = unityMaterial.GetTexture("_Blood_Mask");
			Color detailsColor = unityMaterial.GetColor("_Blood_Color");
			if(!mainTexture || !detailsTexture) {
				Debug.LogWarning("Can not create Material " + unityMaterial + ". Albedo or blood mask texture missing");
				return "";
			}
			string unityMainTexturePath = AssetDatabase.GetAssetPath(mainTexture);
			string unityDetailTexturePath = AssetDatabase.GetAssetPath(detailsTexture);
			resources_to_copy.Add(unityMainTexturePath);
			resources_to_copy.Add(unityDetailTexturePath);
			string godotMainTexturePath = BuildGodotPath(unityMainTexturePath);
			string godotDetailTexturePath = BuildGodotPath(unityDetailTexturePath);
			int mainTextureID = 1;
			int detailsTextureID = 2;
			materialBuilder.AppendLine("[ext_resource type=\"Texture2D\" path=\"" + godotMainTexturePath + "\" id=\"" + mainTextureID + "\"]");
			materialBuilder.AppendLine("[ext_resource type=\"Texture2D\" path=\"" + godotDetailTexturePath + "\" id=\"" + detailsTextureID + "\"]");
			materialBuilder.AppendLine();	
			
			materialBuilder.AppendLine("[resource]");
			materialBuilder.AppendLine($"shader = ExtResource(\"{shaderID}\")");
			materialBuilder.AppendLine($"shader_parameter/texture_albedo = ExtResource(\"{mainTextureID}\")");
			materialBuilder.AppendLine($"shader_parameter/texture_details = ExtResource(\"{detailsTextureID}\")");
			materialBuilder.AppendLine($"shader_parameter/details_color = Color({detailsColor.r}, {detailsColor.g}, {detailsColor.b}, {detailsColor.a})");
		}

		if(unityShader == "Unlit/Color") {
			Color color = unityMaterial.color;
			materialBuilder.AppendLine("[resource]");
			materialBuilder.AppendLine("shading_mode = 0          ; SHADING_MODE_UNSHADED");
			materialBuilder.AppendLine($"albedo_color = Color({color.r}, {color.g}, {color.b}, {color.a})");
		}
		
		materialBuilder.AppendLine();

		WriteFile(materialPath, materialBuilder.ToString());

		return materialPath;
	}

	private void SaveScene(GodotScene godotScene, string assetPath, string sourceExtension) {
		assetPath = assetPath.Replace(sourceExtension, ".tscn");
		WriteFile(assetPath, godotScene.AssembleTSCN());
	}

	private int AddExtResource(GodotScene godotScene, string unityPath, string type) {
		string godotPath = BuildGodotPath(unityPath);
		GodotScene.ExternalResource extResource = new GodotScene.ExternalResource();
		extResource.type = type;
		extResource.path = godotPath;
		int index = godotScene.ext_resources.IndexOf(extResource);
		if(index == -1) {
			godotScene.ext_resources.Add(extResource);
			index = godotScene.ext_resources.Count - 1;
		}
		return index;
	}

	private int AddSubResource(GodotScene godotScene, string type, List<string> properties) {
		int subResourceIndex = godotScene.sub_resources.Count;
		StringBuilder subResourceBuilder = new StringBuilder();
		subResourceBuilder.AppendLine("[sub_resource type=\"" + type + "\" id=\"" + subResourceIndex + "\"]");
		foreach(string property in properties) {
			subResourceBuilder.AppendLine(property);
		}
		godotScene.sub_resources.Add(subResourceBuilder.ToString());
		return subResourceIndex;
	}

	private void AddNode(GodotScene godotScene,
						GameObject gameObject,
						GodotScene.Node parent,
						bool isCreatingPrefab,
						GodotScene.Node editableNode) {
						
		GodotScene.Node newNode = new GodotScene.Node(gameObject.name);
		parent.children.Add(newNode);
		GameObject source = PrefabUtility.GetCorrespondingObjectFromSource(gameObject);
		//prefab export
		bool isRootPrefab = PrefabUtility.IsAnyPrefabInstanceRoot(gameObject);
		PrefabAssetType prefabType = PrefabUtility.GetPrefabAssetType(gameObject);
		//nested instance roots are already part of the outer prefab scene, only outermost ones get instanced
		// if(PrefabUtility.IsOutermostPrefabInstanceRoot(gameObject)) {
		if(isRootPrefab) {
			string prefabPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(gameObject);
			if(prefabType == PrefabAssetType.Regular) {
				CreateSceneFromPrefab(prefabPath);
				prefabPath = prefabPath.Replace(".prefab", ".tscn");
			}
			if(prefabType == PrefabAssetType.Model) {
				CreateModelImportFile(prefabPath);
			}
			newNode.instance = AddExtResource(godotScene, prefabPath, "PackedScene");
			isCreatingPrefab = false;
		} else if(source == null && !isCreatingPrefab) {
			//plain scene object, not part of any prefab/model instance
			newNode.type = GodotScene.Node.Type.Node3D;
		}
		//visibility override check
		if(source == null ? gameObject.activeSelf == false : gameObject.activeSelf != source.activeSelf) {
			string visibleProperty = "visible = ";
			visibleProperty += gameObject.activeSelf ? "true" : "false";
			newNode.properties.Add(visibleProperty);
			if(editableNode != null) {
				editableNode.is_editable = true;
			}
		}
		
		AddTransform(newNode, gameObject, source, isRootPrefab, editableNode);
		
		if(!isRootPrefab) {
			AddMaterial(godotScene, newNode, gameObject, isCreatingPrefab, editableNode);
		}
		
		if(isCreatingPrefab) {
			AddCollisions(godotScene, newNode, gameObject, editableNode);
		}
		
		if(isRootPrefab) {
			editableNode = newNode;
			GodotScene.Node intermediateNode = new GodotScene.Node(source.name);
			newNode.children.Add(intermediateNode);
			newNode = intermediateNode;
			
			AddMaterial(godotScene, intermediateNode, gameObject, isCreatingPrefab, editableNode);
		}
		
		foreach(GameObject child in GetChildren(gameObject)) {
			AddNode(godotScene, child, newNode, isCreatingPrefab, editableNode);
		}
	}

	private void CreateModelImportFile(string modelPath) {
		if(!resources_to_copy.Add(modelPath)) {
			return;
		}
		const bool overrideImportFileIfExists = false;
		string importFile = modelPath + ".import";
		if(!overrideImportFileIfExists && File.Exists(BuildExportPath(importFile))) {
			return;
		}

		StringBuilder importBuilder = new StringBuilder();
		importBuilder.AppendLine("[remap]");
		importBuilder.AppendLine();
		importBuilder.AppendLine("importer=\"scene\"");
		importBuilder.AppendLine();
		importBuilder.AppendLine("[params]");
		importBuilder.AppendLine("fbx/importer=0");
		importBuilder.AppendLine("nodes/use_name_suffixes=false");
		importBuilder.AppendLine("animation/import=false");

		importBuilder.AppendLine("_subresources={");
		importBuilder.AppendLine("\"nodes\": {");
		importBuilder.AppendLine("\"PATH:AnimationPlayer\": {");
		importBuilder.AppendLine("\"import/skip_import\": true");
		importBuilder.AppendLine("}");
		importBuilder.AppendLine("}");
		importBuilder.AppendLine("}");
		
		
		WriteFile(importFile, importBuilder.ToString());
	}

	private void AddTransform(GodotScene.Node node, GameObject gameObject, GameObject sourceObject, bool isRootPrefab, GodotScene.Node editableNode) {
		bool shouldWriteTransform = false;
		if(sourceObject && !isRootPrefab) {
			shouldWriteTransform = gameObject.transform.localPosition != sourceObject.transform.localPosition 
			                       || gameObject.transform.localRotation != sourceObject.transform.localRotation 
			                       || gameObject.transform.localScale != sourceObject.transform.localScale;
			if(editableNode != null && shouldWriteTransform) {
				editableNode.is_editable = true;
			}
		} else {
			shouldWriteTransform = true;
		}
		if(shouldWriteTransform) {
			Transform t = gameObject.transform;
			Matrix4x4 localTransformMatrix = Matrix4x4.TRS(t.localPosition, t.localRotation, t.localScale);
			AddTransform(node, localTransformMatrix);
		}
	}

	private void AddTransform(GodotScene.Node node, Matrix4x4 localTransformMatrix) {
		Matrix4x4 conversionMatrix = Matrix4x4.identity;
		conversionMatrix.m00 = -1.0f;
		localTransformMatrix = conversionMatrix * localTransformMatrix * conversionMatrix;
		string transformProperty = string.Format(
			"transform = Transform3D({0}, {1}, {2}, {3}, {4}, {5}, {6}, {7}, {8}, {9}, {10}, {11})",
			localTransformMatrix.m00, localTransformMatrix.m01, localTransformMatrix.m02, localTransformMatrix.m10,
			localTransformMatrix.m11, localTransformMatrix.m12, localTransformMatrix.m20,
			localTransformMatrix.m21, localTransformMatrix.m22, localTransformMatrix.m03, localTransformMatrix.m13,
			localTransformMatrix.m23);
		node.properties.Add(transformProperty);
	}

	private void AddCollisions(GodotScene godotScene, GodotScene.Node node, GameObject gameObject, GodotScene.Node editableNode) {
		MeshCollider[] meshColliders = gameObject.GetComponents<MeshCollider>();
		BoxCollider[] boxColliders = gameObject.GetComponents<BoxCollider>();
		SphereCollider[] sphereColliders = gameObject.GetComponents<SphereCollider>();
		CapsuleCollider[] capsuleColliders = gameObject.GetComponents<CapsuleCollider>();
		if(meshColliders.Length > 0 || boxColliders.Length > 0 || capsuleColliders.Length > 0 || sphereColliders.Length > 0) {
			GodotScene.Node staticBodyNode = new GodotScene.Node("StaticBody");
			staticBodyNode.type = GodotScene.Node.Type.StaticBody3D;
			node.children.Add(staticBodyNode);
			
			for(int i = 0; i < boxColliders.Length; i++) {
				BoxCollider boxCollider = boxColliders[i];
				Vector3 size = boxCollider.size;
				List<string> properties = new List<string>();
				string sizeProperty = $"size = Vector3( {size.x}, {size.y}, {size.z} )";
				properties.Add(sizeProperty);
				int subResourceId = AddSubResource(godotScene, "BoxShape3D", properties);
				GodotScene.Node boxShapeNode = new GodotScene.Node("BoxShape " + i);
				boxShapeNode.type = GodotScene.Node.Type.CollisionShape3D;
				string shapeProperty = "shape = SubResource(\"" + subResourceId + "\")";
				boxShapeNode.properties.Add(shapeProperty);
				
				Vector3 position = boxCollider.center;
				Matrix4x4 localTransformMatrix = Matrix4x4.TRS(position, Quaternion.identity, Vector3.one);
				AddTransform(boxShapeNode, localTransformMatrix);
				
				staticBodyNode.children.Add(boxShapeNode);
			}

			for(int i = 0; i < capsuleColliders.Length; i++) {
				CapsuleCollider capsuleCollider = capsuleColliders[i];
				float radius = capsuleCollider.radius;
				//Unity treats a capsule shorter than its diameter as a sphere, Godot requires height >= 2 * radius
				float height = Mathf.Max(capsuleCollider.height, radius * 2.0f);
				List<string> properties = new List<string>();
				properties.Add($"radius = {radius}");
				properties.Add($"height = {height}");
				int subResourceId = AddSubResource(godotScene, "CapsuleShape3D", properties);
				GodotScene.Node capsuleShapeNode = new GodotScene.Node("CapsuleShape " + i);
				capsuleShapeNode.type = GodotScene.Node.Type.CollisionShape3D;
				string shapeProperty = "shape = SubResource(\"" + subResourceId + "\")";
				capsuleShapeNode.properties.Add(shapeProperty);

				//Godot capsules always run along Y, Unity's direction picks the axis (0 - X, 1 - Y, 2 - Z)
				Quaternion rotation = Quaternion.identity;
				if(capsuleCollider.direction == 0) {
					rotation = Quaternion.Euler(0.0f, 0.0f, 90.0f);
				} else if(capsuleCollider.direction == 2) {
					rotation = Quaternion.Euler(90.0f, 0.0f, 0.0f);
				}
				Vector3 position = capsuleCollider.center;
				Matrix4x4 localTransformMatrix = Matrix4x4.TRS(position, rotation, Vector3.one);
				AddTransform(capsuleShapeNode, localTransformMatrix);

				staticBodyNode.children.Add(capsuleShapeNode);
			}

			for(int i = 0; i < sphereColliders.Length; i++) {
				SphereCollider sphereCollider = sphereColliders[i];
				List<string> properties = new List<string>();
				properties.Add($"radius = {sphereCollider.radius}");
				int subResourceId = AddSubResource(godotScene, "SphereShape3D", properties);
				GodotScene.Node sphereShapeNode = new GodotScene.Node("SphereShape " + i);
				sphereShapeNode.type = GodotScene.Node.Type.CollisionShape3D;
				string shapeProperty = "shape = SubResource(\"" + subResourceId + "\")";
				sphereShapeNode.properties.Add(shapeProperty);

				Vector3 position = sphereCollider.center;
				Matrix4x4 localTransformMatrix = Matrix4x4.TRS(position, Quaternion.identity, Vector3.one);
				AddTransform(sphereShapeNode, localTransformMatrix);

				staticBodyNode.children.Add(sphereShapeNode);
			}

			for (int i = 0; i < meshColliders.Length; i++) {
				MeshCollider meshCollider = meshColliders[i];
				string resourceType = meshCollider.convex ? "ConvexPolygonShaper3D" : "ConcavePolygonShaper3D";
				string shapePath = CreateCollisionShape(meshCollider.sharedMesh, meshCollider.convex, resourceType);
				int shapeID = AddExtResource(godotScene, shapePath, resourceType);
				GodotScene.Node collisionShapeNode = new GodotScene.Node("MeshShape " + i);
				collisionShapeNode.type = GodotScene.Node.Type.CollisionShape3D;
				string shapeProperty = "shape = ExtResource(\"" + shapeID + "\")";
				collisionShapeNode.properties.Add(shapeProperty);
				staticBodyNode.children.Add(collisionShapeNode);
			}
		}
	}

	private string CreateCollisionShape(Mesh mesh, bool isConvex, string resourceType) {
		string meshPath = AssetDatabase.GetAssetPath(mesh);
		Object[] objects = AssetDatabase.LoadAllAssetsAtPath(meshPath);
		IEnumerable<Mesh> meshes = objects.OfType<Mesh>();
		IEnumerable<long> ids = meshes.Select(m => {
			AssetDatabase.TryGetGUIDAndLocalFileIdentifier(m, out string _, out long id);
			return id;
		});
		ids = ids.OrderBy(id => id);
		List<long> sortedIDs = ids.ToList();

		AssetDatabase.TryGetGUIDAndLocalFileIdentifier(mesh, out string _, out long meshID);
		int index = sortedIDs.IndexOf(meshID);
		string baseName = Path.GetFileNameWithoutExtension(meshPath);
		baseName += "." + index;
		baseName += ".tres";
		string basePath = Path.GetDirectoryName(meshPath);
		string shapePath = basePath + "/" + baseName;
		shapePath = shapePath.Replace('\\', '/');

		if(!created_shapes.Add(shapePath)) {
			return shapePath;
		}

		StringBuilder shapeBuilder = new StringBuilder();
		List<Vector3> points = new List<Vector3>();
		if(isConvex) {
			resourceType = "points";
			points.AddRange(mesh.vertices);
			
			shapeBuilder.AppendLine("[gd_resource type=\"ConvexPolygonShape3D\" format=3]");
		} else {
			resourceType = "data";
			int[] triangles = mesh.triangles;
			Vector3[] vertices = mesh.vertices;
			for(int i = 0; i < triangles.Length; i++) {
				Vector3 vertex = vertices[triangles[i]];
				points.Add(vertex);
			}
			
			shapeBuilder.AppendLine("[gd_resource type=\"ConcavePolygonShape3D\" format=3]");
		}
		
		List<string> formatedPoints = points.Select(p => (-p.x) + ", " + p.y + ", " + p.z.ToString()).ToList();
		string pointsData = string.Join(", ", formatedPoints);
		
		shapeBuilder.AppendLine();
		shapeBuilder.AppendLine("[resource]");
		shapeBuilder.AppendLine($"{resourceType} = PackedVector3Array({pointsData})");

		WriteFile(shapePath, shapeBuilder.ToString());
		
		return shapePath;
	}

	private void AddMaterial(GodotScene godotScene, GodotScene.Node node, GameObject gameObject, bool isCreatingPrefab, GodotScene.Node editableNode) {
		MeshRenderer meshRenderer = gameObject.GetComponent<MeshRenderer>();
		if(!meshRenderer) {
			return;
		}

		Material[] originalMaterials = null;
		if(!isCreatingPrefab) {
			GameObject originalPrefabObject = PrefabUtility.GetCorrespondingObjectFromSource(gameObject);
			if(originalPrefabObject) {
				MeshRenderer originalPrefabMeshRenderer = originalPrefabObject.GetComponent<MeshRenderer>();
				if(originalPrefabMeshRenderer) {
					originalMaterials = originalPrefabMeshRenderer.sharedMaterials;
				}
			}
		}

		for(int i = 0; i < meshRenderer.sharedMaterials.Length; i++) {
			Material material = meshRenderer.sharedMaterials[i];
			Material originalMaterial = null;
			if(originalMaterials != null && originalMaterials.Length > i) {
				originalMaterial = originalMaterials[i];
			}
			bool shouldCreateMaterialOverride = isCreatingPrefab || material != originalMaterial;
			if(shouldCreateMaterialOverride) {
				string materialPath = CreateMaterial(material);
				if(materialPath == "") {
					return;
				}
				int extResID = AddExtResource(godotScene, materialPath, "Material");
				string materialProperty = "surface_material_override/" + i + " = ExtResource(\"" + extResID + "\")";
				node.properties.Add(materialProperty);
				if(editableNode != null) {
					editableNode.is_editable = true;
				}
			}
		}
	}

	private string DetectProjectFolder(string startPath) {
		if(Directory.Exists(startPath)) {
			string[] files = Directory.GetFiles(startPath, "*.godot", SearchOption.TopDirectoryOnly);
			if(files.Length > 0) {
				string result = startPath.Replace("\\", "/");
				return result;
			}
		}

		string parent = Directory.GetParent(startPath).FullName;
		return DetectProjectFolder(parent);
	}

	private string BuildGodotPath(string unityPath) {
		string godotPath = unityPath.Substring("Assets".Length);
		godotPath = export_folder + godotPath;
		godotPath = godotPath.Replace(project_folder, "");
		return "res:/" + godotPath;
	}

	private string BuildExportPath(string unityPath) {
		string exportPath = unityPath.Substring("Assets".Length);
		exportPath = export_folder + exportPath;
		return exportPath;
	}

	private void WriteFile(string unityPath, string data) {
		string exportPath = BuildExportPath(unityPath);
		string parentDirectory = Path.GetDirectoryName(exportPath);
		Directory.CreateDirectory(parentDirectory);
		File.WriteAllText(exportPath, data);
	}
}