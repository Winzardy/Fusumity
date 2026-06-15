using System;
using System.Threading;
using Analytics;
using Content;
using Cysharp.Threading.Tasks;
using Sapientia;
using Sapientia.Pooling;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Booting.Analytics
{
	[TypeRegistryItem(
		"\u2009Analytics", //В начале делаем отступ из-за отрисовки...
		"",
		SdfIconType.GraphUp)]
	[Serializable]
	public class AnalyticsBootTask : BaseBootTask
	{
		public override int Priority => HIGH_PRIORITY - 120;

		public bool @await;

		private BootTaskAnalyticsAggregator _aggregator;

		public override async UniTask RunAsync(Blackboard blackboard, CancellationToken token = default)
		{
			var isValidationEnabled = Application.isEditor || Debug.isDebugBuild;
			var settings = ContentManager.Get<AnalyticsSettings>();
			var management = new AnalyticsManagement(settings, isValidationEnabled);

			if (@await)
				await management.InitializeAsync(token);
			else
				management.InitializeAsync(token)
					.Forget(Bootstrap.LogException);

			AnalyticsCenter.Set(management);
			_aggregator = new BootTaskAnalyticsAggregator(blackboard.Get<Bootstrap>());
		}

		public override void OnBootCompleted()
		{
			_aggregator.Dispose();
			_aggregator = null;
		}

		protected override void OnDispose()
		{
			AnalyticsCenter.Clear();
			_aggregator?.Dispose();
		}
	}

	public class BootTaskAnalyticsAggregator : AnalyticsAggregator
	{
		private readonly Bootstrap _bootstrap;

		public BootTaskAnalyticsAggregator(Bootstrap bootstrap)
		{
			_bootstrap = bootstrap;

			_bootstrap.TaskBooted += OnTaskBooted;
		}

		protected override void OnDisposeInternal()
		{
			base.OnDisposeInternal();

			_bootstrap.TaskBooted -= OnTaskBooted;
		}

		private void OnTaskBooted(IBootTask task, float time)
		{
			using (DictionaryPool<string, object>.Get(out var parameters))
			{
				parameters["name"] = task.GetType().Name;
				parameters["Duration"] = time;
				parameters["Time"] = Time.realtimeSinceStartup;

				Send("BootTask", parameters);
			}
		}
	}
}
