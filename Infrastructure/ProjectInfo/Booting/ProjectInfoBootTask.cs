using System;
using System.Collections.Generic;
using System.Threading;
using Content;
using Cysharp.Threading.Tasks;
using ProjectInformation;
using Sapientia;
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

		public override UniTask RunAsync(Blackboard _, CancellationToken token = default)
		{
			var options = ContentManager.Get<ProjectInfoConfig>();
			var platform = GetTargetPlatform();
			var provider = new DefaultProjectInfoAttendant(in options, in platform, GetBuildInfo());

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
				RuntimePlatform.WindowsPlayer => PlatformType.WINDOWS_DEBUG,
				_ => targetPlatform,
			};
		}

		private static BuildInfo GetBuildInfo()
		{
#if UNITY_EDITOR
			return BuildInfo.CreateFromGit(GitUtility.GetProjectRoot());
#else
			return GetBuildInfoFromResources();
#endif
		}

		private static BuildInfo GetBuildInfoFromResources()
		{
			var textAsset = Resources.Load<TextAsset>(nameof(BuildInfo));
			if (textAsset == null)
			{
				return CreateUnknownBuildInfo();
			}

			try
			{
				return JsonUtility.FromJson<BuildInfo>(textAsset.text);
			}
			catch
			{
				return CreateUnknownBuildInfo();
			}

			BuildInfo CreateUnknownBuildInfo()
			{
				return new BuildInfo() { branch = "unknown", commit = "unknown", submodules = new Dictionary<string, string>()};
			}
		}
	}
}
