using UnityEngine;
using UnityEngine.UI;

namespace UI.Screens
{
	//TODO: не помню зачем я заставляю скрины иметь Canvas...
	[RequireComponent(typeof(Canvas))]
	public abstract class UIBaseScreenLayout : UIBaseContainerLayout
	{
		public override bool UseLayoutAnimations => useAnimations;
		public bool useAnimations = true;

		[HideInInspector]
		public Canvas canvas;

		[HideInInspector]
		public GraphicRaycaster raycaster;

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
