using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AssetManagement;

namespace Content.ScriptableObjects
{
	public class ClientContentImporter : BaseClientContentImporter
	{
		private AssetLabelReferenceEntry _label;

		public ClientContentImporter(AssetLabelReferenceEntry label)
		{
			_label = label;
		}

		public override async Task<IList<IContentEntry>> ImportAsync(CancellationToken cancellationToken = default)
		{
			var databases = await AssetLoader.LoadAssetsAsync<ContentDatabaseScriptableObject>(
				_label,
				cancellationToken);

			return ClientContentImporterUtility.Extract(databases);
		}
	}
}
