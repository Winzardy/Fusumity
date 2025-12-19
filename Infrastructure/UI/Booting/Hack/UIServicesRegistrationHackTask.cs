using Cysharp.Threading.Tasks;
using Game.App.ServiceManagement;
using Sapientia.ServiceManagement;
using System.Threading;
using UI.Screens;
using UI.Windows;

namespace UI
{
	// This is an absolute bullshit hack to be able to resolve UI dependencies as usual, in a consistent manner,
	// without having to use UIDispatcher.

	// Best scenario though, imo - is to eliminate UIBootTask, move everything to an initializer, and get rid of UIDispatcher altogether.
	// For now however - it is an ugly solution to a silly problem.
	public class UIServicesRegistrationHackTask : IServiceTask
	{
		public UniTask RunAsync(CancellationToken token)
		{
			var popups = UIDispatcher.Get<UIPopupDispatcher>();
			var windows = UIDispatcher.Get<UIWindowDispatcher>();
			var screens = UIDispatcher.Get<UIScreenDispatcher>();
			var popovers = UIDispatcher.Get<UIPopoverDispatcher>();

			var provider = ServiceLocator.Get<ServicesProvider>();

			provider.AddInstance(ContextLifetime.Permanent, popups);
			provider.AddInstance(ContextLifetime.Permanent, windows);
			provider.AddInstance(ContextLifetime.Permanent, screens);
			provider.AddInstance(ContextLifetime.Permanent, popovers);

			return UniTask.CompletedTask;
		}
	}
}
