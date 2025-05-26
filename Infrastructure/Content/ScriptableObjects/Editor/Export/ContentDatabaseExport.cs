using System;
using System.Linq;
using Fusumity.Attributes;
using Sapientia.Pooling;
using Sapientia.Reflection;
using Sirenix.OdinInspector;

namespace Content.ScriptableObjects.Editor
{
	[Serializable]
	public struct ContentDatabaseExportOptions
	{
		public string[] skipDatabases;
	}

	public partial class ContentDatabaseExport
	{
		public ContentDatabaseExportOptions options;

		[OnValueChanged(nameof(OnTypeChanged))]
		public Type type = typeof(ContentDatabaseJsonFileExporter);

		[DarkCardBox]
		internal IContentDatabaseExporterArgs _args = new ContentDatabaseJsonFileExporter.Args();

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
					if (options.skipDatabases.Contains(database.name))
						continue;

					filtered.Add(database);
				}

				args ??= new DefaultExporterArgs();
				args.Databases = filtered;
				exporter.Export(_args);
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

		internal void Button() => Export(type, _args);
	}
}
