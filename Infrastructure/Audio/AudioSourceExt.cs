using System;
using DG.Tweening;
using UnityEngine;

namespace Audio
{
	public static class AudioSourceExt
	{
		public static Tween Play(this AudioSource source, AudioTrackEntry track,
			AudioEventArgs args,
			bool editor = false) =>
			Play(source, track, args.fadeIn, args.volume, args.pitch, editor);

		public static Tween Play(this AudioSource source, AudioTrackEntry track,
			float? fade = null,
			float? volume = null,
			float? pitch = null,
			bool editor = false)
		{
			Tween tween = null;

			if (!editor && !track.clip)
				throw new Exception("Clip is null");

			source.clip = editor ? track.clipReference.editorAsset : track.clip;

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
