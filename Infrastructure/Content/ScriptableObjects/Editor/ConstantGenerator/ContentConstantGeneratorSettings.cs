using Content.ScriptableObjects;
using Fusumity;
using Fusumity.Editor.Utility;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;

namespace Content.Editor
{
	[HideMonoScript]
	public partial class ContentConstantGeneratorSettings : AdvancedScriptableObject
	{
		private const string ROOT_FOLDER = "Assets/ProjectSettings/";
		private const string FOLDER = ROOT_FOLDER + "Content";
		private const string FORMAT = ".asset";
		private const string PATH = FOLDER + "/" + nameof(ContentConstantGeneratorSettings) + FORMAT;

		[HideLabel]
		public ContentConstantGenerator.ProjectSettings projectSettings;

		public static ContentConstantGeneratorSettings Asset
		{
			get
			{
				var scriptableObject = AssetDatabase.LoadAssetAtPath<ContentConstantGeneratorSettings>(PATH);
				if (!scriptableObject)
				{
					AssetDatabaseUtility.EnsureOrCreateFolder(FOLDER);
					scriptableObject = CreateInstance<ContentConstantGeneratorSettings>();
					AssetDatabase.CreateAsset(scriptableObject, PATH);
					AssetDatabase.SaveAssets();
				}

				return scriptableObject;
			}
		}
	}
}
