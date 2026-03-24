using System.IO;
using Sapientia;
using Sapientia.Extensions;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using Utils;

namespace ProjectInformation.Editor
{
	public class BuildInfoPreprocessBuild : IPreprocessBuildWithReport
	{
		public int callbackOrder => 0;

		private static string ResourcesFolder = Path.Combine("Assets", "Resources");
		private static string FilePath = Path.Combine(ResourcesFolder, $"{nameof(BuildInfo)}.json");

		public void OnPreprocessBuild(BuildReport report)
		{
			CreateBuildInfoAsset();
		}

		[MenuItem("Tools/Server/Create BuildInfo")]
		private static void CreateBuildInfoAsset()
		{
			Directory.CreateDirectory(ResourcesFolder);
			var buildInfo = BuildInfo.CreateFromGit(GitUtility.GetProjectRoot());

			string json = buildInfo.ToJson(SerializationType.Auto.AddDebugIndent());
			File.WriteAllText(FilePath, json);

			AssetDatabase.Refresh();
			Debug.Log($"[BuildInfo] Created {FilePath}: {buildInfo.ToJson(SerializationType.Auto.AddDebugIndent())}");
		}
	}
}
