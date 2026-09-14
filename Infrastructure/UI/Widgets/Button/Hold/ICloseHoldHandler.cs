using UnityEngine;

namespace UI
{
	public interface ICloseHoldHandler
	{
		void Begin(RectTransform button);

		void Progress(float value);

		void Cancel();

		void Complete();
	}
}
