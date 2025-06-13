using System;
using System.Collections.Generic;
using JetBrains.Annotations;

namespace Content.ScriptableObjects.Editor
{
	public abstract class BaseContentDatabaseExporter : BaseContentDatabaseExporter<DefaultExporterArgs>
	{
	}

	public abstract class BaseContentDatabaseExporter<TArgs> : IContentDatabaseExporter
		where TArgs : IContentDatabaseExporterArgs, new()
	{
		protected abstract void OnExport(ref TArgs args);

		void IContentDatabaseExporter.Export(IContentDatabaseExporterArgs args)
		{
			if (args is DefaultExporterArgs defaultArgs)
			{
				var newArgs = new TArgs
				{
					Databases = defaultArgs.Databases
				};
				OnExport(ref newArgs);
			}else if (args is TArgs typedArgs)
				OnExport(ref typedArgs);
		}
	}

	public interface IContentDatabaseExporter
	{
		public void Export(IContentDatabaseExporterArgs args = null);
	}

	public interface IContentDatabaseExporterArgs
	{
		public List<ContentDatabaseScriptableObject> Databases { get; set; }

		public Type ExporterType { get; }

		[CanBeNull] public string BuildOutputPath { get; set; }
	}

	public struct DefaultExporterArgs : IContentDatabaseExporterArgs
	{
		public List<ContentDatabaseScriptableObject> Databases { get; set; }
		public Type ExporterType => null;
		public string BuildOutputPath { get; set; }
	}
}
