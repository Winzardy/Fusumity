using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Fusumity.Reactive;
using Messaging;
using Sirenix.OdinInspector;

namespace Booting.Messaging
{
	[TypeRegistryItem(
		"\u2009Messaging", //В начале делаем отступ из-за отрисовки...
		"",
		SdfIconType.EnvelopeFill)]
	[Serializable]
	public class MessagingBootTask : BaseBootTask
	{
		public override int Priority => HIGH_PRIORITY;

		public override UniTask RunAsync(CancellationToken token = default)
		{
			var bus = new MessageBus();
			Messenger.Initialize(bus);

			return UniTask.CompletedTask;
		}

		protected override void OnDispose()
		{
			if (UnityLifecycle.ApplicationQuitting)
				return;

			Messenger.Terminate();
		}
	}
}
