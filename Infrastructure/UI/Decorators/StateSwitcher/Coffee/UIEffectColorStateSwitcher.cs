using Coffee.UIEffects;
using UnityEngine;

namespace UI.Coffee
{
	public class UIEffectColorStateSwitcher : StateSwitcher<Color>
	{
		public UIEffect effect;

		protected override void OnStateSwitched(Color color)
		{
			effect.color = color;
		}
	}
}
