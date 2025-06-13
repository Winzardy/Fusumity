using System;
using System.Linq;
using Fusumity.Attributes;
using Sapientia.Pooling;
using Sapientia.Reflection;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;

namespace Content.ScriptableObjects.Editor
{
	public partial class ContentDatabaseExport
	{
		public const string DISPLAY_PROGRESS_TITLE = "Export Content";
		public const string ARGS_FIELD_NAME = nameof(_exportArgs);

		[OnValueChanged(nameof(OnTypeChanged))]
		public Type type = typeof(ContentDatabaseJsonFileExporter);

		[FormerlySerializedAs("_args")]
		[DarkCardBox]
		[SerializeField, SerializeReference]
		protected IContentDatabaseExporterArgs _exportArgs = new ContentDatabaseJsonFileExporter.Args();

		internal void Export()
		{
			_exportArgs.BuildOutputPath = null;
			Export(_exportArgs.ExporterType, _exportArgs);
		}

		internal void Export(string buildOutputPath)
		{
			_exportArgs.BuildOutputPath = buildOutputPath;
			Export(_exportArgs.ExporterType, _exportArgs);
		}

		public void Export<T>(IContentDatabaseExporterArgs args = null)
			where T : IContentDatabaseExporter
			=> Export(typeof(T), args);

		private void Export(Type type, IContentDatabaseExporterArgs args)
		{
			try
			{
				if (type == null)
					throw ContentDebug.Exception("Invalid exporter type!");

				var exporter = type.CreateInstance<IContentDatabaseExporter>();
				using (ListPool<ContentDatabaseScriptableObject>.Get(out var filtered))
				{
					foreach (var database in ContentDatabaseEditorUtility.Databases)
					{
						if (Settings.contentFiltering.skipDatabases
						   .Contains(database.name))
							continue;

						filtered.Add(database);
					}

					args ??= new DefaultExporterArgs();
					args.Databases = filtered;

					exporter.Export(Asset._exportArgs);
				}
			}
			catch (Exception e)
			{
				ContentDebug.LogException(e);
			}
			finally
			{
				EditorUtility.ClearProgressBar();
			}
		}

		private void OnTypeChanged()
		{
			_exportArgs = null;

			var baseType = this.type?.BaseType;

			if (baseType is not {IsGenericType: true})
				return;

			var arguments = baseType.GetGenericArguments();

			if (arguments.Length < 2)
				return;

			var type = arguments[1];
			_exportArgs = type.CreateInstance<IContentDatabaseExporterArgs>();
		}

		internal static bool UseExportOnBuild => Settings.exportOnBuild;
		internal static void DefaultExport(string buildOutputPath = null) => Asset.Export(buildOutputPath);
	}
}
