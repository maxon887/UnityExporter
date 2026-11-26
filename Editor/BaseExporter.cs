using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using Debug = UnityEngine.Debug;

public abstract class BaseExporter {

	protected string export_folder;
	protected string project_folder;
	protected HashSet<string> resources_to_copy = new HashSet<string>();

	public void Export(string exportFolder, bool exportSelected) {
		long start  = Stopwatch.GetTimestamp();
		
		export_folder = exportFolder;
		if(exportSelected) {
			Export(Selection.gameObjects);
		} else {
			Export(SceneManager.GetActiveScene().GetRootGameObjects());
		}
		
		long elapsedTicks = Stopwatch.GetTimestamp() - start;
		double elapsedMs = elapsedTicks * 1000.0 / Stopwatch.Frequency;
		Debug.Log($"Export Finished With: {elapsedMs:F1} ms");
	}

	protected virtual void Export(GameObject[] rootObjects = null) {
		foreach(string asset in resources_to_copy) {
			CopyAsset(asset, export_folder);
		}

	}
	
	protected GameObject[] GetChildren(GameObject parent) {
		Transform parentTransform = parent.transform;
		GameObject[] children = new GameObject[parentTransform.childCount];
		for (int i = 0; i < parentTransform.childCount; i++) {
			children[i] = parentTransform.GetChild(i).gameObject;
		}
		return children;
	}
	
	protected Texture FindMainTexture(Material material) {
		string[] PossibleTexturesNames =
		{
			"_MainTex",      // Built-in Standard
			"_BaseMap",      // URP Lit
			"_BaseColorMap", // HDRP Lit
			"_Albedo_Map",   // Synty
		};
		foreach(string textureName in PossibleTexturesNames) {
			if (material.HasTexture(textureName)) {
				return material.GetTexture(textureName);
			}
		}
		return null;
	}

	protected Color FindMainColor(Material material) {
		string[] PossibleColorsNames =
		{
			"_BaseColor",    // URP/HDRP Lit, Synty
			"_Color",        // Built-in Standard, Unlit/Color
		};
		foreach(string colorName in PossibleColorsNames) {
			if (material.HasColor(colorName)) {
				return material.GetColor(colorName);
			}
		}
		return Color.white;
	}

    private void CopyAsset(string asset, string exportFolder) {
		string assetPath = asset.Substring("Assets".Length);
	    string destinationPath = exportFolder + assetPath;
	    string sourcePath = Application.dataPath + assetPath;
	    
	    string parentDirectory = Path.GetDirectoryName(destinationPath);
	    Directory.CreateDirectory(parentDirectory);
	    
	    File.Copy(sourcePath, destinationPath, true);
    }
}
