using Cysharp.Threading.Tasks;
using Fusumity.Reactive;
using Fusumity.Utility;
using Sapientia;
using Sapientia.Extensions;
using System;
using System.Threading;
using Localization;

namespace Booting
{
	/// <inheritdoc cref="IBootTask"/>
	public abstract class BaseBootTask : CompositeDisposable, IBootTask
	{
		private const string POSTFIX = "BootTask";

		protected const int HIGH_PRIORITY = 1000;

		public LocKey _loadingLocKey;

		public virtual int Priority { get => 0; }
		public virtual bool Active { get => true; }
		public virtual bool WaitForPreviousTasks { get => false; }
		protected virtual bool ShouldSkipDispose { get => UnityLifecycle.ApplicationQuitting; }

		public virtual string Name
		{
			get => GetType().Name
				.Remove(POSTFIX)
				.NicifyText();
		}

		public async UniTask RunAsync(Blackboard blackboard, IProgress<BootProgressInfo> progress = null, CancellationToken token = default)
		{
			progress?.Report(new BootProgressInfo(_loadingLocKey, 0));
			await RunTaskAsync(blackboard, progress, token);
			progress?.Report(new BootProgressInfo(_loadingLocKey, 1));
		}

		protected abstract UniTask RunTaskAsync(Blackboard blackboard, IProgress<BootProgressInfo> progress = null, CancellationToken token = default);

		public virtual void OnBootCompleted()
		{
		}

		public sealed override void Dispose()
		{
			if (ShouldSkipDispose)
				return;

			base.Dispose();
		}

		public virtual bool IsReady() => true;

		public override string ToString() => Name;
	}
}
