using Fusumity.Utility;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
	public class ImageColorStateSwitcher : StateSwitcher<Color>
	{
		public Image image;

		public ColorChannelMask colorMask = ColorChannelMask.Default;

		public override Color Current { get => image.color; set => OnStateSwitched(value); }

		protected override void OnStateSwitched(Color color)
		{
			image.color = image.color.WithChannelMask(color, colorMask);
		}
	}
}
