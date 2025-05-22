using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Content.ScriptableObjects.Editor;

namespace Content.ScriptableObjects
{
	public class EditorClientContentImporter : ClientContentImporter
	{
		public override Task<IList<IContentEntry>> ImportAsync(CancellationToken cancellationToken = default)
		{
			var data = ContentDatabaseEditorUtility.Databases;

			//TODO: нужно эту валидацию добавить в тесты при билде!
			foreach (var database in data)
				if (!database.Validate(out var message))
					ContentDebug.LogError($"Invalid database: {message}", database);

			if (ClientEditorContentImporterMenu.IsEnable)
				data.SyncContent();

			return Task.FromResult(Extract(data));
		}
	}
}
