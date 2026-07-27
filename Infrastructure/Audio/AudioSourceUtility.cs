using System;
using DG.Tweening;
using UnityEngine;

namespace Audio
{
	public static class AudioSourceUtility
	{
		public static Tween Play(this AudioSource source, AudioTrackScheme track, AudioEventDefinition definition) =>
			Play(source, track, definition.fadeIn, definition.volume, definition.pitch);

		/// <summary>
		/// Проигрывает клип, назначенный на источник владельцем: плеер вешает клип на source до вызова
		/// </summary>
		public static Tween Play(this AudioSource source, AudioTrackScheme track,
			float? fade = null,
			float? volume = null,
			float? pitch = null,
			bool editor = false)
		{
			Tween tween = null;

#if UNITY_EDITOR
			if (editor)
			{
				if (UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode)
					return null;

				// Редакторное превью: клип берётся из editorAsset референса
				source.clip = track.clip;
			}
#endif

			if (!editor && !source.clip)
				throw new Exception("Clip is null");

			if (fade.HasValue)
			{
				source.volume = 0;
				tween = source.DOFade(GetVolume(), fade.Value)
					.SetDelay(track.delay);
			}
			else
			{
				source.volume = GetVolume();
			}

			source.pitch = GetPitch();

			//Внутри проверка на тип клипа, наружу они его не вытащили, но запрещают выставлять некорректный питч...
			//При минусовом питче нужно выставить семплы с конца
			if (Math.Abs(source.pitch - track.pitch) < float.Epsilon)
			{
				if (source.pitch < 0)
					source.timeSamples = source.clip.samples - 1;
			}

			if (track.delay > 0)
				source.PlayDelayed(track.delay);
			else
				source.Play();

			return tween;

			float GetVolume() => volume != null ? track.volume * volume.Value : track.volume;
			float GetPitch() => pitch != null ? track.pitch * pitch.Value : track.pitch;
		}
	}
}
