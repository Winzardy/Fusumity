using System;
using UnityEngine;

namespace InputManagement
{
	[Obsolete("Нужно обновить, возможно надо отказаться от мысли на разделение по платформам...")]
	public class MobileInputReader : BaseInputReader
	{
		private const float ZOOM_MULT = 0.01f;

		public override bool Holding => Input.touchCount > 0;

		protected override void ReadInput()
		{
			if (!Holding)
				return;

			TouchCount = Input.touchCount;

			var touch = Input.GetTouch(0);
			InvokeTap(CreateClickInfo(touch));

			switch (touch.phase)
			{
				case TouchPhase.Began:
				{
					if (Input.touchCount != 1)
						break;

					RegisterTaps();
					break;
				}
				case TouchPhase.Moved:
					InvokeSwipe(new SwipeInfo
					{
						position = touch.position,
						delta = touch.deltaPosition,
						touchCount = Input.touchCount,
					});
					break;
			}

			if(Input.touchCount == 2)
			{
				var secondTouch = Input.GetTouch(1);

				var firstPrevPos = touch.position - touch.deltaPosition;
				var secondPrevPos = secondTouch.position - secondTouch.deltaPosition;

				var prevMag = (firstPrevPos - secondPrevPos).magnitude;
				var currentMag = (touch.position - secondTouch.position).magnitude;

				var zoomDelta = (currentMag - prevMag);
				InvokeZoom(zoomDelta * ZOOM_MULT);
			}
		}

		private TapInfo CreateClickInfo(Touch touch) => new(touch.phase, touch.position, touch.fingerId);
	}
}
