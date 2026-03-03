using Sirenix.OdinInspector;

namespace UI.MVVM
{
	public abstract class UIViewBoundWidgetLayout<TLayout> : UIBaseLayout
		where TLayout : UIBaseLayout
	{
		public bool useLayoutAnimations;

		[BoxGroup]
		public TLayout view;

		public override bool UseLayoutAnimations { get => useLayoutAnimations; }

		protected override void Reset()
		{
			base.Reset();

			if (!TryGetComponent(out view))
			{
				view = gameObject.AddComponent<TLayout>();
			}
		}
	}
}
