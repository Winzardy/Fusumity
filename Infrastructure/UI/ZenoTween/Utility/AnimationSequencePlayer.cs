using DG.Tweening;
using System;
using UnityEngine.Assertions;

namespace ZenoTween.Utility
{
	public class AnimationSequencePlayer : IDisposable
	{
		private AnimationSequence _sequenceConfig;
		private bool _cached;

		private Tween _tween;

		public bool IsPlaying { get => _tween.IsActive() && _tween.active && _tween.IsPlaying() && !_tween.IsComplete(); }

		public AnimationSequencePlayer(AnimationSequence sequenceConfig, bool cached = false)
		{
			Assert.IsNotNull(sequenceConfig, "Sequence cannot be null.");

			_sequenceConfig = sequenceConfig;
			_cached = cached;
		}

		public void Dispose()
		{
			_tween.KillSafe();
			_tween = null;
		}

		public void Play(TweenCallback onComplete = null)
		{
			var args = onComplete != null ?
				new AnimationSequencePlayerArgs { onComplete = onComplete } :
				default;

			Play(args);
		}

		public void Play(AnimationSequencePlayerArgs args)
		{
			if (_cached)
			{
				_tween ??= _sequenceConfig
					.ToTween(this)
					.SetAutoKill(false);

				if (_tween.playedOnce)
				{
					_tween.Restart();
				}
				else
				{
					_tween.Play();
				}
			}
			else
			{
				_tween.KillSafe();
				var sequence = _sequenceConfig.ToSequence(this);

				if(args.onStart != null)
				{
					sequence.PrependCallback(args.onStart);
				}

				if (args.onComplete != null)
				{
					sequence.AppendCallback(args.onComplete);
				}

				_tween = sequence;
			}
		}

		public void Stop()
		{
			if (_cached)
			{
				_tween?.Pause();
			}
			else
			{
				_tween.KillSafe();
			}
		}

		public void Complete(bool withCallbacks = true)
		{
			if (IsPlaying)
			{
				_tween.Complete(withCallbacks);
			}
		}
	}

	public struct AnimationSequencePlayerArgs
	{
		public TweenCallback onStart;
		public TweenCallback onComplete;
	}
}
