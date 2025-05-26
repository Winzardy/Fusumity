using Sapientia.Pooling;
using UnityEngine;

namespace UI
{
	public class UIPool<TWidget, TLayout> : ObjectPool<TWidget>
		where TWidget : UIWidget<TLayout>, IWidget
		where TLayout : UIBaseLayout
	{
		public class Args
		{
			public UIWidget root;
			public TLayout template;
			public RectTransform parent = null;
			public bool autoActivation = true;
		}

		public UIPool(Args args) : this(args.root, args.template, args.parent, args.autoActivation)
		{
		}

		public UIPool(UIWidget root, TLayout template, RectTransform parent = null, bool autoActivation = true)
			: base(new Policy(root, template, parent, autoActivation))
		{
		}

		private class Policy : IObjectPoolPolicy<TWidget>
		{
			private readonly UIWidget _root;
			private readonly TLayout _template;
			private readonly RectTransform _parent;
			private readonly bool _autoActivation;

			public Policy(UIWidget root, TLayout template, RectTransform parent = null, bool autoActivation = true)
			{
				_autoActivation = autoActivation;
				_root = root;
				_template = template;
				_parent = parent != null ? parent : root.Root;
			}

			public TWidget Create()
			{
				var widget = UIFactory.CreateWidget<TWidget>();
				var layout = UIFactory.CreateLayout(_template, _parent);
				widget.SetLayer(_root.Layer);
				widget.SetupLayout(layout);
				return widget;
			}

			public void OnGet(TWidget widget)
				=> _root.RegisterChildWidget(widget, _autoActivation);

			public void OnRelease(TWidget widget)
			{
				_root.UnregisterChildWidget(widget);
				widget.Reset();

				if (!_autoActivation)
					return;

				widget.SetActive(false, true);
			}

			public void OnDispose(TWidget widget)
			{
				widget.Dispose();
			}
		}
	}
}
