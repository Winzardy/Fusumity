using UnityEngine;
using UnityEngine.UI;

namespace UI
{
	public class GraphicsColorPulsar : Pulsar
	{
		[SerializeField]
		private Graphic[] _graphics;

		public Color start;
		public Color end;

		protected override void OnUpdate(float normalizedValue)
		{
			foreach (var graphic in _graphics)
				graphic.color = Color.Lerp(start, end, normalizedValue);
		}
	}
}
