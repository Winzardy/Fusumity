using System;
using UnityEngine;

namespace Audio
{
	public class AudioPlayback : IDisposable
	{
		private AudioEventPlayerPool _pool;

		private AudioEventPlayer _player;

		public bool IsPlaying => _player && _player.IsPlaying;

		public AudioPlayback(AudioEventPlayerPool pool, Vector3 position) : this(pool) => _player.SetPosition(position);

		public AudioPlayback(AudioEventPlayerPool pool, Transform transform) : this(pool) => _player.SetTarget(transform);

		public bool IsDisposed => !_player;

		/// <summary>
		/// Можно считать это событие finish звука,
		/// но чтобы не искажать смысл будет название Disposed вместо Finished!
		/// </summary>
		public event Action<AudioPlayback> Disposed;
		/// <summary>
		/// Событие отличается от Disposed тем что срабатывает сразу как вызывали Dispose.
		/// а Disposed перед выгрузкой в пул, то есть Disposed учитывает возможный FadeOut звука,
		/// а DisposeStarted нет!
		/// </summary>
		public event Action<AudioPlayback> DisposeStarted;

		private AudioPlayback(AudioEventPlayerPool pool)
		{
			_pool = pool;
			_player = _pool.Get();
			_player.Finished += OnFinished;
		}

		public void Dispose(bool force) => Release(force);
		public void Dispose() => Release();

		public void Setup(in AudioEventArgs args, bool autoPlay = false)
		{
			_player.Setup(in args);

			if (autoPlay)
				Play();
		}

		public void Play()
		{
			if (IsDisposed)
				return;
			_player.Play();
		}

		/// <summary>
		/// Пауза без сброса на начало
		/// </summary>
		public void Pause()
		{
			if (IsDisposed)
				return;
			_player.Pause();
		}

		/// <summary>
		/// Останавливает с сбросом до начала
		/// </summary>
		public void Stop()
		{
			if (IsDisposed)
				return;
			_player.Stop();
		}

		public AudioPlayback SetPosition(Vector3 position)
		{
			if (IsDisposed)
				return this;

			_player.SetPosition(position);
			return this;
		}

		public AudioPlayback SetTarget(Transform target)
		{
			if (IsDisposed)
				return this;

			_player.SetTarget(target);
			return this;
		}

		private void OnFinished() => Release(true);

		private void Release(bool force = false)
		{
			if (IsDisposed)
				return;

			DisposeStarted?.Invoke(this);
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
				Disposed?.Invoke(this);
				_pool.Release(player);
			}
		}
	}
}
