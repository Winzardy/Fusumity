using System.Threading;
using Cysharp.Threading.Tasks;

namespace Analytics.Integration
{
	public abstract class BaseAnalyticsIntegration : IAnalyticsIntegration
	{
		private readonly EventArgsValidator _eventArgsValidator;

		protected BaseAnalyticsIntegration(EventArgsValidator eventArgsValidator)
		{
			_eventArgsValidator = eventArgsValidator;
		}

		public UniTask InitializeAsync(CancellationToken cancellationToken) => OnInitializeAsync(cancellationToken);

		public void Dispose() => OnDispose();

		protected virtual void OnDispose()
		{
		}

		protected virtual UniTask OnInitializeAsync(CancellationToken cancellationToken) => UniTask.CompletedTask;

		public abstract void SendEvent(in AnalyticsEventPayload args);

		public bool IsValid(in AnalyticsEventPayload args, out string error) => _eventArgsValidator.IsValid(args, out error);
	}
}
