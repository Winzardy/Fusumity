using Sapientia.Extensions;
using UnityEngine;

namespace Audio
{
	public static class AudioEventTriggerArgsExt
	{
		public static AudioPlayback Play(this in AudioEventTriggerArgs triggerArgs, bool disableSpatialWarning = true)
		{
			if (triggerArgs.id.IsNullOrEmpty())
				return default;

			return ToPlayArgs(triggerArgs, disableSpatialWarning).Play();
		}

		public static AudioPlayback Play(this in AudioEventTriggerArgs triggerArgs, Transform transform)
		{
			var args = triggerArgs.ToPlayArgs(false);
			args.transform = transform;
			return args.Play();
		}

		public static AudioPlayback Play(this in AudioEventTriggerArgs triggerArgs, Vector3? position)
		{
			var args = triggerArgs.ToPlayArgs(false);
			args.position = position;
			return args.Play();
		}

		public static void PreloadSafe(this in AudioEventTriggerArgs triggerArgs)
		{
			if (triggerArgs.IsEmpty)
				return;

			triggerArgs.Preload();
		}

		public static void Preload(this in AudioEventTriggerArgs triggerArgs) => AudioManager.Preload(triggerArgs.id);

		public static void ReleaseSafe(this in AudioEventTriggerArgs triggerArgs)
		{
			if (triggerArgs.IsEmpty)
				return;

			triggerArgs.Release();
		}

		public static void Release(this in AudioEventTriggerArgs triggerArgs) => AudioManager.Release(triggerArgs.id);

		private static AudioEventArgs ToPlayArgs(this in AudioEventTriggerArgs triggerArgs, bool disableSpatialWarning)
		{
			return new AudioEventArgs(triggerArgs.id)
			{
				repeat = triggerArgs.loop ? 0 : triggerArgs.repeat > 0 ? triggerArgs.repeat : 1,

				fadeIn = triggerArgs.fadeIn.enable ? triggerArgs.fadeIn.value : null,
				fadeOut = triggerArgs.fadeOut.enable ? triggerArgs.fadeOut.value : null,

				disableSpatialWarning = disableSpatialWarning
			};
		}
	}
}
