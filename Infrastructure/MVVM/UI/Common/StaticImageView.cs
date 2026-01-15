using AssetManagement;
using DG.Tweening;
using Fusumity.Utility;
using UI;
using UnityEngine;
using UnityEngine.UI;

namespace Fusumity.MVVM.UI
{
	public class StaticImageView : View<IAssetReferenceEntry<Sprite>, Image>
	{
		private UISpriteAssigner _assigner;
		private Tween _tween;

		private bool _animated;

		public StaticImageView(Image layout, bool animated = true) : base(layout)
		{
			AddDisposable(_assigner = new UISpriteAssigner());
			layout.SetActive(false);

			_animated = animated;
		}

		protected override void OnDispose()
		{
			_tween?.Kill();
		}

		protected override void OnUpdate(IAssetReferenceEntry<Sprite> entry)
		{
			_layout.SetActive(true);
			_assigner.TrySetSprite(_layout, entry, PlayAppearTween, true);
		}

		protected override void OnNullViewModel()
		{
			_layout.SetActive(false);
		}

		private void PlayAppearTween()
		{
			if (!_animated)
				return;

			_tween = _layout.DOFade(1, 0.45f).From(0);
		}
	}
}
