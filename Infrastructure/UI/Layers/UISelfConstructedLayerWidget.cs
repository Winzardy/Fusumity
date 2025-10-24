using UnityEngine;

namespace UI
{
	/// - Верстка (layout) находится в корне слоя (<see cref="Layer"/>)
	public abstract class UISelfConstructedLayerWidget<TLayout> : UISelfConstructedWidget<TLayout>
		where TLayout : UIBaseLayout
	{
		protected new abstract string Layer { get; }
		protected sealed override RectTransform LayerRectTransform => UIDispatcher.GetLayer(Layer).rectTransform;
		protected override string LayoutPrefixName => $"[{Layer}] ";

		protected override void OnChildWidgetRegistered(UIWidget child)
		{
			child.SetLayer(Layer);
		}
	}
}
