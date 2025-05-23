using System.Collections.Generic;
using Fusumity.Utility;
using UnityEngine.UI;

namespace UI
{
	public static class GraphicUtility
	{
		public static void SetAlpha(this Graphic graphic, float alpha)
		{
			if (!graphic)
				return;

			graphic.color = graphic.color.WithAlpha(alpha);
		}

		public static void SetAlpha(this IEnumerable<Graphic> graphics, float alpha)
		{
			foreach (var graphic in graphics)
			{
				graphic.SetAlpha(alpha);
			}
		}
	}
}
