using Sapientia.Extensions;
using UnityEngine;

namespace Fusumity.Utility
{
	public static class RectUtility
	{
		public static float GetMaxRectSide(this in Rect rect)
			=> rect.width.Max(rect.height);

		public static float GetMinRectSide(this in Rect rect)
			=> rect.width.Min(rect.height);
	}
}
