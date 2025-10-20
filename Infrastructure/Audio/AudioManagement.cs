using System;
using System.Collections.Generic;
using System.Threading;
using AssetManagement;
using Audio.Player;
using Content;
using Cysharp.Threading.Tasks;
using Fusumity.Utility;
using Sapientia.Collections;
using Sapientia.Extensions;
using Sapientia.Utility;
using Sapientia.Pooling;
using Sapientia.Reflection;
using UnityEngine;
using UnityEngine.Audio;

namespace Audio
{
	public class AudioManagement : IDisposable
	{
		//TODO: PlayerProfile
		private const string MIXER_VOLUME_SAVE_KEY_FORMAT = "Mixer/{0}/Volume";
		private const string MIXER_MUTE_VOLUME_SAVE_KEY_FORMAT = "Mixer/{0}/Mute";

		private const float MIN_VOLUME_VALUE = 0.0001f; // LOG10(0.0001) = -80
		private const float MIN_VOLUME_VALUE_DB = -80f;

		private readonly IAudioListenerLocator _listenerLocator;
		private readonly IAudioEngineEvents _engine;

		private readonly AudioSettings _settings;

		private List<IAudioPlayer> _audioPlayers;

		private readonly AudioEventPlayerPool _pool;

		private CancellationTokenSource _cts;

		private AudioMixer _master;

		private (string id, AudioMixerGroupEntry entry) _masterMixer;

		private Dictionary<string, AudioMixerGroupContainer> _idToMixerGroup = new();

		public AudioManagement(AudioSettings settings,
			AudioFactory factory,
			IAudioListenerLocator listenerLocator,
			IAudioEngineEvents engine)
		{
			_settings = settings;

			_listenerLocator = listenerLocator;
			_engine = engine;

			_pool = new AudioEventPlayerPool(factory);

			_cts = new();
		}

		public void Dispose()
		{
			_pool.Dispose();

			_idToMixerGroup = null;

			AsyncUtility.Trigger(ref _cts);

			foreach (var player in _audioPlayers)
			{
				if (player is IDisposable disposable)
					disposable.Dispose();
			}

			_audioPlayers?.ReleaseToStaticPool();

			if (!_master)
				return;

			_settings.masterMixer.reference.Release();
		}

		public async UniTask InitializeAsync(CancellationToken cancellationToken = default)
		{
			//Нужно подгрузить основной миксер и через Addressable он не работает, пришлось пойти через Resource
			_master = await _settings.masterMixer.reference.LoadAsync(cancellationToken);

			_masterMixer = (_master.name, _settings.masterMixer.entry);

			SetVolume(_masterMixer.id, LoadMixerVolume(_masterMixer.id));

			foreach (var (id, _) in ContentManager.GetAllEntries<AudioMixerGroupEntry>())
				SetVolume(id, LoadMixerVolume(id));
		}

		internal AudioPlayback Play(ref AudioEventDefinition definition)
		{
			if (definition.id.IsNullOrEmpty())
				throw new ArgumentException("Id can't be null or empty", nameof(definition.id));

			if (!TryPrepareArgs(ref definition, out var code))
			{
				AudioDebug.LogError($"Error on prepare audio event definition by id [ {definition.id} ] (code: {code})");
				return null;
			}

			if (!IsAudibleBySpatial(in definition))
				return null;

			var isSpatial = definition.isSpatial ?? false;
			var playback = isSpatial && definition.transform
				? new AudioPlayback(_pool, definition.transform)
				: new AudioPlayback(_pool, definition.position ?? Vector3.zero);
			playback.Setup(in definition, true);
			return playback;
		}

