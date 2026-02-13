using System;
using Sapientia.Extensions;
using UnityEngine;

namespace Audio
{
	public static partial class AudioEventUtility
	{
		public static AudioPlayback Play(this in AudioEventRequest request, bool disableSpatialWarning = true)
		{
			if (request.id.IsNullOrEmpty())
				return default;

			return ToPlayArgs(request, disableSpatialWarning).Play();
		}

		public static AudioPlayback Play(this in AudioEventRequest request, Transform transform,
			AudioPlaybackPositionMode mode = AudioPlaybackPositionMode.FollowTarget)
		{
			var args = request.ToPlayArgs(false);
			switch (mode)
			{
				case AudioPlaybackPositionMode.UseLastPosition:
					args.position = transform.position;
					break;
				default:
					args.transform = transform;
					break;
			}

			return args.Play();
		}

		public static AudioPlayback Play(this in AudioEventRequest request, Vector3? position)
		{
			var args = request.ToPlayArgs(false);
			args.position = position;
			return args.Play();
		}

		public static void PreloadSafe(this in AudioEventRequest request)
		{
			if (request.IsEmpty)
				return;

			request.Preload();
		}

		public static void Preload(this in AudioEventRequest request) => AudioManager.Preload(request.id);

		public static void ReleaseSafe(this in AudioEventRequest request)
		{
			if (request.IsEmpty)
				return;

			request.Release();
		}

		public static void Release(this in AudioEventRequest request) => AudioManager.Release(request.id);

		private static AudioEventDefinition ToPlayArgs(this in AudioEventRequest request, bool disableSpatialWarning)
		{
			return new AudioEventDefinition(request.id)
			{
				repeat = request.loop ? 0 : request.repeat > 0 ? request.repeat : 1,

				fadeIn = request.fadeIn.enable ? request.fadeIn.value : null,
				fadeOut = request.fadeOut.enable ? request.fadeOut.value : null,

				disableSpatialWarning = disableSpatialWarning
			};
		}
	}
}
