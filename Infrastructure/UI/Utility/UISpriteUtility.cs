using System;
using AssetManagement;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
	public static class UISpriteUtility
	{
		public static void SetSpriteSafe(this Image placeholder, UISpriteAssigner assigner, AssetReferenceEntry<Sprite> reference,
			Sprite icon = null, Sprite defaultIconSprite = null, Action callback = null) =>
			assigner.SetSprite(placeholder, reference, icon, defaultIconSprite, callback);

		public static void SetSprite(this UISpriteAssigner assigner, Image image, AssetReferenceEntry<Sprite> reference,
			Sprite icon = null, Sprite defaultIconSprite = null, Action callback = null)
		{
			if (!image)
				return;

			if (!reference.IsEmptyOrInvalid())
			{
				assigner.SetSprite(image, reference, callback);
			}
			else
			{
				assigner.TryCancelOrClear(image);
				image.sprite = icon ? icon : defaultIconSprite;
				callback?.Invoke();
			}
		}

		public static void TrySetSprite(this UISpriteAssigner assigner, Image image, UISpriteInfo info, Action callback = null, bool disableDuringLoad = false, Sprite defaultIcon = null)
		{
			if (image == null)
				return;

			if (info.sprite != null)
			{
				image.sprite = info.sprite;
			}
			else
			if (!info.reference.IsEmptyOrInvalid())
			{
				assigner.TrySetSprite(image, info.reference, callback, disableDuringLoad);
			}
			else
			if (defaultIcon != null)
			{
				image.sprite = defaultIcon;
			}
		}
	}
}
