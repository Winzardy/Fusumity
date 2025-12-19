using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using JetBrains.Annotations;
using Sapientia;
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
		private readonly bool _isValidationEnabled;

		private List<AnalyticsAggregator> _registeredAggregators;

		private List<IAnalyticsIntegration> _integrations;

		public event Receiver<AnalyticsEventPayload> BeforeSend;

		public bool Active => !_integrations.IsNullOrEmpty();

		public AnalyticsManagement(AnalyticsSettings settings, bool isValidationEnabled)
		{
			_settings = settings;
			_isValidationEnabled = isValidationEnabled;

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
			AsyncUtility.TriggerAndSetNull(ref _cts);

			if (_integrations.IsNullOrEmpty())
				return;

			foreach (var integration in _integrations)
				integration.Dispose();

			_integrations = null;
		}

		internal bool Register<T>(T aggregator)
			where T : AnalyticsAggregator
		{
			var type = typeof(T);

			if (_settings.disableAggregators.Contains(type.FullName))
				return false;

			_registeredAggregators ??= new();
			_registeredAggregators.Add(aggregator);

			return true;
		}

		internal bool Unregister<T>(T aggregator) where T : AnalyticsAggregator
			=> _registeredAggregators.Remove(aggregator);

		internal void Send(ref AnalyticsEventPayload payload)
		{
			BeforeSend?.Invoke(payload);

			foreach (var integration in _integrations)
			{
				if (_isValidationEnabled && !integration.IsValid(in payload, out var error))
				{
					// даже если была ошибка при валидации, то все равно отправляем событие, вдруг мы просто неправильно написали правила валидации
					AnalyticsDebug.LogError($"{GetDebugNameIntegration(integration)} validation failed: {error}");
				}

				integration.SendEvent(in payload);
			}

			AnalyticsDebug.Log($"Sent event: {payload}\n{_cachedIntegrationsDebugMessage}");
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

		[MustUseReturnValue]
		private string GetDebugNameIntegration(IAnalyticsIntegration integration) =>
			integration.GetType().Name.Replace("AnalyticsIntegration", string.Empty);
	}
}
