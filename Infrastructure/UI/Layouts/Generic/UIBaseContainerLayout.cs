using Fusumity.Utility;
using UnityEngine;

namespace UI
{
	//От класса в основном наследуются верстки Window, Popup, Screens
	//Сомневаюсь в названии...
	//Может быть UIModelRectLayout...оставлю так
	public abstract class UIBaseContainerLayout : UIBaseCanvasGroupLayout
	{
		private const string NAME = "Container";

		[Tooltip("Используется в анимации, чтобы отделить root и контейнер, можно переназначить. " +
			"Reset создает контейнер только в случае если он не назначен")]
		public RectTransform container;

		#region Blend Mode For Layout Animations

		[Tooltip("Режим смешивания для анимации открытия. С помощью выбранного режима можно либо дополнить (<b>"+nameof(AnimationSequenceBlendMode.Additive)+"</b>) анимацию виджета, либо переопдределить (<b>"+nameof(AnimationSequenceBlendMode.Override)+"</b>)")]
		public AnimationSequenceBlendMode openingBlendMode;

		public override AnimationSequenceBlendMode OpeningBlendMode => openingBlendMode;

		[Tooltip("Режим смешивания для анимации закрытия. С помощью выбранного режима можно либо дополнить (<b>"+nameof(AnimationSequenceBlendMode.Additive)+"</b>) анимацию виджета, либо переопдределить (<b>"+nameof(AnimationSequenceBlendMode.Override)+"</b>)")]
		public AnimationSequenceBlendMode closingBlendMode;

		public override AnimationSequenceBlendMode ClosingBlendMode => closingBlendMode;

		#endregion

		protected override void Reset()
		{
			base.Reset();

			if (container)
				return;

			container = rectTransform.FirstOrDefault(NAME);

			if (container)
				return;

			container = rectTransform.CreateChild(NAME);
		}
	}
}
