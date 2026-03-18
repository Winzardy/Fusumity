using System;
using JetBrains.Annotations;

namespace UI
{
	public interface IWidgetAnimator<in TLayout> : IUIAnimator<TLayout>
		where TLayout : UIBaseLayout
	{
		public void Setup(UIWidget widget);
	}

	public abstract class BaseWidgetAnimator<TLayout, TWidget> : BaseWidgetAnimator<TLayout>
		where TLayout : UIBaseLayout
		where TWidget : class, IWidget
	{
		protected TWidget _widget;

		protected override void OnSetup(UIWidget rawWidget)
		{
			if (rawWidget is not TWidget widget)
				throw new Exception($"Invalid widget type [ {typeof(TWidget)} ]");

			_widget    = widget;
			_rawWidget = rawWidget;
		}

		public override void Dispose()
		{
			_widget = null;
			base.Dispose();
		}
	}

	public abstract class BaseWidgetAnimator<TLayout> : UIAnimator<TLayout>, IWidgetAnimator<TLayout>
		where TLayout : UIBaseLayout
	{
		[CanBeNull]
		protected IWidget _rawWidget;

		void IWidgetAnimator<TLayout>.Setup(UIWidget widget)
		{
			OnSetup(widget);
		}

		protected virtual void OnSetup(UIWidget rawWidget)
		{
			_rawWidget = rawWidget;
		}

		public override void Dispose()
		{
			base.Dispose();

			_rawWidget = null;
		}

		protected override void SetVisible(bool active)
		{
			if (_rawWidget != null)
			{
				_rawWidget.SetVisible(active);
				return;
			}

			base.SetVisible(active);
		}
	}
}
