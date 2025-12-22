using Sapientia.Collections;
using System;
using System.Collections.Generic;

namespace UI
{
	/// <summary>
	/// One toggle can be active at a time.
	/// </summary>
	public abstract class UISingularToggleBarViewModel<TSourceData, TButtonViewModel> : UIDefaultToggleBarViewModel<TSourceData, TButtonViewModel>
		where TButtonViewModel : UIDefaultToggleButtonViewModel
	{
		public TButtonViewModel SelectedButton { get; private set; }

		public event Action<TButtonViewModel> SelectionChanged;

		public UISingularToggleBarViewModel() :
			base()
		{
		}

		public UISingularToggleBarViewModel(IList<TSourceData> sourceData, int selectedIndex) :
			base(sourceData, selectedIndex)
		{
		}

		protected override void OnButtonCreated(TButtonViewModel button)
		{
			if (button.IsToggled)
			{
				SelectedButton = button;
			}
		}

		protected override void OnButtonClicked(TButtonViewModel button)
		{
			if (button == SelectedButton)
				return;

			SelectedButton?.SetToggled(false);
			button.SetToggled(true);

			SelectedButton = button;

			OnButtonSelected(button);
			SelectionChanged?.Invoke(button);
		}

		protected virtual void OnButtonSelected(TButtonViewModel button)
		{
		}

		public void Select(int index)
		{
			if (!_buttons.WithinBounds(index))
				return;

			var button = _buttons[index];
			OnButtonClicked(button);
		}

		public void Reset(int defaultIndex = 0)
		{
			SelectedButton?.SetToggled(false);
			SelectedButton = default;

			if (!_buttons.WithinBounds(defaultIndex))
				return;

			SelectedButton = _buttons[defaultIndex];
			SelectedButton.SetToggled(true);
			OnButtonSelected(SelectedButton);
		}
	}
}
