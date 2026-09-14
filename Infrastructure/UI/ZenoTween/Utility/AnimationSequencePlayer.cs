using DG.Tweening;
using System;
using UnityEngine;
using UnityEngine.Assertions;

namespace ZenoTween.Utility
{
	using UnityObject = UnityEngine.Object;

	public class AnimationSequencePlayer : IDisposable
	{
		private AnimationSequence _sequence;
		private bool _cached;
		private object _target;
		private GameObject _link;

		private Tween _tween;

		public bool IsPlaying { get => _tween.IsActive() && _tween.active && _tween.IsPlaying() && !_tween.IsComplete(); }

		public AnimationSequencePlayer(AnimationSequence sequence, bool cached = false, UnityObject owner = null)
		{
			Assert.IsNotNull(sequence, "Sequence cannot be null.");

			_sequence = sequence;
			_cached   = cached;
			_target   = owner;
			_link     = owner switch
			{
				GameObject gameObject => gameObject,
				Component component => component.gameObject,
				_ => null
			};
		}

		public void Dispose()
		{
			_tween.KillSafe();
			_tween = null;
		}

		public void Play(TweenCallback onComplete = null)
		{
			var args = onComplete != null ? new AnimationSequencePlayerArgs {onComplete = onComplete} : default;

			Play(args);
		}

		public void Play(AnimationSequencePlayerArgs args)
		{
			if (_cached)
			{
				_tween ??= CreateTween()
					?.SetAutoKill(false);

				if (_tween == null)
					return;

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
				var sequence = CreateSequence();
				if (sequence == null)
					return;

				if (args.delay > 0)
				{
					sequence.PrependInterval(args.delay);
				}

				if (args.onStart != null)
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

		private Tween CreateTween()
		{
			var tween = _sequence.ToTween(_target);
			return Link(tween);
		}

		private Sequence CreateSequence()
		{
			var sequence = _sequence.ToSequence(_target);
			return Link(sequence) as Sequence;
		}

		private Tween Link(Tween tween)
		{
			if (tween != null && _link != null)
				tween.SetLink(_link, LinkBehaviour.KillOnDestroy);

			return tween;
		}

		public void Stop(bool rewind = false)
		{
			if (_cached)
			{
				if (rewind)
				{
					_tween?.Rewind();
				}
				else
				{
					_tween?.Pause();
				}
			}
			else
			{
				_tween.KillSafe();
			}
		}

		public void Complete(bool withCallbacks = true, bool rewind = false)
		{
			if (IsPlaying)
			{
				_tween.Complete(withCallbacks);

				if (_cached && rewind)
				{
					_tween.Rewind();
				}
			}
		}
	}

	public struct AnimationSequencePlayerArgs
	{
		public float delay;

		public TweenCallback onStart;
		public TweenCallback onComplete;
	}
}
