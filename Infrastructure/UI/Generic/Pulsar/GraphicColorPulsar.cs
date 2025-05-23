using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
	public interface IColorFlicker
	{
		public void SetOriginColor(Color color);
	}

	public class GraphicColorPulsar : Pulsar, IColorFlicker
	{
		[SerializeField]
		private Graphic _graphic;

		public bool useCustomStartColor;

		[ShowIf(nameof(useCustomStartColor))]
		public Color start;

		[ShowInInspector, HideIf(nameof(useCustomStartColor))]
		public Color origin => _originColor ?? _graphic.color;

		public Color end;

		private Color? _originColor;

		protected override void OnDisabled() => _originColor = null;

		protected override void OnUpdate(float normalizedValue)
		{
			if (!useCustomStartColor)
				_originColor ??= _graphic.color;

			_graphic.color = Color.Lerp(useCustomStartColor ? start : _originColor!.Value, end, normalizedValue);
		}

		private void Reset() => _graphic = GetComponent<Graphic>();

		public void SetOriginColor(Color color) => _originColor = color;
	}
}
