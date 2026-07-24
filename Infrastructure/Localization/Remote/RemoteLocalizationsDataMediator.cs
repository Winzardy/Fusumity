using CSharpFunctionalExtensions;
using Cysharp.Threading.Tasks;
using Sapientia.Localization;
using Sapientia.ServiceManagement;
using System;
using System.Collections.Generic;
using System.Threading;

namespace Localization
{
	public interface IRemoteLocalizationsDataMediator
	{
		event Action<RemoteLocalizationRequestResult> RemoteLocalizationReceived;

		UniTask<Result<string>> LoadRemoteLocalization(string id);
	}

	public class RemoteLocalizationsDataMediator
	{
		private IRemoteLocalizationsWebClient _webClient;

		private CancellationTokenSource _cts = new CancellationTokenSource();
		private readonly Dictionary<string, UniTaskCompletionSource<Result<string>>> _requests = new Dictionary<string, UniTaskCompletionSource<Result<string>>>();

		public event Action<RemoteLocalizationRequestResult> RemoteLocalizationReceived;

		public RemoteLocalizationsDataMediator()
		{
			ServiceLocator.Get(out _webClient);
		}

		public UniTask<Result<string>> LoadRemoteLocalization(string id)
		{
			if (_cts == null)
				return UniTask.FromResult(Result.Failure<string>("Mediator disposed."));

			return RequestOnceAsync(id, Routine, "Single remote localization request");
			async UniTask<Result<string>> Routine(string id, CancellationToken ct)
			{
				var result = await _webClient.RequestRemoteLocalization(id, ct);

				if (result.IsSuccess)
				{
					var localizationResult = result.Value;
					if (!localizationResult.EmbeddedKeyPresentInBuild())
					{
						if (!RemoteLocalizationsUtility.TryAddRemoteStrings(localizationResult))
						{
							return Result.Failure<string>($"Received invalid remote localization for [ {id} ]");
						}
					}

					if (localizationResult.TryExtractValidLocKey(out var locKey))
					{
						RemoteLocalizationReceived?.Invoke(localizationResult);
						return Result.Success<string>(locKey);
					}
					else
					{
						return Result.Failure<string>($"Could not handle remote localization for [ {id} ] correctly.");
					}
				}
				else
				{
					return Result.Failure<string>($"Remote localization request for [ {id} ] failed. {result.Error}");
				}
			}
		}

		private UniTask<Result<string>> RequestOnceAsync(string id, Func<string, CancellationToken, UniTask<Result<string>>> routine, string context)
		{
			if (_requests.TryGetValue(id, out var pending))
			{
				return pending.Task;
			}

			var completion = new UniTaskCompletionSource<Result<string>>();
			_requests.Add(id, completion);

			RunAsync().Forget();

			return completion.Task;

			async UniTaskVoid RunAsync()
			{
				var result = default(Result<string>);

				try
				{
					result = await routine.Invoke(id, _cts.Token);
				}
				catch (OperationCanceledException)
				{
					result = Result.Failure<string>($"{context} for [ {id} ] cancelled.");
				}
				catch (Exception exception)
				{
					result = Result.Failure<string>($"{context} for [ {id} ] threw. {exception.Message}");
				}
				finally
				{
					_requests.Remove(id);
					completion.TrySetResult(result);
				}
			}
		}
	}
}
