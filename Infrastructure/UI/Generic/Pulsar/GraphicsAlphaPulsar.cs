using Fusumity.Utility;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
	public class GraphicsAlphaPulsar : Pulsar
	{
		[SerializeField]
		private Graphic[] _graphics;

		protected override void OnUpdate(float normalizedValue)
		{
			foreach (var graphic in _graphics)
				graphic.color = graphic.color.WithAlpha(normalizedValue);
		}
	}
}
