using System;
using AssetManagement;
using UnityEngine;

namespace UI
{
	[Serializable]
	public struct UISpriteInfo : IEquatable<UISpriteInfo>
	{
		public IAssetReferenceEntry<Sprite> reference;
		public Sprite sprite;

		public bool IsEmptyOrInvalid() => sprite == null && reference.IsEmptyOrInvalid();

		public static implicit operator UISpriteInfo(Sprite sprite) => new() {sprite                            = sprite};
		public static implicit operator UISpriteInfo(AssetReferenceEntry<Sprite> reference) => new() {reference = reference};

		public override string ToString()
		{
			if (sprite != null)
				return sprite.name;

			if (!reference.IsEmptyOrInvalid())
				return reference.ToString();

			return "empty";
		}

		public bool Equals(UISpriteInfo other)
		{
			return Equals(reference, other.reference) && Equals(sprite, other.sprite);
		}

		public override bool Equals(object obj)
		{
			return obj is UISpriteInfo other && Equals(other);
		}

		public override int GetHashCode()
		{
			return HashCode.Combine(reference, sprite);
		}
	}
}
