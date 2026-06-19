using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
	/// <summary>
	/// Считает и задаёт радиус скругления для 9-sliced <see cref="Image"/> (круг или скруглённый
	/// прямоугольник), управляя <see cref="Image.pixelsPerUnitMultiplier"/>.
	/// </summary>
	[RequireComponent(typeof(Image))]
	public class ImageSlicedCircleRadiusCalculator : MonoBehaviour
	{
#if UNITY_EDITOR
		[SerializeField, ReadOnly]
		private Image _image;

		[InlineButton(nameof(OnAutoClicked), "Auto")]
		public float CanvasReferencePixelPerUnit = 100;

		[ShowInInspector]
		[Tooltip("Радиус скругления в пикселях (как он рендерится при scale = 1). Only Editor")]
		[SuffixLabel("in pixels (only editor)", true)]
		public float Radius
		{
			get
			{
				if (!TryGetBorder(out var radiusInTexels))
					return 0;

				// Unity рисует бордюр 9-slice как
				//   border / (sprite.pixelsPerUnit / referencePixelsPerUnit * pixelsPerUnitMultiplier).
				// Для круга/скругления радиус угла равен размеру бордюра.
				return radiusInTexels * CanvasReferencePixelPerUnit /
					(_image.sprite.pixelsPerUnit * _image.pixelsPerUnitMultiplier);
			}
			set
			{
				if (value <= 0 || !TryGetBorder(out var radiusInTexels))
					return;

				UnityEditor.Undo.RecordObject(_image, "Set Sliced Radius");

				_image.pixelsPerUnitMultiplier = radiusInTexels * CanvasReferencePixelPerUnit /
					(_image.sprite.pixelsPerUnit * value);

				UnityEditor.EditorUtility.SetDirty(_image);
			}
		}

		/// <summary>
		/// Размер угла (бордюра 9-slice) в текселях спрайта. Берётся из <see cref="Sprite.border"/>,
		/// поэтому, в отличие от ширины текстуры, не зависит от упаковки спрайта в атлас.
		/// </summary>
		private bool TryGetBorder(out float radiusInTexels)
		{
			radiusInTexels = 0;

			if (!_image || !_image.sprite || _image.type != Image.Type.Sliced)
				return false;

			// border: x=left, y=bottom, z=right, w=top. Для симметричного скругления стороны равны.
			radiusInTexels = _image.sprite.border.x;
			return radiusInTexels > 0;
		}

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
