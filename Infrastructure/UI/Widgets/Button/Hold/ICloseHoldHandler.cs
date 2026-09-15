using UnityEngine;

namespace UI
{
	public interface ICloseHoldHandler
	{
		/// <summary>
		/// Заявка на удержание, отказ означает что удержание уже ведёт другая кнопка
		/// </summary>
		bool Begin(RectTransform button);

		void Progress(float value);

		void Cancel();

		void Complete();
	}
}
