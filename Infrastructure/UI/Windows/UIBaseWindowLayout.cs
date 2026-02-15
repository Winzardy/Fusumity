using ActionBusSystem;
using Fusumity.Attributes.Odin;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
	public class UIBaseWindowLayout : UIBaseContainerLayout
	{
		public bool useAnimations = true;
		public override bool UseLayoutAnimations => useAnimations;

		[HideInInspector]
		public Canvas canvas;

		[HideInInspector]
		public GraphicRaycaster raycaster;

		[BoxGroup("Close", showLabel: false)]
		public Button close;
		[BoxGroup("Close", showLabel: false)]
		[ConstDropdown(typeof(ActionBusElementType))] // consider it a hack, since close is a default unity button.
		public string uId;
		[BoxGroup("Close", showLabel: false)]
		[ConstDropdown(typeof(ActionBusGroupType))]
		public string groupId;

		//Защита от дурака)
		//Вообще лучше максимально избегать кода завязаного на жизненый цикл юнити (unity lifecycle)
		private void Awake()
		{
			TryGetComponent(out canvas);
			TryGetComponent(out raycaster);
		}

		protected override void Reset()
		{
			base.Reset();

			TryGetComponent(out canvas);
			TryGetComponent(out raycaster);
		}
	}
}
