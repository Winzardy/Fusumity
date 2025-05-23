using System;
using AssetManagement;
using Sapientia.Extensions;

namespace Audio
{
	public static class AudioEventArgsExt
	{
		private const int DEFAULT_RELEASE_DELAY_MS = 15000;

		public static AudioPlayback Play(this AudioEventArgs args)
		{
			if (args.id.IsNullOrEmpty())
				return null;

			return AudioManager.Play(ref args);
		}

		public static void RollPlaylist(this ref AudioEventArgs args)
		{
			if (args.entry == null)
				throw new Exception("Entry is null!");

			args.playlist = args.entry.RollPlaylist(args.GetHashCode());
		}

		public static void ReleasePlaylist(this ref AudioEventArgs args)
		{
			foreach (var track in args.playlist)
				track.clipReference.ReleaseSafe(DEFAULT_RELEASE_DELAY_MS);
		}

		public static void PreloadSafe(this in AudioEventArgs args)
		{
			if (args.id.IsNullOrEmpty())
				return;

			args.Preload();
		}

		public static void Preload(this in AudioEventArgs args) => AudioManager.Preload(args.id);

		public static void ReleaseSafe(this in AudioEventArgs args)
		{
			if (args.id.IsNullOrEmpty())
				return;

			args.Release();
		}

		public static void Release(this in AudioEventArgs args) => AudioManager.Release(args.id);
	}
}
