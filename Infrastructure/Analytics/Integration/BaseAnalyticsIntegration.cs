using System.Threading;
using Cysharp.Threading.Tasks;

namespace Analytics.Integration
{
	public abstract class BaseAnalyticsIntegration : IAnalyticsIntegration
	{
		public UniTask InitializeAsync(CancellationToken cancellationToken) => OnInitializeAsync(cancellationToken);

		public void Dispose() => OnDispose();

		protected virtual void OnDispose()
		{
		}

		protected virtual UniTask OnInitializeAsync(CancellationToken cancellationToken) => UniTask.CompletedTask;

		public abstract void SendEvent(in AnalyticsEventArgs args);
	}
}
