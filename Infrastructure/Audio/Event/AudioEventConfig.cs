using System;
using AssetManagement;
using Content;
using Sapientia;
using UnityEngine;

namespace Audio
{
	public enum AudioPlayMode
	{
		SameTime,
		Sequence
	}

	public enum SequenceType
	{
		ByOrder,
		Shuffle,
	}

	public enum SelectionMode
	{
		None,

		Random,
		ByOrder,
		ByLocalOrder
	}

	[Serializable]
	[Documentation("https://www.notion.so/winzardy/13f1c74f154380c19420c23fdd0bc3c4?pvs=4")]
	[Constants(filterOut: new[] {"Migrated"})]
	public partial class AudioEventConfig : IdentifiableConfig
	{
		public const float DEFAULT_STEREO_PAN = 0;
		public const float MIN_STEREO_PAN = -1;
		public const float MAX_STEREO_PAN = 1;

		public const int DEFAULT_PRIORITY = 128;
		public const int MIN_PRIORITY = 0;
		public const int MAX_PRIORITY = 256;

		public string mixer;

		[Tooltip("<b>" + nameof(AudioPlayMode.SameTime) + "</b> - проиграть все сразу<br>" +
			"<b>" + nameof(AudioPlayMode.Sequence) + "</b> - проиграть по очереди")]
		public AudioPlayMode playMode;

		[Tooltip("<b>" + nameof(SequenceType.ByOrder) + "</b> - проиграть по порядку<br>" +
			"<b>" + nameof(SequenceType.Shuffle) + "</b> - проиграть предварительно перемешав")]
		public SequenceType sequenceType;

		[Tooltip("Выборка треков для проигрывния.<br>" +
			"<b>" + nameof(SelectionMode.Random) + "</b> - выбирает рандомно трек из списка (без повторений)<br>" +
			"<b>" + nameof(SelectionMode.ByOrder) + "</b> - проигрывает по порядку (глобально)<br>" +
			"<b>" + nameof(SelectionMode.ByLocalOrder) +
			"</b> - проигрывает по порядку относительно <u>position</u> или <u>transform</u> (приоритетный ориентир)," +
			" если нет ни position ни transfrom, то работает как <u>" + nameof(SelectionMode.ByOrder) + "</u>, то есть глобально")]
		public SelectionMode selection;

		[Tooltip("Диапозон выборки, например выбрать 1 из списка")]
		public int selectionRange = 1;

		public AudioTrackScheme[] tracks;

		[Tooltip("Будет ли <b>Pitch</b> зависить от Time Scale (грубо говоря меняет скорость воспроизведения клипа)")]
		public bool timeScaledPitch;

		[Tooltip("Определяет приоритет звука среди всех, которые сосуществуют в сцене" +
			". (Приоритет: 0 = самый важный. 256 = наименее важный. По умолчанию = 128.)" +
			". Используйте 0 для музыкальных дорожек, чтобы избежать их случайной замены")]
		public int priority = DEFAULT_PRIORITY;

		[Tooltip("Устанавливает положение в стереополе 2D-звуков")]
		public float stereoPan = DEFAULT_STEREO_PAN;

		[Tooltip(
			"Зависит ли звук от положения в пространстве. Игнорируется если не будет назначен 'источник' или позиция звука при вызове события")]
		public bool isSpatial;

		public AudioSpatialScheme spatial;
	}

	[Serializable]
	public class AudioSpatialScheme
	{
		#region Constants

		public const int DEFAULT_AUDIO_SPATIAL_DISTANCE_MIN = 10;
		public const int DEFAULT_AUDIO_SPATIAL_DISTANCE_MAX = 100;

		public const float DEFAULT_SPATIAL_BLEND = 1;

		public const float DEFAULT_DOPPLER_LEVEL = 1;
		public const float DOPPLER_LEVEL_MIN = 0;
		public const float DOPPLER_LEVEL_MAX = 5;

		public const int DEFAULT_SPREAD = 50;
		public const int SPREAD_MIN = 0;
		public const int SPREAD_MAX = 360;

		#endregion

		[Tooltip("Влияние положения в пространстве на громокость звука (normalized: 0 -> 1)")]
		public float spatialBlend = DEFAULT_SPATIAL_BLEND;

		public float dopplerLevel = DEFAULT_DOPPLER_LEVEL;
		public int spread = DEFAULT_SPREAD;

		public AudioRolloffMode rolloffMode = AudioRolloffMode.Logarithmic;
		public AnimationCurve customRolloffCurve = AnimationCurve.EaseInOut(0, 1, 1, 0);

		[Tooltip("Расстояние от AudioListener на котором звук 'слышен' (максимально, минимально) в метрах." +
			" Первое значение это расстояние до которого громкость не будет падать")]
		public Range<float> distance = new(DEFAULT_AUDIO_SPATIAL_DISTANCE_MIN,
			DEFAULT_AUDIO_SPATIAL_DISTANCE_MAX);
	}

	[Serializable]
	public partial class AudioTrackScheme : IWeightable
	{
		public const float DEFAULT_VOLUME = 1;

		public const float DEFAULT_PITCH = 1;
		public const float MIN_PITCH = -3;
		public const float MAX_PITCH = 3;

		public AssetReferenceEntry<AudioClip> clipReference;

		[Tooltip("Собственная громкость воспроизведения, так же громкость будет зависить от <b>Mixer</b>'a к которому звук привязан")]
		public float volume = DEFAULT_VOLUME;

		[Tooltip("В простонародье 'скорость воспроизведения' звука (но есть нюансы)")]
		public float pitch = DEFAULT_PITCH;

		[Tooltip("Задержка перед воспроизведением")]
		public float delay = 0;

		[Tooltip("Участвуте в рандоме. Нулевой вес не участвует")]
		public int weight = 1;

		int IWeightable.Weight => weight;

		#region Runtime

		[NonSerialized]
		private AudioClip _clip;

		public AudioClip clip
		{
			get
			{
#if UNITY_EDITOR
				return Application.isPlaying ? _clip : clipReference.editorAsset;
#endif
				return _clip;
			}
			set
			{
#if UNITY_EDITOR
				if (!Application.isPlaying)
					return;
#endif
				_clip = value;
			}
		}

		#endregion
	}
}
