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

		[Sirenix.OdinInspector.PropertySpace(0, 10)]
		public Button close;

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
