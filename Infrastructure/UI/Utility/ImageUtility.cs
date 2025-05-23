using UnityEngine;
using UnityEngine.UI;

namespace UI
{
	public static class ImageUtility
	{
		public static void SetSpriteOrDisable(this Image image, Sprite sprite)
		{
			image.sprite = sprite;
			image.enabled = sprite;
		}
	}
}
