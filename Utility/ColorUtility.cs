using Sapientia.Collections;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Fusumity.Utility
{
	public static class UnityColorUtility
	{
		public static readonly Color ERROR = new Color(1, 0, 0.8f, 1);

		public static Color WithAlpha(this Color color, float alpha)
		{
			color.a = alpha;
			return color;
		}

		public static void SetColor(this IList<Image> images, Color color)
		{
			if (images.IsNullOrEmpty())
				throw new NullReferenceException(nameof(images));

			for (int i = 0; i < images.Count; i++)
			{
				var image = images[i];
				image.color = color;
			}
		}
	}
}
