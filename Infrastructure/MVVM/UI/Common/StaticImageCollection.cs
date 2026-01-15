using AssetManagement;
using UnityEngine;
using UnityEngine.UI;

namespace Fusumity.MVVM.UI
{
	public class StaticImageCollection : ViewCollection<IAssetReferenceEntry<Sprite>, StaticImageView, Image>
	{
		public StaticImageCollection(ViewCollectionLayout<Image> layout) : base(layout)
		{
		}

		protected override StaticImageView CreateViewInstance(Image layout) => new StaticImageView(layout);
	}
}
