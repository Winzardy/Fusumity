using System;
using System.Collections.Generic;
using System.Threading;
using Content;
using Cysharp.Threading.Tasks;
using Fusumity.Reactive;
using JetBrains.Annotations;
using Sapientia;
using Sapientia.Collections;
using Sapientia.Extensions;
using Sapientia.Utility;
using Sapientia.Pooling;

namespace Analytics
{
	public class AnalyticsManagement : IDisposable
	{
		private CancellationTokenSource _cts;
		private string _cachedIntegrationsDebugMessage;

		private readonly AnalyticsSettings _settings;
		private readonly bool _isValidationEnabled;

		private List<AnalyticsAggregator> _registeredAggregators;

		private List<IntegrationHandle> _integrations;

		private DeferredQueue<AnalyticsEventPayload> _deferred;

		public event Receiver<AnalyticsEventPayload> BeforeSend;

		public bool Active => !_integrations.IsNullOrEmpty();

		public AnalyticsManagement(AnalyticsSettings settings, bool isValidationEnabled)
		{
			_settings = settings;
			_isValidationEnabled = isValidationEnabled;

			_cts = new CancellationTokenSource();

			_deferred = new DeferredQueue<AnalyticsEventPayload>(SendDeferred);
		}

		public async UniTask InitializeAsync(CancellationToken cancellationToken)
		{
			_integrations = new();

			using (ListPool<UniTask>.Get(out var tasks))
			{
				_integrations = new();
				using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(_cts.Token, cancellationToken);
				foreach (var config in _settings.integrations)
					tasks.Add(InitializeIntegrationAsync(config, linkedCts.Token));

				await UniTask.WhenAll(tasks);
			}
		}

		public void Dispose()
		{
			AsyncUtility.TriggerAndSetNull(ref _cts);

			DisposeUtility.DisposeAndSetNull(ref _deferred);

			if (_integrations.IsNullOrEmpty())
				return;

			foreach (var handle in _integrations)
				handle.integration.Dispose();

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

			// Логируем до откладывания, пока в стеке виден отправитель
			AnalyticsDebug.Log($"Sent event: {payload}\n{_cachedIntegrationsDebugMessage}");

			if (_deferred.CanHandleNow())
			{
				SendInternal(in payload);
				return;
			}

			// Параметры приходят из пула и вернутся туда сразу после вызова, поэтому копируем в свой словарь
			if (payload.parameters != null)
			{
				var parameters = DictionaryPool<string, object>.Get();
				parameters.AddRange(payload.parameters);
				payload.parameters = parameters;
			}

			_deferred.Enqueue(in payload);
		}

		private void SendDeferred(in AnalyticsEventPayload payload)
		{
			try
			{
				SendInternal(in payload);
			}
			finally
			{
				if (payload.parameters != null)
					DictionaryPool<string, object>.Release(payload.parameters);
			}
		}

		private void SendInternal(in AnalyticsEventPayload payload)
		{
			foreach (var handle in _integrations)
			{
				if (!AnalyticsEventRouter.TryRoute(in handle.config, in payload, out var routed))
					continue;

				if (_isValidationEnabled && !handle.integration.IsValid(in routed, out var error))
				{
					// даже если была ошибка при валидации, то все равно отправляем событие, вдруг мы просто неправильно написали правила валидации
					AnalyticsDebug.LogError($"{GetDebugNameIntegration(handle)} validation failed: {error}\n{routed.ToJson()}");
				}

				handle.integration.SendEvent(in routed);
			}
		}

		private async UniTask InitializeIntegrationAsync(ContentReference<AnalyticsIntegrationConfig> config,
			CancellationToken cancellationToken)
		{
			// Читаем конфиг под try: битая ссылка должна ронять одну интеграцию, а не всю инициализацию
			var handle = default(IntegrationHandle);

			try
			{
				handle = new IntegrationHandle(in config, config.Read().integration);

				if (handle.integration == null)
				{
					AnalyticsDebug.LogError($"Integration is empty in config [ {config.ToId()} ]");
					return;
				}

				await handle.integration.InitializeAsync(cancellationToken);
				_integrations.Add(handle);

				AnalyticsDebug.Log($"[ {GetDebugNameIntegration(handle)} ] integration initialized");
				_cachedIntegrationsDebugMessage = $"Integrations:{_integrations.GetCompositeString(getter: GetDebugNameIntegration)}";
			}
			catch (OperationCanceledException o)
			{
				AnalyticsDebug.LogWarning($"[ {GetDebugNameIntegration(handle)} ] integration initialization canceled");
			}
			catch (Exception e)
			{
				AnalyticsDebug.LogException(e);
			}
		}

		[MustUseReturnValue]
		private string GetDebugNameIntegration(IntegrationHandle handle) =>
			handle.integration?.GetType().Name.Replace("AnalyticsIntegration", string.Empty) ?? "Unknown";

		/// <summary>
		/// Интеграция вместе со своим конфигом: маршрут спрашивают у конфига, а не у самой интеграции
		/// </summary>
		private readonly struct IntegrationHandle
		{
			public readonly ContentReference<AnalyticsIntegrationConfig> config;
			public readonly IAnalyticsIntegration integration;

			public IntegrationHandle(in ContentReference<AnalyticsIntegrationConfig> config, IAnalyticsIntegration integration)
			{
				this.config = config;
				this.integration = integration;
			}
		}
	}
}
