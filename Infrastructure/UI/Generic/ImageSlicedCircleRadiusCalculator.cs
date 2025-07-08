using System;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
	// TODO: Починить
	[Obsolete("Обманывает, надо починить")]
	[RequireComponent(typeof(Image))]
	public class ImageSlicedCircleRadiusCalculator : MonoBehaviour
	{
#if UNITY_EDITOR
		[SerializeField, ReadOnly]
		private Image _image;

		[InlineButton(nameof(OnAutoClicked), "Auto")]
		public float CanvasReferencePixelPerUnit = 100;

		[Tooltip("Временное решение чтобы привести к корректным значениям..."), Indent]
		public float calibrator = 0.04f;

		[PropertyOrder(10)]
		public bool forCircle = true;

		[PropertyOrder(11)]
		[HideIf(nameof(forCircle))]
		public float textureSizeMultiplier = 1;

		[ShowInInspector]
		[Tooltip("Only Editor")]
		[SuffixLabel("in pixels (only editor)", true)]
		public float Radius
		{
			get
			{
				if (!_image)
					return 0;

				if (_image.type != Image.Type.Sliced)
					return 0;

				return TextureSize / _image.pixelsPerUnitMultiplier /
					(2 * (_image.sprite.pixelsPerUnit / (CanvasReferencePixelPerUnit * calibrator)));
			}
			set
			{
				if (!_image)
					return;

				if (_image.type != Image.Type.Sliced)
					return;

				_image.pixelsPerUnitMultiplier = TextureSize /
					(2 * (_image.sprite.pixelsPerUnit / (CanvasReferencePixelPerUnit * calibrator)) * value);
			}
		}

		public float TextureSize => _image ? _image.sprite.texture.width / (forCircle ? 1 : textureSizeMultiplier) : 0;

		private void Reset() => _image = GetComponent<Image>();

		private void OnAutoClicked()
		{
			var canvas = gameObject.GetComponentInParent<Canvas>();

			if (canvas)
				CanvasReferencePixelPerUnit = canvas.referencePixelsPerUnit;
		}
#endif
	}
}
