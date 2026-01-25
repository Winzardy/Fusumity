using Sirenix.OdinInspector;

namespace UI.Screens
{
	public abstract class UIViewBoundScreenLayout<TLayout> : UIBaseScreenLayout
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
