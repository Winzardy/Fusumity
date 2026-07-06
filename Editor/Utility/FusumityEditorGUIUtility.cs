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

		public static Rect AlignBottom(this Rect rect, float height, float offset = 0)
		{
			rect.y = rect.y + rect.height - height - offset;
			rect.height = height;
			return rect;
		}
	}

	public static class FusumityGUIEditorLayout
	{
		private static readonly Vector2 _objectFieldIconSpriteOffset = new(0.5f, 0.5f);
		private static readonly Vector2 _objectFieldIconSpriteShrink = new Vector2(-0.5f, -0.5f);

		public static void DrawObjectFieldIconSprite(Rect rect, Sprite sprite)
		{
			DrawSprite(rect, sprite, _objectFieldIconSpriteOffset, _objectFieldIconSpriteShrink);
		}

		public static void DrawSprite(Rect rect, Sprite sprite)
		{
			DrawSprite(rect, sprite, Vector2.zero, Vector2.zero);
		}

		private static void DrawSprite(Rect rect, Sprite sprite, Vector2 offset, Vector2 shrink)
		{
			if (!sprite || !sprite.texture)
				return;

			var tex = sprite.texture;
			var tr = sprite.textureRect;
			var texCoords = new Rect(
				tr.x / tex.width,
				tr.y / tex.height,
				tr.width / tex.width,
				tr.height / tex.height);

			rect.x += offset.x;
			rect.y += offset.y;
			var drawRect = FitRect(rect, tr.size);
			drawRect.height += shrink.x;
			drawRect.width += shrink.y;
			GUI.DrawTextureWithTexCoords(drawRect, tex, texCoords);
		}

		private static Rect FitRect(Rect rect, Vector2 size)
		{
			if (size.x <= 0f || size.y <= 0f)
				return rect;

			var aspect = size.x / size.y;
			var rectAspect = rect.width / rect.height;

			if (aspect > rectAspect)
			{
				var height = rect.width / aspect;
				return new Rect(
					rect.x,
					rect.y + (rect.height - height) * 0.5f,
					rect.width,
					height);
			}

			var width = rect.height * aspect;
			return new Rect(
				rect.x + (rect.width - width) * 0.5f,
				rect.y,
				width,
				rect.height);
		}
	}
}
