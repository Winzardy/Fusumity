using System;
using System.Threading;
using Analytics;
using Content;
using Cysharp.Threading.Tasks;
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

		public override async UniTask RunAsync(CancellationToken token = default)
		{
			var settings = ContentManager.Get<AnalyticsSettings>();
			var isValidationEnabled = Application.isEditor || Debug.isDebugBuild;
			var management = new AnalyticsManagement(settings, isValidationEnabled);

			if (@await)
				await management.InitializeAsync(token);
			else
				management.InitializeAsync(token)
					.Forget(Bootstrap.LogException);

			AnalyticsCenter.Set(management);
		}

		protected override void OnDispose()
		{
			AnalyticsCenter.Clear();
		}
	}
}
