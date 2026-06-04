using DG.Tweening;
using Fusumity.Utility;
using Sirenix.OdinInspector;
using System;
using UnityEngine;
using UnityEngine.UI;

namespace ZenoTween.Participant.Tweens
{
	[Serializable]
	[TypeRegistryItem(Icon = SdfIconType.Boombox, CategoryPath = CATEGORY_PATH)]
	public class ShakeAndExplodeTween : AnimationTween
	{
		private Vector2? _originalPos;
		private Vector3? _originalScale;

		public RectTransform rectTransform;
		public CanvasGroup canvasGroup;
		public Image flashImage;

		[Header("Shake")]
		public float shakeDuration = 1.5f;
		public float shakeStartStrength = 5f;
		public float shakeEndStrength = 30f;
		public int shakeStartVibrato = 5;
		public int shakeEndVibrato = 40;
		public int shakeSteps = 6;

		[Header("Explode")]
		public float popScale = 2.5f;
		public float popDuration = 0.15f;
		public float fadeDuration = 0.2f;

		[Header("Flash")]
		public float flashShowDuration = 0.05f;
		public float flashDissipateDuration = 1f;

		protected override Tween Create()
		{
			_originalPos ??= rectTransform.anchoredPosition;
			_originalScale ??= rectTransform.localScale;

			rectTransform.anchoredPosition = _originalPos.Value;
			rectTransform.localScale = _originalScale.Value;
			canvasGroup.alpha = 1f;

			var sequence = DOTween.Sequence();

			float stepTime = shakeDuration / shakeSteps;
			for (int i = 0; i < shakeSteps; i++)
			{
				var t = (float)(i + 1) / shakeSteps;
				var strength = Mathf.Lerp(shakeStartStrength, shakeEndStrength, t);
				var vibrato = (int)Mathf.Lerp(shakeStartVibrato, shakeEndVibrato, t);

				sequence.Append(rectTransform.DOShakeAnchorPos(stepTime, strength, vibrato, fadeOut: false));
			}

			sequence.AppendCallback(() => rectTransform.anchoredPosition = _originalPos.Value);
			sequence.Append(rectTransform.DOScale(_originalScale.Value * popScale, popDuration).SetEase(Ease.OutBack));
			sequence.Join(canvasGroup.DOFade(0f, fadeDuration).SetDelay(popDuration * 0.25f));

			sequence.Join(flashImage.DOFade(1f, flashShowDuration).From(0));
			sequence.Append(flashImage.DOFade(0, flashDissipateDuration));

			sequence.AppendInterval(0.15f);
			sequence.AppendCallback(() =>
			{
				rectTransform.localScale = _originalScale.Value;
				rectTransform.anchoredPosition = _originalPos.Value;
				canvasGroup.alpha = 1f;
				flashImage.color = flashImage.color.WithAlpha(0);
			});

			return sequence;
		}
	}
}
