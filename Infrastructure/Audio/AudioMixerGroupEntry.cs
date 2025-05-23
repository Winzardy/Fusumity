using System;
using AssetManagement;
using Content;
using UnityEngine;

namespace Audio
{
	[Serializable]
	[Documentation("https://www.notion.so/winzardy/Audio-Mixer-Group-13f1c74f154380d5b2cec9b4e2f6128b?pvs=4")]
	public class AudioMixerGroupEntry
	{
		private const string VOLUME_EXPOSED_PARAMETER_NAME_PREFIX = "Volume_";

		[Tooltip("Используется ли в настройках, чтобы дать игроку управлять громкостью группы звуков через панель настрок")]
		public bool configurable;

		public AssetReferenceEntry<Sprite> icon;

		[Tooltip("Нужен для сортировки микшеров в окне настроек! Чем выше приоритет тем выше он в списке")]
		public int priority;

		[Tooltip("Использовать кастомный путь для соотвествия AudioMixerGroup в Master Mixer (по дефолту: <b>{id}</b>)")]
		public bool useCustomPath;

		[Tooltip("Кастомный путь для соотвествия AudioMixerGroup в Master Mixer")]
		public string customMixerPath;

		[Tooltip("Использовать кастомное название exposed parameter'a для регулировки громкости (по дефолту: <b>Volume_{id}</b>")]
		public bool useCustomVolumeExposedParameterName;

		[Tooltip("Кастомное название exposed parameter'a для регулировки громкости")]
		public string customVolumeExposedParameterName;

		[Tooltip("Путь AudioMixerGroup в Master Mixer")]
		public string GetMixerPath(string id) => useCustomPath ? customMixerPath : id;

		[Tooltip("Название <b>Exposed Parameter</b>'a для регулировки громкости")]
		public string GetVolumeExposedParameterName(string id) =>
			useCustomVolumeExposedParameterName ? customVolumeExposedParameterName : GetExposedParameterName(id);

		public static string GetExposedParameterName(string id) => VOLUME_EXPOSED_PARAMETER_NAME_PREFIX + id;
	}
}
