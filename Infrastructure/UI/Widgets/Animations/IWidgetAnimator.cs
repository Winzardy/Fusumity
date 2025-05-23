using System;
using DG.Tweening;
using Sapientia.Extensions;

namespace UI
{
	public struct WidgetAnimationArgs
	{
		public string key;

		public TweenCallback startCallback;
		public TweenCallback endCallback;

		public bool IsEmpty => key.IsNullOrWhiteSpace();
		public static implicit operator WidgetAnimationArgs(string key) => new() {key = key};
	}

	/// <summary>
	/// Animator for open/close animations
	/// </summary>
	public interface IWidgetAnimator : IDisposable
	{
		public string LastKey { get; }

		public void Setup(UIWidget widget);

		public void Play(in WidgetAnimationArgs args, bool immediate = false);

		public void Stop(string key, bool complete = false);

		public void Pause(string key);

		public void Resume(string key);
	}

	public interface IWidgetAnimator<in TLayout> : IWidgetAnimator
		where TLayout : UIBaseLayout
	{
		public bool SetupLayout(TLayout layout);
	}
}
