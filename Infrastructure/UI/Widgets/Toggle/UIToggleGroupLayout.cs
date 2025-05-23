using UnityEngine;

namespace UI
{
	public class UIToggleGroupLayout : UIBaseLayout
	{
		public bool useAnimations;

		/// <summary>
		/// <see cref="UIToggleButtonLayout"/>
		/// </summary>
		[Space]
		public UIGroupLayout group;

		public override bool UseLayoutAnimations => useAnimations;
	}
}
