using UnityEngine;

namespace UI
{
	public interface ICloseHoldHandler
	{
		void Attach(UIWidget host, RectTransform button);

		void Detach(RectTransform button);

		bool Begin(RectTransform button);

		void Progress(float value);

		void Cancel();

		void Complete();
	}
}
