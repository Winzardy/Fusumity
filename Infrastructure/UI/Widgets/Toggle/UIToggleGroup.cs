using System;
using System.Collections;
using System.Collections.Generic;

namespace UI
{
	/// <summary>
	/// Дефолтная обычная реализация, для сложных кейсов есть Generic)
	/// </summary>
	public class UIToggleGroup : UIToggleGroup<UIToggleWidget, UIToggleButtonLayout, UIToggleWidget.Args>
	{
	}

	public struct UIToggleGroupArgs<TToggleWidgetArgs>
		where TToggleWidgetArgs : struct, IToggleArgs
	{
		public TToggleWidgetArgs[] Toggles { get; set; }

		/// <summary>
		/// Если не назначен, устанавливается <see cref="SelectionParameters.Single"/>
		/// </summary>
		public SelectionParameters? SelectionParameters { get; set; }

		public int[] PrioritySelectionItemIndexes { get; set; }

		public bool NotAutoSelectOnClick { get; set; }
	}

	//TODO: выглядит как говно, попробовать избавиться позже)
	public class UIToggleGroup<TToggleWidget, TToggleLayout, TToggleWidgetArgs> : UIWidget<UIToggleGroupLayout,
		UIToggleGroupArgs<TToggleWidgetArgs>>, IEnumerable<TToggleWidget>
		where TToggleWidget : UIToggleWidget<TToggleLayout, TToggleWidgetArgs>
		where TToggleLayout : UIToggleButtonLayout
		where TToggleWidgetArgs : struct, IToggleArgs
	{
		public delegate void ToggleDelegate(TToggleWidget widget, int index, bool selected, bool immediate);

		private Selection<TToggleWidgetArgs> _selection;
		private UIGroup<TToggleWidget, TToggleLayout, TToggleWidgetArgs> _group;

		public int this[TToggleWidget widget] => _group[widget];

		public LinkedList<int> SelectedIndexes => _selection.SelectedElements;

		public event ToggleDelegate Toggled;
		public event Action<TToggleWidget, int> Clicked;
		public event Action<TToggleWidget> Registered;
		public event Action<TToggleWidget> Unregistered;

		protected override void OnInitialized()
		{
			_selection = new Selection<TToggleWidgetArgs>(OnSelected);
		}

		private protected override void OnDisposeInternal()
		{
			base.OnDisposeInternal();

			_selection?.Dispose();
		}

		protected override void OnLayoutInstalled()
		{
			CreateWidget(out _group, _layout.group, true);
			_group.Registered += OnRegistered;
			_group.Unregistered += OnUnregistered;
		}

		protected override void OnLayoutCleared()
		{
			_group.Registered -= OnRegistered;
			_group.Unregistered -= OnUnregistered;
		}

		protected override void OnShow(ref UIToggleGroupArgs<TToggleWidgetArgs> _)
		{
			_group.Update(_args.Toggles);

			_selection.SetParameters(_args.SelectionParameters ?? SelectionParameters.Single);
			_selection.Bind(_args.Toggles, _args.PrioritySelectionItemIndexes);
		}

		private void OnSelected(int index, bool selected, bool immediate)
		{
			if (!_group.TryGet(index, out var widget))
				return;

			_args.Toggles[index].IsOn = selected;
			widget.Toggle(selected, immediate);
			Toggled?.Invoke(widget, index, selected, immediate);
		}

		public bool Select(int index, bool immediate = false) => _selection.TrySelect(index, immediate);
		public bool Deselect(int index, bool immediate = false) => _selection.TryDeselect(index, immediate);

		private void OnRegistered(TToggleWidget item)
		{
			item.SetOverrideActionOnClick(OnToggled);
			Registered?.Invoke(item);
		}

		private void OnUnregistered(TToggleWidget item)
		{
			Unregistered?.Invoke(item);
		}

		private void OnToggled(IButtonWidget widget)
		{
			var toggle = (TToggleWidget) widget;
			var index = _group[toggle];

			if (!_args.NotAutoSelectOnClick)
			{
				if (_selection.IsSelected(index))
					_selection.TryDeselect(index);
				else
					_selection.TrySelect(index);
			}

			toggle.args.Action?.Invoke();
			Clicked?.Invoke(toggle, index);
		}

		public IEnumerator<TToggleWidget> GetEnumerator() => _group.GetEnumerator();

		IEnumerator IEnumerable.GetEnumerator() => _group.GetEnumerator();
	}
}
