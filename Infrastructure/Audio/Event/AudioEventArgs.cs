using Sapientia;
using UnityEngine;

namespace Audio
{
	public struct AudioEventArgs
	{
		/// <summary>
		/// Идентификатор аудио события
		/// </summary>
		public string id;

		public AudioEventEntry entry;

		public string mixer;

		public AudioPlayMode mode;
		public AudioTrackEntry[] playlist;

		public bool? isSpatial;

		public float? spatialBlend;
		public float? dopplerLevel;
		public int? spread;
		public Range<float>? distance;

		public Transform transform;
		public Vector3? position;

		/// <summary>
		/// Количество повторений. (0 = loop)
		/// </summary>
		public int repeat;

		/// <summary>
		/// Пересобрать плейлист при каждом повтором воспроизведении. Когда вызывается событие,
		/// он изначально роллит (собирает) плейлист треков. Если указать false, то он будет воспроизводить
		/// первоначально собранный плейлист
		/// </summary>
		public bool rerollOnRepeat;

		public float? fadeIn;
		public float? fadeOut;

		/// <summary>
		/// Глобальный звук для события (normalized: 0 -> 1). Важно что у трека может быть своя громкость и они будут множится
		/// </summary>
		public float? volume;

		/// <summary>
		/// Глобальный питч для события (-3 -> 3). Важно что у трека может быть свой питч и они будут множится
		/// </summary>
		public float? pitch;

		public AudioSourceSettings settings;

		public int? priority;
		public float? stereoPan;

		public AudioEventArgs(string id, bool loop = false) : this()
		{
			this.id = id;
			repeat = loop ? 0 : 1;
		}

		public bool disableSpatialWarning;

		public override int GetHashCode()
		{
			if (transform != null)
				return transform.GetHashCode();

			return position.HasValue ? position.GetHashCode() : id.GetHashCode();
		}
	}
}
