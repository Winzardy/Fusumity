using System;
using UnityEngine;

namespace Audio
{
	// TODO: если в один фрейм делать Play Dispose Play, то playback не уходит в пул... подумать что с этим сделать
	public class AudioPlayback : IDisposable
	{
		private AudioEventPlayerPool _pool;

		private AudioEventPlayer _player;

		public bool IsPlaying { get => _player && _player.IsPlaying; }

		public bool IsReleased { get => _player == null; }

		/// <summary>
		/// Можно считать это событие finish звука,
		/// но чтобы не искажать смысл будет название Released вместо Finished!
		/// </summary>
		public event Action<AudioPlayback> Released;

		/// <summary>
		/// Событие отличается от Released тем что срабатывает сразу как вызывали Release.
		/// а Released перед выгрузкой в пул, то есть Released учитывает возможный FadeOut звука,
		/// а BeforeRelease нет!
		/// </summary>
		public event Action<AudioPlayback> BeforeRelease;

		public AudioEventPlayer Player { get => _player; }

		public AudioPlayback(AudioEventPlayerPool pool, Vector3 position) : this(pool) => _player.SetPosition(position);

		public AudioPlayback(AudioEventPlayerPool pool, Transform transform) : this(pool) => _player.SetTarget(transform);

		private AudioPlayback(AudioEventPlayerPool pool)
		{
			_pool = pool;
			_player = _pool.Get();
			_player.Finished += OnFinished;
		}

		public void Dispose(bool force) => Release(force);
		public void Dispose() => Release();

		public void Setup(in AudioEventDefinition definition, bool autoPlay = false)
		{
			_player.Setup(in definition);

			if (autoPlay)
				Play();
		}

		public void Play()
		{
			if (IsReleased)
				return;
			_player.Play();
		}

		/// <summary>
		/// Пауза без сброса на начало
		/// </summary>
		public void Pause()
		{
			if (IsReleased)
				return;
			_player.Pause();
		}

		/// <summary>
		/// Останавливает с сбросом до начала
		/// </summary>
		public void Stop()
		{
			if (IsReleased)
				return;
			_player.Stop();
		}

		public AudioPlayback SetPosition(Vector3 position)
		{
			if (IsReleased)
				return this;

			_player.SetPosition(position);
			return this;
		}

		public AudioPlayback SetTarget(Transform target)
		{
			if (IsReleased)
				return this;

			_player.SetTarget(target);
			return this;
		}

		private void OnFinished() => Release(true);

		private void Release(bool force = false)
		{
			if (IsReleased)
				return;

			BeforeRelease?.Invoke(this);
			var player = _player;

			player.Finished -= OnFinished;
			_player = null;

			if (force)
			{
				OnRelease();
				return;
			}

			player.Fade(OnRelease);

			void OnRelease()
			{
				Released?.Invoke(this);
				_pool.Release(player);
			}
		}
	}
}
