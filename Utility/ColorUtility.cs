using Sapientia.Collections;
using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
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

		public static Color WithChannelMask(this Color origin, Color newColor, ColorChannelMask channelMask)
		{
			origin.r = channelMask.r ? newColor.r : origin.r;
			origin.g = channelMask.g ? newColor.g : origin.g;
			origin.b = channelMask.b ? newColor.b : origin.b;
			origin.a = channelMask.a ? newColor.a : origin.a;
			return origin;
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

	[Serializable]
	[InlineProperty(LabelWidth = 13)]
	public struct ColorChannelMask
	{
		public static ColorChannelMask Default = new ColorChannelMask {r = true, g = true, b = true, a = true};

		[HorizontalGroup]
		public bool r;
		[HorizontalGroup]
		public bool g;
		[HorizontalGroup]
		public bool b;
		[HorizontalGroup]
		public bool a;
	}

}
