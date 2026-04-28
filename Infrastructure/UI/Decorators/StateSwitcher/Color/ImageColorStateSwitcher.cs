using UnityEngine;
using UnityEngine.UI;

namespace UI
{
	public class ImageColorStateSwitcher : StateSwitcher<Color>
	{
		public Image image;

		protected override void OnStateSwitched(Color color)
		{
			image.color = color;
		}
	}
}
