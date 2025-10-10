using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using Cysharp.Threading.Tasks;
using Fusumity.Utility;
using Sapientia.Reflection;
using Sirenix.OdinInspector;
using UnityEngine;

#if UNITY_EDITOR
using Sapientia.Pooling;
using Sirenix.Utilities.Editor;
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

		private List<IBootTask> _bootedTasks;

		private void Start()
		{
			RunTasksAsync(destroyCancellationToken)
			   .Forget();
		}

		private void OnDestroy()
		{
			DisposeTask();
		}

		private async UniTaskVoid RunTasksAsync(CancellationToken token)
		{
			_bootedTasks = new();

			var loadingTime = 0f;
			string passedTimeStr;
			foreach (var task in tasks)
			{
				if (!task.Active)
					continue;

				var sinceStartup = Time.realtimeSinceStartup;

				await task.RunAsync(token);

				var passedTime = Time.realtimeSinceStartup - sinceStartup;
				passedTimeStr = passedTime
				   .ToString(CultureInfo.InvariantCulture)
				   .BoldText();

				var taskName = task.GetType().Name
				   .UnderlineText();

				loadingTime += passedTime;

				Log($"Launched the task [ {taskName} ] in {passedTimeStr} seconds");

				_bootedTasks.Add(task);
				token.ThrowIfCancellationRequested();
			}

			passedTimeStr = loadingTime
			   .ToString(CultureInfo.InvariantCulture)
			   .BoldText();

			Log($"Completed in {passedTimeStr} seconds");

			foreach (var task in _bootedTasks)
				task.OnBootCompleted();
		}

		private void DisposeTask()
		{
			if (_bootedTasks == null)
				return;

			for (var i = _bootedTasks.Count; i-- > 0;)
				_bootedTasks[i].Dispose();

			_bootedTasks = null;
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
	}
}
