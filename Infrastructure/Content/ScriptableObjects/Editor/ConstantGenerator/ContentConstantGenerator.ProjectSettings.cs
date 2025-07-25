using System;
using Fusumity.Attributes;
using Fusumity.Collections;
using Sapientia;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Content.ScriptableObjects.Editor
{
	public partial class ContentConstantGenerator
	{
		[Serializable]
		public class ProjectSettings
		{
			//TODO: добавить возможность генерировать не в одну папку, а рядом с классами которые используют константы

			[HideLabel]
			[DarkCardBox]
			public ConstantsOutput output;

			public SerializableDictionary<string, ConstantsOutput> namespaceToOutput = new();

			[Space]
			//TODO: добавлять ли .asmdef (сейчас по дефолту да)
			[Tooltip(
				"Список используемых аббревиатур, которые будут использоваться в генерации (UIObjectDefault -> UI_OBJECT_DEFAULT)")]
			public string[] abbreviations = {"UI", "VFX", "FX", "HUD", "IAP"};

			[TextArea]
			public string scriptComment = "// <auto-generated/>";

			public string scriptFileNamePostfix = "Generated";

			[Space]
			[Tooltip("Window -> Window{ending}")]
			public string classNameEnding = "Type";

			[Tooltip("Чтобы убрать лишние окончания, например: WindowEntry -> Window -> Window{ending}")]
			public string[] removeEndings = {"Entry"};
		}

		internal static ProjectSettings projectSettings => ContentConstantGeneratorSettings.Asset.settings;
	}

	[Serializable]
	public class ConstantsOutput
	{
		[FolderPath]
		[Tooltip("Папка куда сгенерируются константы")]
		public string folderPath = "Assets/Scripts/Content/Constants/";

		public Toggle<string> asmdef = "Content.Constants";

		[Tooltip("Убирает namespace из пути")]
		public bool trimGeneratePath;
	}
}
