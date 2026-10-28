using System.IO;
using UnityEditor;
using UnityEngine;

public class UnityExporter : EditorWindow
{
	private enum ExportEngine {
		Cross,
		Godot
	}

	private ExportEngine export_engine = ExportEngine.Cross;

	private string export_folder = "";

    [MenuItem("UnityExporter/Export to Cross")]
    public static void ExportToCross() {
		UnityExporter window = GetWindow<UnityExporter>("Exporter");
		window.titleContent = new GUIContent("Cross Exporter");
		window.export_engine = ExportEngine.Cross;
		window.LoadExportFolder();
	}

	[MenuItem("UnityExporter/Export to Godot")]
	public static void ExportToGodot() {
		UnityExporter window = GetWindow<UnityExporter>("Exporter");
		window.titleContent = new GUIContent("Godot Exporter");
		window.export_engine = ExportEngine.Godot;
		window.LoadExportFolder();
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

		if(GUILayout.Button("Export")) {

		}
	}

	private static string GetDefaultExportFolder() {
		string exportFolder = Directory.GetParent(Directory.GetParent(Application.dataPath).FullName).FullName;
		exportFolder += "\\ExporterTest";
		return exportFolder.Replace("\\", "/");
	}

	private void SaveExportFolder() {
		if(export_engine == ExportEngine.Cross) {
			EditorPrefs.SetString("cross_export_folder", export_folder);
		}
		if(export_engine == ExportEngine.Godot) {
			EditorPrefs.SetString("godot_export_folder", export_folder);
		}
	}

	private void LoadExportFolder() {
		if(export_engine == ExportEngine.Cross) {
			export_folder = EditorPrefs.GetString("cross_export_folder", GetDefaultExportFolder());
		}
		if(export_engine == ExportEngine.Godot) {
			export_folder = EditorPrefs.GetString("godot_export_folder", GetDefaultExportFolder());
		}
	}
}