		private bool TryPrepareArgs(ref AudioEventDefinition definition, out PlayErrorCode errorCode)
		{
			errorCode = PlayErrorCode.None;

			if (definition.playlist.IsNullOrEmpty())
			{
				if (definition.config == null)
				{
					if (!ContentManager.Contains<AudioEventConfig>(definition.id))
					{
						errorCode = PlayErrorCode.NotEventEntry;
						return false;
					}

					definition.config = ContentManager.Get<AudioEventConfig>(definition.id);
				}

				definition.RollPlaylist();
			}

			if (definition.playlist.IsNullOrEmpty())
			{
				errorCode = PlayErrorCode.EmptyPlaylist;
				return false;
			}

			if (definition.mixer.IsNullOrEmpty())
				definition.mixer = definition.config != null
					? definition.config.mixer.IsNullOrEmpty() ? _masterMixer.id : definition.config.mixer
					: _masterMixer.id;

			if (!TryGetAudioMixerGroup(definition.mixer, out var mixerGroup))
			{
				errorCode = PlayErrorCode.NotFoundMixerGroup;
				AudioDebug.LogError($"Not found audio mixer group by id [ {definition.mixer} ]");
				return false;
			}

			var rolloffMode = definition.config?.spatial.rolloffMode ?? AudioRolloffMode.Linear;
			var customRolloffCurve = rolloffMode == AudioRolloffMode.Custom ? definition.config?.spatial.customRolloffCurve : null;

			var audioSourceSettings = new AudioSourceSettings(
				mixerGroup,
				rolloffMode: rolloffMode,
				customRolloffCurve: customRolloffCurve,
				timeScaledPitch: definition.config?.timeScaledPitch ?? false);

			if (!definition.isSpatial.HasValue)
			{
				if (definition.config != null)
					definition.isSpatial = definition.config.isSpatial;
			}

			if (definition.transform || definition.position.HasValue)
			{
				if (definition.isSpatial.HasValue && definition.isSpatial.Value)
				{
					audioSourceSettings.priority = definition.priority ?? (definition.config?.priority ?? AudioEventConfig.DEFAULT_PRIORITY);
					audioSourceSettings.spatialBlend =
						definition.spatialBlend ?? (definition.config?.spatial.spatialBlend ?? AudioSpatialScheme.DEFAULT_SPATIAL_BLEND);
					audioSourceSettings.dopplerLevel =
						definition.dopplerLevel ?? (definition.config?.spatial.dopplerLevel ?? AudioSpatialScheme.DEFAULT_DOPPLER_LEVEL);
					audioSourceSettings.spread = definition.spread ?? (definition.config?.spatial.spread ?? AudioSpatialScheme.DEFAULT_SPREAD);
					audioSourceSettings.minDistance =
						definition.distance?.min ?? (definition.config?.spatial.distance.min ?? AudioSpatialScheme.DEFAULT_AUDIO_SPATIAL_DISTANCE_MIN);
					audioSourceSettings.maxDistance =
						definition.distance?.max ?? (definition.config?.spatial.distance.max ?? AudioSpatialScheme.DEFAULT_AUDIO_SPATIAL_DISTANCE_MAX);
					audioSourceSettings.stereoPan = definition.stereoPan ?? (definition.config?.stereoPan ?? AudioEventConfig.DEFAULT_STEREO_PAN);
				}
				else
				{
					audioSourceSettings.spatialBlend = 0;
				}
			}
			else
			{
				if (definition is {disableSpatialWarning: false, isSpatial: not null})
					AudioDebug.LogWarning("Audio source without position or transform is can't be spatial.");

				audioSourceSettings.spatialBlend = 0;
			}

			definition.settings = audioSourceSettings;
			definition.mode = definition.config?.playMode ?? AudioPlayMode.SameTime;
			return true;
		}

		private bool IsAudibleBySpatial(in AudioEventDefinition definition)
		{
			if (!definition.isSpatial.HasValue)
				return true;

			if (!definition.isSpatial.Value)
				return true;

			if (definition.repeat != 1)
				return true;

			var listener = GetListener();

			if (listener == null)
				return false;

			var sourcePosition = definition.transform ? definition.transform.position : definition.position ?? Vector3.zero;

			return Vector3.SqrMagnitude(sourcePosition - listener.transform.position) <
				definition.settings.maxDistance * definition.settings.maxDistance;
		}

		internal AudioListener GetListener() => _listenerLocator.Get();

		/// <returns>Normalized value (from 0 to 1)</returns>
		internal float GetVolume(string mixerId)
		{
			if (!TryGetAudioMixerGroup(mixerId, out _))
				return 0;

			var entry = GetMixerEntry(mixerId);
			return GetVolume(mixerId, entry);
		}

		/// <returns>Normalized value (from 0 to 1)</returns>
		private float GetVolume(string mixerId, AudioMixerGroupEntry entry)
		{
			var parameterName = entry.GetVolumeExposedParameterName(mixerId);
			if (_master.GetFloat(parameterName, out var volume))
			{
				var value = volume / 20;
				var normalizedValue = Mathf.Pow(10, value);

				return normalizedValue;
			}

			AudioDebug.LogError($"Not found parameter [ {parameterName} ] by mixer group by id [ {mixerId} ]");
			return 0;
		}

		internal void SetVolume(string mixerId, float normalizedValue, bool save = true)
		{
			var entry = GetMixerEntry(mixerId);
			SetVolume(mixerId, entry, normalizedValue, save);
		}

		internal void Preload(string eventId)
		{
			var entry = ContentManager.Get<AudioEventConfig>(eventId);
			foreach (var track in entry.tracks)
				track.clipReference.Preload();
		}

		internal void Release(string eventId)
		{
			var entry = ContentManager.Get<AudioEventConfig>(eventId);
			foreach (var track in entry.tracks)
				track.clipReference.Release();
		}

