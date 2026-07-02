using System.Threading;
using Cysharp.Threading.Tasks;
using Fusumity.Reactive;
using Sapientia;
using Sapientia.Extensions;

namespace Booting
{
	/// <inheritdoc cref="IBootTask"/>
	public abstract class BaseBootTask : CompositeDisposable, IBootTask
	{
		protected const int HIGH_PRIORITY = 1000;

		public virtual int Priority => 0;

		protected virtual bool ShouldSkipDispose { get => UnityLifecycle.ApplicationQuitting; }

		public abstract UniTask RunAsync(Blackboard blackboard, CancellationToken token = default);

		public virtual void OnBootCompleted()
		{
		}

		public sealed override void Dispose()
		{
			if (ShouldSkipDispose)
				return;

			base.Dispose();
		}

		public virtual bool Active => true;
	}
}
