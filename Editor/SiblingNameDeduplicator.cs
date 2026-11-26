using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

public static class SiblingNameDeduplicator {

	public static int Deduplicate(List<Transform> siblings) {
		int renamed = 0;

		HashSet<string> usedNames = new HashSet<string>();
		HashSet<string> existingNames = new (siblings.Select(s => s.name));
		
		foreach(Transform sibling in siblings) {
			if(!usedNames.Add(sibling.name)) {
				string baseName = StripNumberSuffix(sibling.name);
				int number = 1;
				string newName;
				do {
					newName = baseName + " (" +  number + ")";
					number++;
				} while(usedNames.Contains(newName) || existingNames.Contains(newName));
				usedNames.Add(newName);
				Undo.RecordObject(sibling.gameObject, "Make Sibling Names Unique");
				Debug.Log($"Renamed: {GetPath(sibling)} -> '{newName}'", sibling.gameObject);
				sibling.name = newName;
				renamed++;
			}

			renamed += Deduplicate(GetChildren(sibling));
		}
		
		return renamed;
	}
	
	private static string StripNumberSuffix(string name) {
		Match match = Regex.Match(name, @"^(.*) \(\d+\)$");
		return match.Success ? match.Groups[1].Value : name;
	}
	
	private static string GetPath(Transform t) {
		return t.parent ? GetPath(t.parent) + "/" + t.name : t.name;
	}
	
	private static List<Transform> GetChildren(Transform parent) {
		List<Transform> children = new List<Transform>();
		for (int i = 0; i < parent.childCount; i++) {
			children.Add(parent.GetChild(i));
		}
		return children;
	}
}