﻿using System;
using System.Collections.Generic;
using System.Linq;
using AssetManagement;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Sapientia.Collections;
using Sapientia.Pooling;
using Sirenix.OdinInspector;
using UnityEngine;
using ZenoTween.Utility;

namespace Audio
{
	/// <summary>
	/// Ответственнен за воспроизведение только одного события в отличие от AudioPlayer который в себе может содержать сложную логику
	/// </summary>
	public class AudioEventPlayer : MonoBehaviour
	{
		[ShowInInspector, ReadOnly]
		private AudioEventDefinition _current;

		private RectTransform _rectTransform;

		private AudioSource _singleAudioSource;
		private List<AudioSource> _audioSources;

		private bool _initialPlay;
		private bool _pause = true;

		private int _playCount = 0;
		private bool _cleared = true;
		private bool _finished;

		private Tween _fade;

		[ShowInInspector, ReadOnly, ShowIf(nameof(ShowCurrentTrack)), PropertyOrder(-1)]
		private int _currentTrack;

		private HashSet<AudioTrackEntry> _loadingTracks;

		public bool IsPlaying => IsLoaded || GetUsedSources().Any(source => source.isPlaying);

		private bool IsLoaded => _loadingTracks is {Count: > 0};
		public bool IsAlive => IsPaused || IsPlaying;
		public bool IsPaused => _pause;

		public event Action Finished;

		public void Dispose()
		{
			Release();

			_audioSources?.ReleaseToStaticPool();
			_loadingTracks?.ReleaseToStaticPool();
		}

		public void Setup(in AudioEventDefinition definition)
		{
			if (!_cleared)
			{
				AudioDebug.LogWarning("AudioEventPlayer not cleared?", this);
				Release();
			}

			_cleared = false;
			_finished = false;
			_playCount = 0;
			_current = definition;

			if (transform is RectTransform rectTransform)
				_rectTransform = rectTransform;
			else
				_rectTransform = null;

			if (_current.mode == AudioPlayMode.Sequence && _current.entry.tracks.Length > 1)
				AudioManager.Subscribe(EventsType.Update, OnSequenceUpdate);

			AudioManager.Subscribe(EventsType.LateUpdate, OnLateUpdate);

#if UNITY_EDITOR
			_cacheName = gameObject.name;
			gameObject.name = $"{_cacheName} ({_current.id})";
#endif
			Restart();
		}

		private void Restart()
		{
			_initialPlay = false;
			_playCount++;

			if (!_audioSources.IsNullOrEmpty())
				foreach (var source in _audioSources)
					ClearAndDisableSource(source);

			switch (_current.mode)
			{
				case AudioPlayMode.SameTime:

					if (_current.playlist.Length > 1)
					{
						if (_audioSources == null)
						{
							_audioSources = ListPool<AudioSource>.Get();

							if (_singleAudioSource)
								_audioSources.Add(_singleAudioSource);
						}

						foreach (var _ in _current.playlist)
						{
							var audioSource = GetOrCreate();
							_current.settings.Apply(audioSource);
						}
					}
					else
					{
						InitializeSingleSource();
					}

					break;

				case AudioPlayMode.Sequence:

					_currentTrack = 0;
					InitializeSingleSource();

					break;
			}

			foreach (var track in _current.playlist)
				LoadTrackAndTryPlayAsync(track).Forget();
		}

		public void Release()
		{
			if (_cleared)
				return;

			_fade?.KillSafe();

			_current.ReleasePlaylist();

			if (_singleAudioSource)
				ClearAndDisableSource(_singleAudioSource);

			if (!_audioSources.IsNullOrEmpty())
			{
				foreach (var source in _audioSources)
					ClearAndDisableSource(source);
			}

			if (_current.mode == AudioPlayMode.Sequence && _current.entry.tracks.Length > 1)
				AudioManager.Unsubscribe(EventsType.Update, OnSequenceUpdate);

			AudioManager.Unsubscribe(EventsType.LateUpdate, OnLateUpdate);

			_pause = true;
			_cleared = true;

#if UNITY_EDITOR
			gameObject.name = _cacheName;
#endif
		}

		private IEnumerable<AudioSource> GetUsedSources()
		{
			switch (_current.mode)
			{
				case AudioPlayMode.SameTime:
					if (_current.playlist.Length > 1)
					{
						if (_audioSources.IsNullOrEmpty())
							yield break;

						foreach (var source in _audioSources)
						{
							if (source.enabled)
								yield return source;
						}
					}
					else
					{
						yield return _singleAudioSource;
					}

					break;
				case AudioPlayMode.Sequence:
					yield return _singleAudioSource;
					break;
			}
		}

		private AudioSource GetOrCreate()
		{
			AudioSource source = null;
			foreach (var x in _audioSources)
			{
				if (x.enabled)
					continue;

				source = x;
				break;
			}

			if (source == null)
			{
				source = Create();
				_audioSources.Add(source);
			}

			source.enabled = true;
			return source;
		}

		private AudioSource Create()
		{
			var source = gameObject.AddComponent<AudioSource>();
			source.playOnAwake = false;
			return source;
		}

