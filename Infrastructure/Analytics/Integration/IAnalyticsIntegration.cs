using System;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace Analytics
{
	public interface IAnalyticsIntegration : IDisposable
	{
		public UniTask InitializeAsync(CancellationToken cancellationToken);

		public void SendEvent(in AnalyticsEventArgs args);
		public bool IsValid(in AnalyticsEventArgs args, out string error);
	}
}
