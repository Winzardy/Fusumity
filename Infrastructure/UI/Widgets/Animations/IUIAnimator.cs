using System;
using Sapientia.Extensions;

namespace UI
{
	public delegate void AnimationCallback(string key);

	public struct WidgetAnimationArgs
	{
		public string key;

		public AnimationCallback startCallback;
		public AnimationCallback endCallback;

		public bool completeOnKill;
		public bool disablePrevEndCallbackOnKill;

		public bool IsEmpty => key.IsNullOrWhiteSpace();
		public static implicit operator WidgetAnimationArgs(string key) => new() {key = key};
	}

	/// <summary>
	/// Animator for open/close animations
	/// </summary>
	public interface IZenoAnimator : IDisposable
	{
		string LastKey { get; }

		void Play(in WidgetAnimationArgs args, bool immediate = false);

		void Stop(string key, bool complete = false);

		void Pause(string key);

		void Resume(string key);
	}

	public interface IUIAnimator<in TLayout> : IZenoAnimator
		where TLayout : UIBaseLayout
	{
		bool SetupLayout(TLayout layout);
	}
}