		/// <summary>
		/// Запускает воспроизведение звука или снимает паузу с воспроизведения.
		/// </summary>
		public void Play()
		{
			if (_pause)
			{
				if (_initialPlay)
				{
					foreach (var source in GetUsedSources())
						source.UnPause();
				}

				_pause = false;
			}

			if (_initialPlay)
				return;

			if (IsLoaded)
				return;

			_fade?.KillSafe();
			_fade = null;

			switch (_current.mode)
			{
				case AudioPlayMode.SameTime:

					Sequence sequence = null;

#if DebugLog
					var sourceCount = GetUsedSources()
					   .Count();
					if (sourceCount != _current.playlist.Length)
						AudioDebug.LogError(
							$"[ TASK-1964 ] AudioSource count != playlist length (count: {sourceCount}, lenght: {_current.playlist.Length})");
#endif
					foreach (var (source, index) in GetUsedSources()
						        .WithIndexSafe())
					{
						if (_current.fadeIn.HasValue)
							sequence ??= DOTween.Sequence();

						if (index >= _current.playlist.Length)
							break;

						var track = _current.playlist[index];

						var tween = source.Play(track, _current);

						if (tween != null)
							sequence?.Join(tween);
					}

					_fade = sequence;

					break;

				case AudioPlayMode.Sequence:
					if (_currentTrack >= _current.playlist.Length)
						return;

					var nextTrack = _current.playlist[_currentTrack];
					_fade = _singleAudioSource.Play(nextTrack, _current);
					break;
			}

			_fade?.Play();
			_initialPlay = true;
		}

		/// <summary>
		/// Останавливает воспроизведение звука, но не сбрасывает текущий положение клипа
		/// </summary>
		public void Pause()
		{
			if (_pause)
				return;

			if (_initialPlay)
				foreach (var source in GetUsedSources())
					source.Pause();

			_pause = true;
		}

		/// <summary>
		/// Останавливает воспроизведение звука и сбрасывает текущее положение клипа до 0
		/// </summary>
		public void Stop()
		{
			Fade(OnStop);

			void OnStop()
			{
				foreach (var source in GetUsedSources())
					source.Stop();

				_initialPlay = false;
				_pause = false;
				_currentTrack = 0;
			}
		}

		public void Fade(TweenCallback callback)
		{
			_fade?.KillSafe(true);
			_fade = null;

			if (_current.fadeOut.HasValue)
			{
				var sequence = DOTween.Sequence();
				foreach (var source in GetUsedSources())
				{
					var tween = source.DOFade(0, _current.fadeOut.Value);
					sequence.Join(tween);
				}

				sequence.OnComplete(callback).Play();
				_fade = sequence;
			}
			else
			{
				callback();
			}
		}

		public void SetPosition(Vector3 position) => transform.position = position;

		public void SetTarget(Transform target) => _current.transform = target;

		private void ClearAndDisableSource(AudioSource source)
		{
			source.clip = null;
			source.enabled = false;
		}

		private void OnSequenceUpdate()
		{
			if (!_initialPlay || IsAlive)
				return;

			TryNextTrackAndPlay();
		}

		private void OnLateUpdate()
		{
			if (IsAlive)
			{
				if (_current.settings.timeScaledPitch)
				{
					foreach (var (source, index) in GetUsedSources().WithIndexSafe())
					{
						var track = _current.playlist[index];
						source.pitch = Mathf.Clamp(track.pitch * Time.timeScale,
							AudioTrackEntry.MIN_PITCH,
							AudioTrackEntry.MAX_PITCH);
					}
				}

				if (_rectTransform)
					transform.position = _rectTransform.GetAudioSpatialPosition();
				else if (_current.transform)
					transform.position = _current.transform.position;
			}
			else
			{
				if (_current.repeat == 0 || _playCount < _current.repeat)
				{
					if (_current is {rerollOnRepeat: true, entry: not null})
						RollPlaylist();

					Restart();
					return;
				}

				Finish();
			}
		}

		private void RollPlaylist()
		{
			_current.ReleasePlaylist();
			_current.RollPlaylist();
		}

		private void Finish()
		{
			_finished = true;
			Finished?.Invoke();
		}

		private void TryNextTrackAndPlay()
		{
			_currentTrack++;

			if (_currentTrack >= _current.playlist.Length)
				return;

			_initialPlay = false;
			Play();
		}

		private async UniTaskVoid LoadTrackAndTryPlayAsync(AudioTrackEntry track)
		{
			if (!track.clipReference)
			{
				if (!track.clip)
					AudioDebug.LogError("Reference and clip can't be null same time!");

				return;
			}

			_loadingTracks ??= HashSetPool<AudioTrackEntry>.Get();
			_loadingTracks.Add(track);

			track.clip = await track.clipReference.LoadAsync();

			if (track.clip.loadState != AudioDataLoadState.Loaded)
			{
				track.clip.LoadAudioData();

				if (track.clip.loadType != AudioClipLoadType.Streaming)
					while (track.clip.loadState == AudioDataLoadState.Loading)
						await UniTask.NextFrame();
			}

			if (track.clip.loadType == AudioClipLoadType.Streaming)
				await UniTask.NextFrame();

			_loadingTracks.Remove(track);

			TryPlay();
		}

		private void InitializeSingleSource()
		{
			if (!_singleAudioSource)
				_singleAudioSource = _audioSources != null ? _audioSources[0] : Create();

			_current.settings.Apply(_singleAudioSource);
			_singleAudioSource.enabled = true;
		}

		private void TryPlay()
		{
			if (!_initialPlay && !_pause && _loadingTracks.Count <= 0)
				Play();
		}

		#region Editor

#if UNITY_EDITOR
		private string _cacheName;
#endif
		private bool ShowCurrentTrack => !_current.playlist.IsNullOrEmpty() && _current.mode == AudioPlayMode.Sequence;

		#endregion
	}
}
