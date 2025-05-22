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
				if (TryExtractEntry(database, out var entry))
					entries.Add(entry);

				foreach (var scriptableObject in database.scriptableObjects)
				{
					if (TryExtractEntry(scriptableObject, out entry))
						entries.Add(entry);
				}

				bool TryExtractEntry(ContentScriptableObject target, out IContentEntry contentEntry)
				{
					contentEntry = null;
					if (target is IContentEntryScriptableObject scriptableObject)
					{
						contentEntry = scriptableObject.ScriptableContentEntry;
						return true;
					}

					return false;
				}
			}

			return entries;
		}

		public abstract Task<IList<IContentEntry>> ImportAsync(CancellationToken cancellationToken = default);
	}
}
