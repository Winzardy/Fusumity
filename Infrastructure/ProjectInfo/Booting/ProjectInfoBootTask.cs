using System;
using System.Threading;
using Content;
using Cysharp.Threading.Tasks;
using ProjectInformation;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Booting.ProjectInformation
{
	[TypeRegistryItem(
		"\u2009Project Info",
		"",
		SdfIconType.Info)]
	[Serializable]
	public class ProjectInfoBootTask : BaseBootTask
	{
		public override int Priority => HIGH_PRIORITY - 30;

		public override UniTask RunAsync(CancellationToken token = default)
		{
			var options = ContentManager.Get<ProjectInfoConfig>();
			var platform = GetTargetPlatform();
			var provider = new DefaultProjectInfoAttendant(in options, in platform);

			ProjectInfo.Set(provider);
			return UniTask.CompletedTask;
		}

		protected override void OnDispose()
		{
			ProjectInfo.Clear();
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
