using Fusumity.Editor.Utility;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;

namespace Content.ScriptableObjects.Editor
{
	[HideMonoScript]
	public partial class ContentDatabaseExport : ScriptableObject
	{
		private const string ROOT_FOLDER = "Assets/ProjectSettings/";
		private const string FOLDER = ROOT_FOLDER + "Content";
		private const string FORMAT = ".asset";
		private const string PATH = FOLDER + "/" + nameof(ContentDatabaseExport) + FORMAT;

		public static ContentDatabaseExport Asset
		{
			get
			{
				var scriptableObject = AssetDatabase.LoadAssetAtPath<ContentDatabaseExport>(PATH);
				if (!scriptableObject)
				{
					AssetDatabaseUtility.EnsureOrCreateFolder(FOLDER);
					scriptableObject = CreateInstance<ContentDatabaseExport>();
					AssetDatabase.CreateAsset(scriptableObject, PATH);
					AssetDatabase.SaveAssets();
				}

				return scriptableObject;
			}
		}
	}
}
