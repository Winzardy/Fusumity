using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AssetManagement;

namespace Content.ScriptableObjects
{
	public class BuildInClientContentImporter : ClientContentImporter
	{
		public override async Task<IList<IContentEntry>> ImportAsync(CancellationToken cancellationToken = default)
		{
			var data = await AssetLoader.LoadAssetsAsync<ContentDatabaseScriptableObject>(ContentDatabaseScriptableObject.LABEL, cancellationToken);
			return ClientContentImporterUtility.Extract(data);
		}
	}
}
