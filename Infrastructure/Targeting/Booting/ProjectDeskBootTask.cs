using System;
using System.Threading;
using Content;
using Cysharp.Threading.Tasks;
using Fusumity.Reactive;
using Sirenix.OdinInspector;
using Targeting;
using UnityEngine;

namespace Booting.Targeting
{
	[TypeRegistryItem(
		"\u2009Targeting", //В начале делаем отступ из-за отрисовки...
		"",
		SdfIconType.Info)]
	[Serializable]
	public class ProjectDeskBootTask : BaseBootTask
	{
		public override int Priority => HIGH_PRIORITY - 30;

		public override UniTask RunAsync(CancellationToken token = default)
		{
			var options = ContentManager.Get<ProjectInfo>();
			var platform = GetTargetPlatform();
			var provider = new DefaultProjectDeskAttendant(in options, in platform);

			ProjectDesk.Initialize(provider);
			return UniTask.CompletedTask;
		}

		protected override void OnDispose()
		{
			if (UnityLifecycle.ApplicationQuitting)
				return;

			ProjectDesk.Terminate();
		}

		private static PlatformEntry GetTargetPlatform()
		{
			var unsupportedPlatform = Application.platform.ToString();
			var postfix = unsupportedPlatform.Contains("Editor") ? string.Empty : " (unsupported)";
			var targetPlatform = $"{unsupportedPlatform}{postfix}";
			return Application.platform switch
			{
				RuntimePlatform.Android => PlatformType.ANDROID,
				RuntimePlatform.IPhonePlayer => PlatformType.IOS,
				_ => targetPlatform,
			};
		}
	}
}
