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

		public static void SetSprite(this UISpriteAssigner assigner, Image placeholder, AssetReferenceEntry<Sprite> reference,
			Sprite icon = null, Sprite defaultIconSprite = null, Action callback = null)
		{
			if (!placeholder)
				return;

			if (!reference.IsEmpty())
			{
				assigner.SetSprite(placeholder, reference, callback);
			}
			else
			{
				assigner.TryCancelOrClear(placeholder);
				placeholder.sprite = icon ? icon : defaultIconSprite;
				callback?.Invoke();
			}
		}
	}
}
