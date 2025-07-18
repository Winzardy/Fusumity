using Fusumity;
using Fusumity.Editor.Utility;
using Sirenix.OdinInspector;
using UnityEditor;

namespace Localization.Editor
{
	[HideMonoScript]
	public partial class LocalizationConstantGeneratorSettings : AdvancedScriptableObject
	{
		private const string ROOT_FOLDER = "Assets/ProjectSettings/";
		private const string FOLDER = ROOT_FOLDER + "Localization";
		private const string FORMAT = ".asset";
		private const string PATH = FOLDER + "/" + nameof(LocalizationConstantGeneratorSettings) + FORMAT;

		[HideLabel]
		public LocalizationConstantGenerator.ProjectSettings settings;

		public static LocalizationConstantGeneratorSettings Asset
		{
			get
			{
				var scriptableObject = AssetDatabase.LoadAssetAtPath<LocalizationConstantGeneratorSettings>(PATH);
				if (!scriptableObject)
				{
					AssetDatabaseUtility.EnsureOrCreateFolder(FOLDER);
					scriptableObject = CreateInstance<LocalizationConstantGeneratorSettings>();
					AssetDatabase.CreateAsset(scriptableObject, PATH);
					AssetDatabase.SaveAssets();
				}

				return scriptableObject;
			}
		}
	}
}
