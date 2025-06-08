using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Sapientia.Collections;
using Sapientia.Extensions;
using Sapientia.Utility;
using Sapientia.Pooling;
using Sapientia.Reflection;

namespace Analytics
{
	public class AnalyticsManagement : IDisposable
	{
		private CancellationTokenSource _cts;
		private string _cachedIntegrationsDebugMessage;

		private readonly AnalyticsSettings _settings;

		private SimpleList<AnalyticsAggregator> _aggregators;
		private SimpleList<IAnalyticsIntegration> _integrations;

		public event Action<AnalyticsEventArgs> BeforeSend;

		public bool Active => !_integrations.IsNullOrEmpty();

		public AnalyticsManagement(AnalyticsSettings settings)
		{
			_settings = settings;

			_cts = new CancellationTokenSource();
		}

		public async UniTask InitializeAsync(CancellationToken cancellationToken)
		{
			_integrations = new();

			using (ListPool<UniTask>.Get(out var tasks))
			{
				_integrations = new();
				using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(_cts.Token, cancellationToken);
				foreach (var integration in _settings.integrations)
					tasks.Add(InitializeIntegrationAsync(integration, linkedCts.Token));

				await UniTask.WhenAll(tasks);
			}
		}

		public void Dispose()
		{
			AsyncUtility.Trigger(ref _cts);

			if (_integrations.IsNullOrEmpty())
				return;

			foreach (var integration in _integrations)
				integration.Dispose();

			_integrations = null;

			foreach (var aggregator in _aggregators)
				aggregator.Dispose();

			_aggregators = null;
		}

		internal bool TryRegister(Type type, out AnalyticsAggregator aggregator)
		{
			aggregator = null;

			if (_settings.disableAggregators.Contains(type.FullName))
				return false;

			aggregator = type.CreateInstance<AnalyticsAggregator>();
			_aggregators ??= new();
			_aggregators.Add(aggregator);

			return true;
		}

		internal void Send(ref AnalyticsEventArgs args)
		{
			BeforeSend?.Invoke(args);

			foreach (var integration in _integrations)
				integration.SendEvent(in args);

			AnalyticsDebug.Log($"Sent event: {args}\n{_cachedIntegrationsDebugMessage}");
		}

		private async UniTask InitializeIntegrationAsync(IAnalyticsIntegration integration, CancellationToken cancellationToken)
		{
			try
			{
				await integration.InitializeAsync(cancellationToken);
				_integrations.Add(integration);

				AnalyticsDebug.Log($"[ {GetDebugNameIntegration(integration)} ] integration initialized");
				_cachedIntegrationsDebugMessage = $"Integrations:{_integrations.GetCompositeString(getter: GetDebugNameIntegration)}";
			}
			catch (OperationCanceledException o)
			{
				AnalyticsDebug.LogWarning($"[ {GetDebugNameIntegration(integration)} ] integration initialization canceled");
			}
			catch (Exception e)
			{
				AnalyticsDebug.LogException(e);
			}
		}

		private string GetDebugNameIntegration(IAnalyticsIntegration integration) =>
			integration.GetType().Name.Replace("AnalyticsIntegration", string.Empty);
	}
}
