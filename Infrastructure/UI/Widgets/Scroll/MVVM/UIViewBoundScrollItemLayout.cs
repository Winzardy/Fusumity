using Sapientia;
using Sirenix.OdinInspector;

namespace UI.Scroll
{
	public abstract class UIViewBoundScrollItemLayout : UIScrollItemLayout
	{
		[CanBeEmpty]
		public ActionBusButtonScheme button;
	}

	public abstract class UIViewBoundScrollItemLayout<TLayout> : UIViewBoundScrollItemLayout
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
