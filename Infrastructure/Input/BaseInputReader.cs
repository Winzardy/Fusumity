using System;
using Fusumity.Reactive;

namespace InputManagement
{
	public interface IInputReader : IDisposable
	{
		public bool Holding { get; }
		public event Action<SwipeInfo> Swiped;
		public event Action<TapInfo> Tapped;
		public event Action<float> Zoomed;
		public event Action DoubleTapped;
	}

	public abstract class BaseInputReader : IDisposable, IInputReader
	{
		private const float DOUBLE_TAP_TIMEOUT = 0.5f;

		protected bool _enabled = true;

		private float _doubleTapDelay;
		private int _tapCount;

		public abstract bool Holding { get; }

		public event Action<SwipeInfo> Swiped;
		public event Action<TapInfo> Tapped;
		public event Action DoubleTapped;
		public event Action<float> Zoomed;

		public BaseInputReader()
		{
			UnityLifecycle.UpdateEvent.Subscribe(Update);
		}

		public void Dispose()
		{
			UnityLifecycle.UpdateEvent.UnSubscribe(Update);
		}

		protected virtual void InvokeTap(TapInfo info)
		{
			Tapped?.Invoke(info);
		}

		protected virtual void InvokeSwipe(SwipeInfo info)
		{
			Swiped?.Invoke(info);
		}

		protected virtual void InvokeZoom(float zoom)
		{
			Zoomed?.Invoke(zoom);
		}

		private void Update()
		{
			if (!_enabled)
				return;

			TapCountdown(UnityLifecycle.DeltaTime);
			ReadInput();
		}

		protected abstract void ReadInput();

		protected void RegisterTaps()
		{
			_tapCount++;

			if (_tapCount == 2)
			{
				_tapCount = 0;
				_doubleTapDelay = 0;

				DoubleTapped?.Invoke();
			}
		}		

		private void TapCountdown(float deltaTime)
		{
			if (_tapCount > 0)
			{
				_doubleTapDelay += deltaTime;
			}

			if (_doubleTapDelay > DOUBLE_TAP_TIMEOUT)
			{
				_tapCount = 0;
				_doubleTapDelay = 0;
			}
		}		
	}
}
