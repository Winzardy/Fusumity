using System;
using Fusumity.Reactive;
using Sapientia.ServiceManagement;
using UnityEngine;

namespace UI
{
	public class UIButtonHold : IDisposable
	{
		private const float BEGIN_DELAY = 0.2f;
		private const float DURATION = 1.2f;

		private readonly CustomButton _button;

		private ICloseHoldHandler _handler;
		private float _time;
		private bool _holding;

		public UIButtonHold(CustomButton button)
		{
			_button = button;

			_button.Pressed += HandlePressed;
			_button.Released += HandleReleased;
		}

		public void Dispose()
		{
			Cancel();

			_button.Pressed -= HandlePressed;
			_button.Released -= HandleReleased;
		}

		private bool TryBegin()
		{
			if (!ServiceLocator.TryGet(out _handler))
				return false;

			_handler.Begin(_button.transform as RectTransform);
			return true;
		}

		private void Cancel()
		{
			var handler = _handler;
			Stop();

			handler?.Cancel();
		}

		private void Complete()
		{
			var handler = _handler;
			Stop();

			_button.SuppressClick();
			handler.Complete();
		}

		private void Stop()
		{
			if (!_holding)
				return;

			_holding = false;
			_handler = null;

			UnityLifecycle.UpdateEvent.UnSubscribe(HandleUpdated);
		}

		private void HandlePressed()
		{
			_time = 0;
			_holding = true;

			UnityLifecycle.UpdateEvent.Subscribe(HandleUpdated);
		}

		private void HandleReleased() => Cancel();

		private void HandleUpdated()
		{
			_time += Time.unscaledDeltaTime;

			if (_time < BEGIN_DELAY)
				return;

			if (_handler == null && !TryBegin())
				return;

			if (_time < DURATION)
			{
				_handler.Progress((_time - BEGIN_DELAY) / (DURATION - BEGIN_DELAY));
				return;
			}

			Complete();
		}
	}
}
