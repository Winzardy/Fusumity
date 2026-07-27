using System;
using Content;
using Sapientia.Extensions;
using UnityEngine;

namespace Audio
{
	public static partial class AudioEventUtility
	{
		// Задержка перед выгрузкой клипа после отпускания владения (используется AudioEventPlayer)
		internal const int DEFAULT_RELEASE_DELAY_MS = 15000;

		public static AudioPlayback Play(this AudioEventDefinition definition)
		{
			if (definition.id.IsNullOrEmpty())
				return null;

#if UNITY_EDITOR
			if (!Application.isPlaying)
			{
				ContentManager.Get<AudioEventConfig>(definition.id)
					.PlayEditor();
				return null;
			}
#endif
			return AudioManager.Play(ref definition);
		}

		public static void RollPlaylist(this ref AudioEventDefinition definition)
		{
			if (definition.config == null)
				throw new Exception("Entry is null!");

			definition.playlist = definition.config.RollPlaylist(definition.GetHashCode());
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
