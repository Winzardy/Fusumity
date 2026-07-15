using Cysharp.Threading.Tasks;
using Fusumity.Reactive;
using Fusumity.Utility;
using Sapientia;
using Sapientia.Extensions;
using System;
using System.Threading;

namespace Booting
{
	/// <inheritdoc cref="IBootTask"/>
	public abstract class BaseBootTask : CompositeDisposable, IBootTask
	{
		private const string POSTFIX = "BootTask";

		protected const int HIGH_PRIORITY = 1000;

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

		public virtual bool IsReady() => true;

		public override string ToString() => Name;
	}

	public abstract class ProgressiveBootTask : BaseBootTask, IProgressNotifier
	{
		public float Progress { get; protected set; }
		public event Action<float> ProgressChanged;

		public sealed override async UniTask RunAsync(Blackboard blackboard, CancellationToken token = default)
		{
			ReportProgress(0);
			await RunTaskAsync(blackboard, token);
			ReportProgress(1);
		}

		protected abstract UniTask RunTaskAsync(Blackboard blackboard, CancellationToken token);

		protected void ReportProgress(float progress)
		{
			Progress = progress;
			ProgressChanged?.Invoke(Progress);
		}
	}
}
