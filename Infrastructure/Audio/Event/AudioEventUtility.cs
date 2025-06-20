using System;
using AssetManagement;
using Sapientia.Extensions;

namespace Audio
{
	public static partial class AudioEventUtility
	{
		private const int DEFAULT_RELEASE_DELAY_MS = 15000;

		public static AudioPlayback Play(this AudioEventDefinition definition)
		{
			if (definition.id.IsNullOrEmpty())
				return null;

			return AudioManager.Play(ref definition);
		}

		public static void RollPlaylist(this ref AudioEventDefinition definition)
		{
			if (definition.entry == null)
				throw new Exception("Entry is null!");

			definition.playlist = definition.entry.RollPlaylist(definition.GetHashCode());
		}

		public static void ReleasePlaylist(this ref AudioEventDefinition definition)
		{
			foreach (var track in definition.playlist)
				track.clipReference.ReleaseSafe(DEFAULT_RELEASE_DELAY_MS);
		}

		public static void PreloadSafe(this in AudioEventDefinition definition)
		{
			if (definition.id.IsNullOrEmpty())
				return;

			definition.Preload();
		}

		public static void Preload(this in AudioEventDefinition definition) => AudioManager.Preload(definition.id);

		public static void ReleaseSafe(this in AudioEventDefinition definition)
		{
			if (definition.id.IsNullOrEmpty())
				return;

			definition.Release();
		}

		public static void Release(this in AudioEventDefinition definition) => AudioManager.Release(definition.id);
	}
}
