using System;
using Fusumity.Reactive;
using Sapientia.ServiceManagement;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
	public class UIHoldButtonView : IDisposable
	{
		private const float INDICATOR_DELAY = 0.2f;
		private const float HOLD_DURATION = 1.2f;

		private readonly HoldPointerTrigger _trigger;
		private readonly RectTransform _rectTransform;

		private IHoldIndicator _indicator;
		private bool _indicatorShown;

		private float _holdTime;
		private bool _holding;

		public event Action Completed;

		public UIHoldButtonView(Button button)
		{
			if (!button.TryGetComponent(out _trigger))
				_trigger = button.gameObject.AddComponent<HoldPointerTrigger>();

			_rectTransform = button.transform as RectTransform;

			_trigger.Pressed += HandlePressed;
			_trigger.Released += HandleReleased;
		}

		public void Dispose()
		{
			StopHold();

			_trigger.Pressed -= HandlePressed;
			_trigger.Released -= HandleReleased;
		}

		private void StartHold()
		{
			_holdTime = 0;
			_holding = true;

			UnityLifecycle.UpdateEvent.Subscribe(HandleUpdated);
		}

		private void StopHold()
		{
			if (!_holding)
				return;

			_holding = false;
			_holdTime = 0;

			UnityLifecycle.UpdateEvent.UnSubscribe(HandleUpdated);
			HideIndicator();
		}

		private void UpdateIndicator()
		{
			if (_holdTime < INDICATOR_DELAY)
				return;

			if (!_indicatorShown)
			{
				if (!ServiceLocator.TryGet(out _indicator))
					return;

				_indicator.Show(_rectTransform);
				_indicatorShown = true;
			}

			_indicator.SetProgress((_holdTime - INDICATOR_DELAY) / (HOLD_DURATION - INDICATOR_DELAY));
		}

		private void HideIndicator()
		{
			if (!_indicatorShown)
				return;

			_indicatorShown = false;
			_indicator.Hide();
		}

		private void HandlePressed() => StartHold();

		private void HandleReleased() => StopHold();

		private void HandleUpdated()
		{
			_holdTime += Time.unscaledDeltaTime;

			UpdateIndicator();

			if (_holdTime < HOLD_DURATION)
				return;

			StopHold();

			_trigger.SuppressClick();
			Completed?.Invoke();
		}
	}
}
