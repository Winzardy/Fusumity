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
			[ListDrawerSettings(Expanded = true)]
			public List<ContentValueValidatorEntry> customValidators;

			internal List<IContentValueValidator> GetEnabledCustomValidators()
			{
				if (customValidators == null || customValidators.Count == 0)
					return null;

				var result = new List<IContentValueValidator>();
				foreach (var entry in customValidators)
				{
					if (entry is not {disable: false, validator: not null})
						continue;

					result.Add(entry.validator);
				}

				return result;
			}

			internal T GetEnabledCustomValidator<T>()
				where T : class, IContentValueValidator
			{
				if (customValidators == null || customValidators.Count == 0)
					return null;

				foreach (var entry in customValidators)
				{
					if (entry is {disable: false, validator: T validator})
						return validator;
				}

				return null;
			}
		}

		[Serializable]
		public struct ContentValueValidatorEntry
		{
			public bool disable;

			[SerializeReference]
			[HideLabel]
			[DisableIf(nameof(disable))]
			public IContentValueValidator validator;
		}
	}
}
