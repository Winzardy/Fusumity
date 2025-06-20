using Fusumity.Utility;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
	public class GraphicAlphaPulsar : Pulsar
	{
		[SerializeField]
		private Graphic _graphic;

		protected override void OnUpdate(float normalizedValue)
			=> _graphic.color = _graphic.color.WithAlpha(normalizedValue);

		private void Reset() => _graphic = GetComponent<Graphic>();
	}
}
