using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ExporterWindow : EditorWindow
{
	private enum ExportEngine {
		Cross,
		Godot
	}
	
	[SerializeField]
	private ExportEngine export_engine = ExportEngine.Cross;
	
	[SerializeField]
	private string export_folder = "";

	private bool clear_export_folder = false;
	
	[SerializeField]
	private bool export_selected = false;

    [MenuItem("UnityExporter/Export to Cross")]
    public static void ExportToCross() {
		ExporterWindow window = GetWindow<ExporterWindow>("Exporter");
		window.titleContent = new GUIContent("Cross Exporter");
		window.InitializeForCross();
	}

	[MenuItem("UnityExporter/Export to Godot")]
	public static void ExportToGodot() {
		ExporterWindow window = GetWindow<ExporterWindow>("Exporter");
		window.titleContent = new GUIContent("Godot Exporter");
		window.InitializeForGodot();
	}

	[MenuItem("UnityExporter/Make Unique Siblings Names")]
	public static void MakeUniqueSiblingNames() {
		Scene scene = SceneManager.GetActiveScene();
		GameObject[] roots = scene.GetRootGameObjects();
		List<Transform> rootTransforms = roots.Select(go => go.transform).ToList();
		int renamed = SiblingNameDeduplicator.Deduplicate(rootTransforms);
		Debug.Log($"Renamed {renamed} objects in '{scene.name}'. Save the scene to keep the changes.");
	}
	
	public void OnGUI() {
		EditorGUILayout.BeginHorizontal();
		string editedFolder = EditorGUILayout.DelayedTextField("Export Folder", export_folder);
		if(editedFolder != export_folder) {
			export_folder = editedFolder;
			SaveExportFolder();
		}
		if(GUILayout.Button("Browse", GUILayout.MaxWidth(80))) {
			string directoryToOpen = Directory.Exists(export_folder) ? export_folder : Directory.GetParent(Application.dataPath).FullName;
			string selected = EditorUtility.OpenFolderPanel("Select Export Folder", directoryToOpen, "");
			if(!string.IsNullOrEmpty(selected)) {
				export_folder = selected;
				SaveExportFolder();
				GUI.FocusControl(null);
			}
		}
		EditorGUILayout.EndHorizontal();
		
		clear_export_folder = EditorGUILayout.Toggle("Clear Export Folder", clear_export_folder);
		export_selected = EditorGUILayout.Toggle("Export Selected", export_selected);
		
		if(GUILayout.Button("Export")) {
			BaseExporter exporter = null;
            if(export_engine == ExportEngine.Cross) {
                exporter = new CrossExporter();
            }
            if(export_engine == ExportEngine.Godot) {
                exporter = new GodotExporter();
            }

            try {
	            if(clear_export_folder) {
		            ClearExportFolder();
	            }
	            exporter.Export(export_folder, export_selected);
	            EditorUtility.DisplayDialog("Export Complete", "Export Succeed", "Ok");
            } catch(Exception e) {
	            EditorUtility.DisplayDialog("Export Failed", e.ToString(), "Ok");
	            Console.WriteLine(e);
            }
		}
	}

	private static string GetDefaultExportFolder() {
		string exportFolder = Directory.GetParent(Directory.GetParent(Application.dataPath).FullName).FullName;
		exportFolder += "\\ExporterTest";
		return exportFolder.Replace("\\", "/");
	}

	private void InitializeForCross() {
		export_engine = ExportEngine.Cross;
		export_folder = EditorPrefs.GetString("cross_export_folder", GetDefaultExportFolder());
	}

	private void InitializeForGodot() {
		export_engine = ExportEngine.Godot;
		export_folder = EditorPrefs.GetString("godot_export_folder", GetDefaultExportFolder());
	}

	private void SaveExportFolder() {
		switch(export_engine) {
			case ExportEngine.Cross:
				EditorPrefs.SetString("cross_export_folder", export_folder);
				break;
			case ExportEngine.Godot:
				EditorPrefs.SetString("godot_export_folder", export_folder);
				break;
		}
	}

	private void ClearExportFolder() {
		DirectoryInfo directory = new DirectoryInfo(export_folder);
		foreach(FileInfo file in directory.GetFiles()) {
			file.Delete();
		}

		foreach(DirectoryInfo subDirectory in directory.GetDirectories()) {
			subDirectory.Delete(true);
		}
	}
}
