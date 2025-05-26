using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Content.Management;

namespace Content.ScriptableObjects
{
	public abstract class ClientContentImporter : IContentImporter
	{
		public abstract Task<IList<IContentEntry>> ImportAsync(CancellationToken cancellationToken = default);
	}
}
