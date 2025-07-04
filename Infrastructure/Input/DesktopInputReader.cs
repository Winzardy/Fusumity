﻿using UnityEngine;

namespace InputManagement
{
	public class DesktopInputReader : BaseInputReader
	{
		private Vector3 _lastMousePosition;

		private Vector3 _cacheDownMousePosition;
		private float _cacheDownTime;
		private int _cacheDownTouchCount;

		private bool _holding;
		public override bool Holding => _holding;

		protected override void ReadInput()
		{
			var mousePosition = Input.mousePosition;

			if (Input.GetMouseButtonDown(0))
			{
				_holding = true;
				_lastMousePosition = mousePosition;

				_cacheDownMousePosition = mousePosition;
				_cacheDownTime = Time.realtimeSinceStartup;
				_cacheDownTouchCount = Input.touchCount;

				Invoke(CreateTapInfo(TouchPhase.Began, mousePosition));
				RegisterTaps();
			}
			else if (Input.GetMouseButton(0))
			{
				var delta = mousePosition - _lastMousePosition;
				var isMoved = delta.sqrMagnitude > 0;
				var touchPhase = isMoved ? TouchPhase.Moved : TouchPhase.Stationary;

				Invoke(CreateTapInfo(touchPhase, mousePosition));

				if (isMoved)
				{
					Invoke(new SwipeInfo
					{
						position = mousePosition,
						delta = delta,
						touchCount = Input.touchCount,
						phase = touchPhase,
						time =  Time.realtimeSinceStartup - _cacheDownTime
					});
					_lastMousePosition = mousePosition;
				}
			}
			else if (Input.GetMouseButtonUp(0))
			{
				var delta = mousePosition - _cacheDownMousePosition;
				var isMoved = delta.sqrMagnitude > 0;

				if (isMoved)
				{
					Invoke(new SwipeInfo
					{
						position = mousePosition,
						delta = delta,
						touchCount = _cacheDownTouchCount,
						phase = TouchPhase.Ended,
						time =  Time.realtimeSinceStartup - _cacheDownTime
					});
					_cacheDownMousePosition = mousePosition;
				}

				Invoke(CreateTapInfo(TouchPhase.Ended, mousePosition));
				_holding = false;
			}
		}

		private TapInfo CreateTapInfo(TouchPhase phase, Vector3 mousePosition) => new(phase, mousePosition);
	}
}
