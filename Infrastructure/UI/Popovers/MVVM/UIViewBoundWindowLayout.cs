using Sirenix.OdinInspector;

namespace UI.Popovers
{
	public abstract class UIViewBoundScreenLayout<TLayout> : UIBasePopoverLayout
		where TLayout : UIBaseLayout
	{
		[BoxGroup]
		public TLayout view;

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
