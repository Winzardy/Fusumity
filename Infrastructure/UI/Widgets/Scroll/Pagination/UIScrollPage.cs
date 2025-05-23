using System;
using Sapientia.Collections;

namespace UI.Scroll.Pagination
{
	public abstract class UIScrollPage<TLayout, TItemArgs> : UIScrollListItem<TLayout, UIScrollPageArgs<TItemArgs>>
		where TLayout : UIScrollPageLayout
		where TItemArgs : struct, IScrollListItemArgs
	{
		private UIToggleWidget _toggle;

		public event Action<UIScrollPageArgs<TItemArgs>> Clicked;

		/// <summary>
		/// Для кейсов когда нам нужно обрабатывать в OnShow/OnHide null
		/// </summary>
		protected virtual bool UseEmptyArgs => false;

		public sealed override void SetupLayout(TLayout layout)
		{
			CreateWidget(out _toggle, layout.toggle, true);
			_toggle.SetOverrideActionOnClick(OnClicked);

			base.SetupLayout(layout);
		}

		public void SetSelected(bool selected, bool immediate = false) => _toggle.Toggle(selected, immediate);

		protected sealed override void OnShow(ref UIScrollPageArgs<TItemArgs> args)
		{
			if (args.IsEmpty && !UseEmptyArgs)
			{
				Reset(false);
				return;
			}

			SetSelected(args.selected, true);
			OnShow(args.Reference);
		}

		protected sealed override void OnHide(ref UIScrollPageArgs<TItemArgs> args)
			=> OnHide(args.Reference);

		protected abstract void OnShow(in ArrayReference<TItemArgs> reference);

		protected virtual void OnHide(in ArrayReference<TItemArgs> reference)
		{
		}

		private void OnClicked(IButtonWidget _) => Clicked?.Invoke(args);
	}
}
