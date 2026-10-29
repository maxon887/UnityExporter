using System.IO;
using UnityEditor;
using UnityEngine;

public class ExporterWindow : EditorWindow
{
	private enum ExportEngine {
		Cross,
		Godot
	}

	private ExportEngine export_engine = ExportEngine.Cross;

	private string export_folder = "";

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
            if(export_engine == ExportEngine.Cross) {
                CrossExporter exporter = new CrossExporter();
                exporter.Export(export_folder);
            }
            if(export_engine == ExportEngine.Godot) {
                GodotExporter exporter = new GodotExporter();
                exporter.Export(export_folder);
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
		if(export_engine == ExportEngine.Cross) {
			EditorPrefs.SetString("cross_export_folder", export_folder);
		}
		if(export_engine == ExportEngine.Godot) {
			EditorPrefs.SetString("godot_export_folder", export_folder);
		}
	}
}
