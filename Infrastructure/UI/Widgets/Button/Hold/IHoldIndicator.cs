using UnityEngine;

namespace UI
{
	public interface IHoldIndicator
	{
		void Show(RectTransform target);

		void SetProgress(float progress);

		void Hide();
	}
}
