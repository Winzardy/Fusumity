using System;
using System.Linq;
using Fusumity.Attributes;
using Sapientia.Pooling;
using Sapientia.Reflection;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Content.ScriptableObjects.Editor
{
	public partial class ContentDatabaseExport
	{
		/// <see cref="ContentDatabaseExport._args"/>
		public const string ARGS_FIELD_NAME = "_args";

		[OnValueChanged(nameof(OnTypeChanged))]
		public Type type = typeof(ContentDatabaseJsonFileExporter);

		[DarkCardBox]
		[SerializeField, SerializeReference]
		protected IContentDatabaseExporterArgs _args = new ContentDatabaseJsonFileExporter.Args();

		internal void Export() => Export(_args.ExporterType, _args);

		public void Export<T>(IContentDatabaseExporterArgs args = null)
			where T : IContentDatabaseExporter
			=> Export(typeof(T), args);

		private void Export(Type type, IContentDatabaseExporterArgs args)
		{
			if (type == null)
				throw ContentDebug.Exception("Invalid exporter type!");

			var exporter = type.CreateInstance<IContentDatabaseExporter>();
			using (ListPool<ContentDatabaseScriptableObject>.Get(out var filtered))
			{
				foreach (var database in ContentDatabaseEditorUtility.Databases)
				{
					if (Asset.projectSettings.skipDatabases.Contains(database.name))
						continue;

					filtered.Add(database);
				}

				args ??= new DefaultExporterArgs();
				args.Databases = filtered;
				exporter.Export(Asset._args);
			}
		}

		private void OnTypeChanged()
		{
			_args = null;

			var baseType = this.type?.BaseType;

			if (baseType is not {IsGenericType: true})
				return;

			var arguments = baseType.GetGenericArguments();

			if (arguments.Length < 2)
				return;

			var type = arguments[1];
			_args = type.CreateInstance<IContentDatabaseExporterArgs>();
		}

		internal static bool UseExportOnBuild => Asset.projectSettings.exportOnBuild;
		internal static void DefaultExport() => Asset.Export();
	}
}
