using Fusumity.Reactive;
using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Fusumity.Utility
{
	// TODO: need to test this shit on device, doing it blindly
	public class PinchDetector : IDisposable
	{
		private IInputRouter _inputRouter;
		private float _multiplier;

		private int _firstFingerId = -1;
		private int _secondFingerId = -1;
		private Vector2 _firstPos, _secondPos;
		private Vector2 _firstPrevPos, _secondPrevPos;

		public event Action<float> Pinching;

		public PinchDetector(IInputRouter inputRouter, float multiplier = 0.1f)
		{
			_inputRouter = inputRouter;
			_multiplier = multiplier;

			_inputRouter.PointerUp += HandlePointerUp;
			_inputRouter.PointerDown += HandlePointerDown;
			_inputRouter.PointerMoved += HandlePointerMoved;

#if UNITY_EDITOR
			UnityLifecycle.UpdateEvent.Subscribe(Update);
#endif
		}

		public void Dispose()
		{
#if UNITY_EDITOR
			UnityLifecycle.UpdateEvent.UnSubscribe(Update);
#endif
			_inputRouter.PointerUp -= HandlePointerUp;
			_inputRouter.PointerDown -= HandlePointerDown;
			_inputRouter.PointerMoved -= HandlePointerMoved;
		}

		private void RegisterFingers(PointerEventData eventData)
		{
			if (_firstFingerId == -1)
			{
				_firstFingerId = eventData.pointerId;
				_firstPos = _firstPrevPos = eventData.position;
			}
			else if (_secondFingerId == -1)
			{
				_secondFingerId = eventData.pointerId;
				_secondPos = _secondPrevPos = eventData.position;
			}
		}

		private void ProcessPinch(PointerEventData eventData)
		{
			if (eventData.pointerId == _firstFingerId)
			{
				_firstPrevPos = _firstPos;
				_firstPos = eventData.position;
			}
			else if (eventData.pointerId == _secondFingerId)
			{
				_secondPrevPos = _secondPos;
				_secondPos = eventData.position;
			}

			if (_firstFingerId != -1 && _secondFingerId != -1)
			{
				var prevMag = (_firstPrevPos - _secondPrevPos).magnitude;
				var currentMag = (_firstPos - _secondPos).magnitude;
				var pinchDelta = currentMag - prevMag;

				Pinching?.Invoke(pinchDelta * _multiplier);
			}
		}

		public void CancelPinch(PointerEventData eventData)
		{
			if (eventData.pointerId == _firstFingerId)
			{
				_firstFingerId = -1;
			}
			else if (eventData.pointerId == _secondFingerId)
			{
				_secondFingerId = -1;
			}
		}

		private void HandlePointerDown(PointerEventData eventData)
		{
			RegisterFingers(eventData);
		}

		private void HandlePointerMoved(PointerEventData eventData)
		{
			ProcessPinch(eventData);
		}

		private void HandlePointerUp(PointerEventData eventData)
		{
			CancelPinch(eventData);
		}

#if UNITY_EDITOR
		private void Update()
		{
			var scroll = Input.GetAxis("Mouse ScrollWheel");
			if (scroll != 0)
			{
				Pinching?.Invoke(scroll);
			}
		}
#endif
	}
}
