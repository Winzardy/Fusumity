using UnityEngine;

namespace UI.Popovers
{
	public class UIBasePopoverLayout : UIBaseCanvasGroupLayout
	{
		public override bool UseLayoutAnimations => useAnimations;
		public bool useAnimations = true;

		#region Blend Mode For Layout Animations

		[Tooltip("Режим смешивания для анимации открытия. С помощью выбранного режима можно либо дополнить (<b>"+nameof(AnimationSequenceBlendMode.Additive)+"</b>) анимацию виджета, либо переопдределить (<b>"+nameof(AnimationSequenceBlendMode.Override)+"</b>)")]
		public AnimationSequenceBlendMode openingBlendMode;

		public override AnimationSequenceBlendMode OpeningBlendMode => openingBlendMode;

		[Tooltip("Режим смешивания для анимации закрытия. С помощью выбранного режима можно либо дополнить (<b>"+nameof(AnimationSequenceBlendMode.Additive)+"</b>) анимацию виджета, либо переопдределить (<b>"+nameof(AnimationSequenceBlendMode.Override)+"</b>)")]
		public AnimationSequenceBlendMode closingBlendMode;

		public override AnimationSequenceBlendMode ClosingBlendMode => closingBlendMode;

		#endregion
	}
}
