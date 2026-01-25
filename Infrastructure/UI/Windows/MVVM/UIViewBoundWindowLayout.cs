using Sirenix.OdinInspector;

namespace UI.Windows
{
	public abstract class UIViewBoundWindowLayout<TLayout> : UIBaseWindowLayout
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
