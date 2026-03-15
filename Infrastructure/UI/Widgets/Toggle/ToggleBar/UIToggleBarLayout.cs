using ActionBusSystem;
using Fusumity.Attributes.Odin;
using Fusumity.MVVM.UI;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
	public class UIToggleBarLayout : UIViewCollectionLayout<UIToggleButtonLayout>
	{
		[Space, Tooltip("Optional"), OptionalSuffixLabel]
		public Button back;

		[Indent]
		[ConstDropdown(typeof(ActionBusElementType))]
		public string backButtonUniqueId;

		[Indent]
		[ConstDropdown(typeof(ActionBusGroupType))]
		public string backButtonGroupId;
	}
}
