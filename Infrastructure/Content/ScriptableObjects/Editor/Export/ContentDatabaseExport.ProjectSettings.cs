using System;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;

namespace Content.ScriptableObjects.Editor
{
	public partial class ContentDatabaseExport
	{
		[Serializable]
		public class ExportProjectSettings
		{
			public bool exportOnBuild = true;

			[Title("Content Filtering", "фильтруем по контенту", TitleAlignments.Split), HideLabel]
			public ContentFiltering contentFiltering;

			[Title("Type Filtering", "фильтруем по типу", TitleAlignments.Split), HideLabel]
			public ContentTypeFiltering typeFiltering;
		}

		[FormerlySerializedAs("projectSettings")]
		public ExportProjectSettings settings;

		public static ExportProjectSettings Settings => Asset.settings;
	}

	[Serializable]
	public class ContentFiltering
	{
		public string[] skipDatabases;
	}

	[Serializable]
	public class ContentTypeFiltering
	{
		[FormerlySerializedAs("client")]
		[Tooltip("Пропустить типы с [ClientOnly] аттрибутом")]
		public bool skipTypesWithClientOnlyAttribute;

		[Space(5)]
		public string[] skipNamespaces;
		[FormerlySerializedAs("skipNameTags")]
		[Tooltip("Может содержать не полное имя класса, а также его часть")]
		public string[] skipClassNameTags;
	}
}
