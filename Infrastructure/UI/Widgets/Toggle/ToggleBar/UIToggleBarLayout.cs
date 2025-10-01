using Fusumity.MVVM.UI;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
	public class UIToggleBarLayout : UIViewCollectionLayout<UIToggleButtonLayout>
	{
		[Space, Tooltip("Optional")]
		public Button back;
	}
}
