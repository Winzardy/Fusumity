using UnityEngine;

namespace Fusumity.Editor
{
	public static class FusumityEditorGUIUtility
	{
		public static Rect AlignLeft(this Rect rect, float width, float offset = 0)
		{
			rect.x += offset;
			rect.width = width;
			return rect;
		}

		public static Rect AlignRight(this Rect rect, float width, float offset = 0)
		{
			rect.x = rect.x + rect.width - width - offset;
			rect.width = width;
			return rect;
		}
	}
}
