using Coffee.UIEffects;
using UnityEngine;

namespace UI.Coffee
{
	public class UIEffectGradientStateSwitcher : StateSwitcher<Gradient>
	{
		public UIEffect effect;

		protected override void OnStateSwitched(Gradient gradient)
		{
			effect.SetGradientKeys(gradient.colorKeys, gradient.alphaKeys, gradient.mode);
		}
	}
}
