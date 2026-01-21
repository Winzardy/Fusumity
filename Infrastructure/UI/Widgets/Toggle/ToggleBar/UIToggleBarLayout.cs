using Fusumity.MVVM.UI;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
	public class UIToggleBarLayout : UIViewCollectionLayout<UIToggleButtonLayout>
	{
		[Space, Tooltip("Optional"), SuffixLabel("Optional           ", true)]
		public Button back;
	}
}