		private void SetVolume(string mixerId, AudioMixerGroupEntry entry, float normalizedValue, bool save = true)
		{
			var clampedValue = Mathf.Clamp(normalizedValue, MIN_VOLUME_VALUE, 1);
			var parameterName = entry.GetVolumeExposedParameterName(mixerId);
			var db = IsMute(mixerId) ? MIN_VOLUME_VALUE_DB : Mathf.Log10(clampedValue) * 20;

			if (!_master.SetFloat(parameterName, db))
				AudioDebug.LogError($"Not found parameter [ {parameterName} ] by mixer group by id [ {mixerId} ]");

			if (save)
				SaveMixerVolume(mixerId, clampedValue);
		}

		private bool TryGetAudioMixerGroup(string mixerId, out AudioMixerGroupContainer container)
		{
			if (_idToMixerGroup.TryGetValue(mixerId, out container))
				return container;

			container = FindMixerGroup(mixerId);
			container.mute = LoadMute(mixerId);

			_idToMixerGroup[mixerId] = container;
			return container;
		}

		private AudioMixerGroup FindMixerGroup(string mixerId)
		{
			var entry = GetMixerEntry(mixerId);
			var path = entry.GetMixerPath(mixerId);

			return _master.FindMatchingGroups(path)
			   .FirstOrDefault();
		}

		private AudioMixerGroupEntry GetMixerEntry(string mixerId) =>
			mixerId == _masterMixer.id ? _masterMixer.entry : ContentManager.Get<AudioMixerGroupEntry>(mixerId);

		#region Engine

		internal void Subscribe(EventsType type, Action action) => _engine.Subscribe(type, action);
		internal void Unsubscribe(EventsType type, Action action) => _engine.Unsubscribe(type, action);

		#endregion

		internal IEnumerable<(string, AudioMixerGroupEntry)> GetConfigurableMixer()
		{
			foreach (var (id, mixer) in ContentManager.GetAllEntries<AudioMixerGroupEntry>())
			{
				if (mixer.configurable)
					yield return (id, mixer);
			}
		}

		internal bool TryRegisterAudioPlayer(Type type, out IAudioPlayer player)
		{
			player = null;

			if (_settings.disableAudioPlayers.Contains(type.FullName))
				return false;

			player = type.CreateInstance<IAudioPlayer>();

			_audioPlayers ??= ListPool<IAudioPlayer>.Get();
			_audioPlayers.Add(player);

			return true;
		}

		private void SaveMixerVolume(string mixerId, float volume)
		{
			var saveKey = string.Format(MIXER_VOLUME_SAVE_KEY_FORMAT, mixerId);
			LocalSave.Save(saveKey, volume);
		}

		private float LoadMixerVolume(string mixerId)
		{
			var saveKey = string.Format(MIXER_VOLUME_SAVE_KEY_FORMAT, mixerId);
			return LocalSave.Load(saveKey, 1f);
		}

		internal void Register(IAudioListenerOwner owner) => _listenerLocator.Register(owner);

		internal void Unregister(IAudioListenerOwner owner) => _listenerLocator.Unregister(owner);

		public void SetMute(bool value, bool save = false) => SetMute(_masterMixer.id, value, save);

		public void SetMute(string mixerId, bool value, bool save = false)
		{
			if (!TryGetAudioMixerGroup(mixerId, out var container))
				return;

			if (container.mute == value)
				return;

			container.mute = value;

			//Просто обновление громкости
			SetVolume(mixerId, LoadMixerVolume(mixerId), false);

			if (save)
				SaveMute(mixerId, value);
		}

		public bool IsMute() => IsMute(_masterMixer.id);

		public bool IsMute(string mixerId) => TryGetAudioMixerGroup(mixerId, out var mixerGroup) && mixerGroup.mute;

		private void SaveMute(string mixerId, bool value)
		{
			var saveKey = string.Format(MIXER_MUTE_VOLUME_SAVE_KEY_FORMAT, mixerId);
			LocalSave.Save(saveKey, value);
		}

		private bool LoadMute(string mixerId)
		{
			var saveKey = string.Format(MIXER_MUTE_VOLUME_SAVE_KEY_FORMAT, mixerId);
			return LocalSave.Load(saveKey, false);
		}

		private class AudioMixerGroupContainer
		{
			public AudioMixerGroup group;
			public bool mute;

			public static implicit operator AudioMixerGroupContainer(AudioMixerGroup group) => new() {group = group};
			public static implicit operator AudioMixerGroup(AudioMixerGroupContainer container) => container.group;

			public static implicit operator bool(AudioMixerGroupContainer container) => container != null && container.group;
		}
	}

	public enum PlayErrorCode
	{
		None,
		NotFoundMixerGroup,
		NotEventEntry,
		EmptyPlaylist
	}
}
