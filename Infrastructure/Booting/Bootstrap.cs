using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Threading;
using Cysharp.Threading.Tasks;
using Fusumity.Utility;
using Sapientia;
using Sapientia.Collections;
using Sapientia.Extensions;
using Sapientia.Pooling;
using Sapientia.Reflection;
using Sapientia.ServiceManagement;
using Sapientia.Utility;
using Sirenix.OdinInspector;
using UnityEngine;
using WLog;

#if UNITY_EDITOR
using Sirenix.Utilities.Editor;
using Fusumity.Editor.Utility;
#endif

namespace Booting
{
	[DefaultExecutionOrder(-2000)]
	[HideMonoScript]
	public partial class Bootstrap : MonoBehaviour
	{
		[SerializeReference]
#if UNITY_EDITOR
		[ListDrawerSettings(OnTitleBarGUI = nameof(DrawAutoFillTasksButton), NumberOfItemsPerPage = 100)]
#endif
		public IBootTask[] tasks;

		public event Action<IBootTask, float> TaskBooted;

		// Максимально допустимое время без Update: если синхронные буттаски держат главный поток дольше,
		// перед запуском следующей таски ждём, пока движок "продышится" (кадр отрисуется, Update тикнет)
		private const double MAX_TIME_WITHOUT_UPDATE_SEC = 0.5d;

		private List<IBootTask> _bootedTasks;

		private CompositeProgress<BootProgressInfo> _compositeProgress;

		private double _lastUpdateTime;

		private void Start()
		{
			RunTasksAsync(destroyCancellationToken)
				.Forget();
		}

		private void Update()
		{
			_lastUpdateTime = Time.realtimeSinceStartupAsDouble;
		}

		private void OnDestroy()
		{
			DisposeTask();
		}

		private async UniTaskVoid RunTasksAsync(CancellationToken cancellationToken)
		{
			_bootedTasks = new();

			var loadingTime = Time.realtimeSinceStartupAsDouble;
			Log($"Started loading tasks at {loadingTime.ToString(CultureInfo.InvariantCulture).BoldText(true)} seconds");

			var loadingTimer = Timer.Start();
			string passedTimeStr;
			using var blackboard = new Blackboard();
			blackboard.Register(this);

			var controller = new BootstrapLoadingProgress() as ILoadingProgress;
			blackboard.Register(controller);
			controller.RegisterAsService();

			_compositeProgress = new CompositeProgress<BootProgressInfo>(GetTotalWeight(tasks), controller.Progress);

			var invalidUsageProgress = new InvalidUsageProgress();

			// Start вызывается до первого Update — считаем, что на старте главный поток свободен
			_lastUpdateTime = loadingTime;

			foreach (var task in tasks)
			{
				if (!task.Active)
					continue;

				if (task.WaitForPreviousTasks)
					await WaitForTasksReadyAsync(cancellationToken);

				await WaitForUpdateAsync(cancellationToken);

				var taskTimer = Timer.Start();

				var progress = task is IWeightedProgress weightedProgress ? _compositeProgress.CreateChild(weightedProgress.Weight) : invalidUsageProgress;

				await task.RunAsync(blackboard, progress, cancellationToken);

				var passedTime = taskTimer.Seconds;
				passedTimeStr = passedTime
					.ToString(CultureInfo.InvariantCulture)
					.BoldText(true);

				var taskName = task.Name
					.UnderlineText(true);

				TaskBooted?.Invoke(task, (float) passedTime);

				Log($"Launched the task [ {taskName} ] in {passedTimeStr} seconds"
#if UNITY_EDITOR
					, task.GetType().FindMonoScript()
#endif
				);

				_bootedTasks.Add(task);
				cancellationToken.ThrowIfCancellationRequested();
			}

			await WaitForTasksReadyAsync(cancellationToken);

			passedTimeStr = loadingTimer.Seconds
				.ToString(CultureInfo.InvariantCulture)
				.BoldText(true);

			Log($"Completed in {passedTimeStr} seconds");

			foreach (var task in _bootedTasks)
				task.OnBootCompleted();

			controller.UnRegisterAsService();
		}

		/// <summary>
		/// Если синхронные буттаски заблокировали главный поток дольше <see cref="MAX_TIME_WITHOUT_UPDATE_SEC"/>,
		/// ждём следующего Update, чтобы движок успел прокрутить кадр
		/// </summary>
		private async UniTask WaitForUpdateAsync(CancellationToken cancellationToken)
		{
			var lastUpdateTime = _lastUpdateTime;
			if (Time.realtimeSinceStartupAsDouble - lastUpdateTime < MAX_TIME_WITHOUT_UPDATE_SEC)
				return;

			var waitingTimer = Timer.Start();

			await UniTask.WaitUntil(Wait, cancellationToken: cancellationToken);

			var passedTime = waitingTimer.Seconds
				.ToString(CultureInfo.InvariantCulture)
				.BoldText();
			Log($"Main thread was busy, waited for update {passedTime} seconds");

			bool Wait() => _lastUpdateTime > lastUpdateTime;
		}

