using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Fusumity.Reactive;
using Messaging;
using Sapientia;
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

		protected override UniTask RunTaskAsync(Blackboard _, IProgress<BootProgressInfo> progress = null, CancellationToken token = default)
		{
			var bus = new MessageBus();
			Messenger.Set(bus);

			return UniTask.CompletedTask;
		}

		protected override void OnDispose()
		{
			Messenger.Clear();
		}
	}
}
