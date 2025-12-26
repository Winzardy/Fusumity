using AssetManagement;
using System;
using UnityEngine;

namespace UI
{
	[Serializable]
	public struct UISpriteInfo
	{
		public IAssetReferenceEntry<Sprite> reference;
		public Sprite sprite;

		public bool IsEmptyOrInvalid() => sprite == null && reference.IsEmptyOrInvalid();

		public static implicit operator UISpriteInfo(Sprite sprite) => new() { sprite = sprite };
		public static implicit operator UISpriteInfo(AssetReferenceEntry<Sprite> reference) => new() { reference = reference };
	}
}
