using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Content.Management;

namespace Content.ScriptableObjects
{
	public abstract class ClientContentImporter : IContentImporter
	{
		protected IList<IContentEntry> Extract(IList<ContentDatabaseScriptableObject> data)
		{
			var entries = new List<IContentEntry>();

			foreach (var database in data)
			{
				if (TryImport(database, out var entry))
					entries.Add(entry);

				foreach (var scriptableObject in database.scriptableObjects)
				{
					if (TryImport(scriptableObject, out entry))
						entries.Add(entry);
				}

				bool TryImport(ContentScriptableObject target, out IContentEntry entry)
				{
					entry = target.Import();
					return entry != null;
				}
			}

			return entries;
		}

		public abstract Task<IList<IContentEntry>> ImportAsync(CancellationToken cancellationToken = default);
	}
}
