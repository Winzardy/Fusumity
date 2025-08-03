using System;
using System.Threading;
using Analytics;
using Content;
using Cysharp.Threading.Tasks;
using Fusumity.Reactive;
using Fusumity.Utility;
using Sirenix.OdinInspector;

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

		public override async UniTask RunAsync(CancellationToken token = default)
		{
			var settings = ContentManager.Get<AnalyticsSettings>();
			var router = new AnalyticsManagement(settings);

			if (@await)
				await router.InitializeAsync(token);
			else
				router.InitializeAsync(token).Forget();

			AnalyticsCenter.Initialize(router);
		}

		protected override void OnDispose()
		{
			AnalyticsCenter.Terminate();
		}

		public override void OnBootCompleted()
		{
			foreach (var type in ReflectionUtility.GetAllTypes<AnalyticsAggregator>(false))
			{
				if (!AnalyticsCenter.TryRegister(type, out var aggregator))
					continue;

				AddDisposable(aggregator);
				aggregator.Initialize();
			}
		}
	}
}
