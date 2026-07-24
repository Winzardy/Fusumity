using Fusumity.Utility;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
	public class ImageColorStateSwitcher : StateSwitcher<Color>
	{
		public Image image;

		public ColorChannelMask colorMask = ColorChannelMask.Default;

		public override Color Current { get => image?.color ?? Color.clear; set => OnStateSwitched(value); }

		protected override void OnStateSwitched(Color color)
		{
			if (image == null)
				return;
			image.color = image.color.WithChannelMask(color, colorMask);
		}

		private void Reset()
		{
			image = GetComponent<Image>();
		}
	}
}
