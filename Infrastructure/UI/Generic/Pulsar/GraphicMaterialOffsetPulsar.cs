using UnityEngine;
using UnityEngine.UI;

namespace UI
{
	public class GraphicMaterialOffsetPulsar : Pulsar
	{
		[SerializeField]
		private Graphic _graphic;

		[Space]
		public bool x;

		public bool y;

		private bool _material;

		protected override void OnUpdate(float normalizedValue)
		{
			if (!_material)
			{
				_graphic.material = Instantiate(_graphic.material);
				_material = true;
			}

			_graphic.material.mainTextureOffset = new Vector2(x ? normalizedValue : 0, y ? normalizedValue : 0);
		}

		private void Reset()
		{
			_graphic = GetComponent<Graphic>();
		}
	}
}
