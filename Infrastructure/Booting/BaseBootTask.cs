using System.Threading;
using Cysharp.Threading.Tasks;
using Fusumity.Reactive;
using Sapientia.Extensions;

namespace Booting
{
	/// <inheritdoc cref="IBootTask"/>
	public abstract class BaseBootTask : CompositeDisposable, IBootTask
	{
		protected const int HIGH_PRIORITY = 1000;

		public virtual int Priority => 0;

		public abstract UniTask RunAsync(CancellationToken token = default);

		public virtual void OnBootCompleted()
		{
		}

		public sealed override void Dispose()
		{
			if (UnityLifecycle.ApplicationQuitting)
				return;

			base.Dispose();
			OnDispose();
		}

		protected virtual void OnDispose()
		{
		}

		public virtual bool Active => true;
	}
}
