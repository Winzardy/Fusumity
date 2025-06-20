using System;
using UnityEngine;

namespace InputManagement
{
	[Obsolete("Нужно обновить, возможно надо отказаться от мысли на разделение по платформам...")]
	public class MobileInputReader : BaseInputReader
	{
		public override bool Holding => Input.touchCount > 0;

		protected override void ReadInput()
		{
			if (!Holding)
				return;

			var touch = Input.GetTouch(0);
			Invoke(CreateClickInfo(touch));

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
					Invoke(new SwipeInfo
					{
						position = touch.position,
						delta = touch.deltaPosition,
						touchCount = Input.touchCount,
					});
					break;
			}
		}

		private TapInfo CreateClickInfo(Touch touch) => new(touch.phase, touch.position, touch.fingerId);
	}
}
