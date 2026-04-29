using Coffee.UIEffects;
using UnityEngine;

namespace UI.Coffee
{
	public class UIEffectColorStateSwitcher : StateSwitcher<Color>
	{
		public UIEffect effect;

		public override Color Current { get => effect.color; set => OnStateSwitched(value); }

		protected override void OnStateSwitched(Color color)
		{
			effect.color = color;
		}
	}
}
