using CSharpFunctionalExtensions;
using Cysharp.Threading.Tasks;
using Sapientia.Localization;
using System.Threading;

namespace Localization
{
	public interface IRemoteLocalizationsWebClient
	{
		UniTask<Result<RemoteLocalizationRequestResult>> RequestRemoteLocalization(string id, CancellationToken ct);
	}
}
