using System;
using System.Collections.Generic;
using Fusumity;
using Fusumity.Editor.Utility;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;

namespace Content.ScriptableObjects.Editor
{
	[HideMonoScript]
	public partial class ContentValidationSettings : AdvancedScriptableObject
	{
		private const string ROOT_FOLDER = "Assets/ProjectSettings/";
		private const string FOLDER = ROOT_FOLDER + "Content";
		private const string FORMAT = ".asset";
		private const string PATH = FOLDER + "/" + nameof(ContentValidationSettings) + FORMAT;

		[HideLabel]
		public ProjectSettings settings = new();

		public static ProjectSettings Settings
		{
			get
			{
				var asset = Asset;
				asset.settings ??= new ProjectSettings();
				return asset.settings;
			}
		}

		public static ContentValidationSettings Asset
		{
			get
			{
				var scriptableObject = AssetDatabase.LoadAssetAtPath<ContentValidationSettings>(PATH);
				if (!scriptableObject)
				{
					AssetDatabaseUtility.EnsureOrCreateFolder(FOLDER);
					scriptableObject = CreateInstance<ContentValidationSettings>();
					AssetDatabase.CreateAsset(scriptableObject, PATH);
					AssetDatabase.SaveAssets();
				}

				return scriptableObject;
			}
		}

		[Serializable]
		public class ProjectSettings
		{
			[SerializeReference]
			[ListDrawerSettings(Expanded = true)]
			public List<IContentValueValidator> customValidators;
		}
	}
}
