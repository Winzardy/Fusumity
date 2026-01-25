using Sirenix.OdinInspector;

namespace UI.Popups
{
	public abstract class UIViewBoundPopupLayout<TLayout> : UIBasePopupLayout
		where TLayout : UIBaseLayout
	{
		[BoxGroup]
		public TLayout view;

		protected override void Reset()
		{
			base.Reset();

			if (!TryGetComponent(out view))
				view = gameObject.AddComponent<TLayout>();
		}
	}
}
