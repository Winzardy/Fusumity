using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Fusumity.Editor
{
	public class AsmdefFixerTool
	{
		//TODO: добавить очистку не использумых...
		[MenuItem("Tools/Other/Asmdef/Fix Duplicates", priority = 10000)]
		public static void FixDuplicates()
		{
			var files = Directory.GetFiles(Application.dataPath, "*.asmdef", SearchOption.AllDirectories);

			foreach (var file in files)
			{
				var json = File.ReadAllText(file);
				var asmdef = JsonUtility.FromJson<AsmdefData>(json);

				var modified = false;

				if (asmdef.references != null)
				{
					var distinctReferences = asmdef.references.Distinct().ToArray();
					if (distinctReferences.Length != asmdef.references.Length)
					{
						asmdef.references = distinctReferences;
						modified = true;
					}
				}

				if (asmdef.optionalReferences != null)
				{
					var distinctOptionalReferences = asmdef.optionalReferences.Distinct().ToArray();
					if (distinctOptionalReferences.Length != asmdef.optionalReferences.Length)
					{
						asmdef.optionalReferences = distinctOptionalReferences;
						modified = true;
					}
				}

				if (modified)
				{
					var newJson = JsonUtility.ToJson(asmdef, true);
					File.WriteAllText(file, newJson);
					Debug.Log($"Fixed duplicates in {file}");
				}
			}

			AssetDatabase.Refresh();
			Debug.Log("All .asmdef duplicate references have been fixed.");
		}

		[Serializable]
		private class AsmdefData
		{
			public string name;
			public string rootNamespace;
			public string[] references;
			public string[] includePlatforms;
			public string[] excludePlatforms;
			public bool allowUnsafeCode;
			public bool overrideReferences;
			public string[] precompiledReferences;
			public bool autoReferenced;
			public string[] defineConstraints;
			public VersionDefine[] versionDefines;
			public bool noEngineReferences;
			public string[] optionalReferences;
		}

		[Serializable]
		private class VersionDefine
		{
			public string name;
			public string expression;
			public string define;
		}
	}
}