		private async UniTask WaitForTasksReadyAsync(CancellationToken cancellationToken)
		{
			using (ListPool<IBootTask>.Get(out var waitingTasks))
			{
				foreach (var task in _bootedTasks)
				{
					if (task.IsReady())
						continue;

					waitingTasks.Add(task);
				}

				if (waitingTasks.IsEmpty())
					return;

				var str = waitingTasks.GetCompositeString(false,
					task => task.Name.UnderlineText(true),
					numerate: false,
					separator: ", ");
				var waitingTimer = Timer.Start();
				Log($"Waiting for the tasks [ {str} ]");

				await UniTask.WaitUntil(AreTasksReady, cancellationToken: cancellationToken);

				var passedTime = waitingTimer.Seconds
					.ToString(CultureInfo.InvariantCulture)
					.BoldText();
				Log($"Tasks [ {str} ] became ready in {passedTime} seconds");

				bool AreTasksReady()
				{
					foreach (var task in waitingTasks)
					{
						if (!task.IsReady())
							return false;
					}

					return true;
				}
			}
		}

		private void DisposeTask()
		{
			if (_bootedTasks == null)
				return;

			for (var i = _bootedTasks.Count; i-- > 0;)
				_bootedTasks[i].Dispose();

			_bootedTasks = null;
		}

		private static float GetTotalWeight(IBootTask[] tasks)
		{
			float weight = 0;
			foreach (var task in tasks)
			{
				if (task.Active && task is IWeightedProgress weightedProgress)
				{
					weight += weightedProgress.Weight;
				}
			}
			return weight;
		}

#if UNITY_EDITOR
		private void DrawAutoFillTasksButton()
		{
			if (SirenixEditorGUI.ToolbarButton(EditorIcons.Refresh))
				AutoFillTasks();
		}

		[ContextMenu("Auto Fill Tasks")]
		private void AutoFillTasks()
		{
			var types = ReflectionUtility.GetAllTypes<IBootTask>();
			using (ListPool<IBootTask>.Get(out var list))
			{
				for (int i = 0; i < types.Count; i++)
				{
					var task = types[i].CreateInstance<IBootTask>();
					list.Add(task);
				}

				list.Sort(SortByPriority);

				tasks = list.ToArray();
			}

			int SortByPriority(IBootTask a, IBootTask b)
				=> b.Priority.CompareTo(a.Priority);
		}
#endif

		private class InvalidUsageProgress : IProgress<BootProgressInfo>
		{
			public void Report(BootProgressInfo value)
			{
#if UNITY_EDITOR || DEV
				if (value.Progress != 0 && !Mathf.Approximately(value.Progress, 1))
				{
					// Все буттаски из бутстрапа, которые репортят прогресс отличный от дефолтных (0 и 1) должны имплементировать интерфейс IWeightedProgress,
					// чтобы можно было корректно отображать прогресс
					this.LogError($"{nameof(IBootTask)} report progress, but not implement {nameof(IWeightedProgress)}");
				}
#endif
			}
		}
	}

	public interface ILoadingProgress
	{
		BootProgressInfo CurrentProgress { get; }
		Progress<BootProgressInfo> Progress { get; }
	}

	public class BootstrapLoadingProgress : ILoadingProgress
	{
		public BootProgressInfo CurrentProgress { get; private set; }

		public Progress<BootProgressInfo> Progress { get; }

		public BootstrapLoadingProgress()
		{
			Progress = new(newProgress =>
			{
				CurrentProgress = new(newProgress.locKey.value ?? CurrentProgress.locKey.value, newProgress.Progress);
			});
		}
	}

	public readonly struct Timer
	{
		// Секунд в одном тике счётчика Stopwatch
		private static readonly double SECONDS_PER_TICK = 1d / Stopwatch.Frequency;

		private readonly long _startTimestamp;

		/// <summary>
		/// Прошедшее время в секундах с момента запуска
		/// </summary>
		public double Seconds
		{
			get => (Stopwatch.GetTimestamp() - _startTimestamp) * SECONDS_PER_TICK;
		}

		private Timer(long startTimestamp)
		{
			_startTimestamp = startTimestamp;
		}

		/// <summary>
		/// Запускает новый таймер от текущего момента
		/// </summary>
		public static Timer Start() => new(Stopwatch.GetTimestamp());
	}
}
