using Sapientia.Collections;
using System;
using System.Collections.Generic;

namespace UI
{
	/// <summary>
	/// One toggle can be active at a time.
	/// </summary>
	public abstract class SingularToggleBarViewModel<TSourceData, TButtonViewModel> : DefaultToggleBarViewModel<TSourceData, TButtonViewModel>
		where TButtonViewModel : DefaultToggleButtonViewModel
	{
		public TButtonViewModel SelectedButton { get; private set; }

		public event Action<TButtonViewModel> SelectionChanged;

		public SingularToggleBarViewModel() :
			base()
		{
		}

		public SingularToggleBarViewModel(IList<TSourceData> sourceData, int selectedIndex) :
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
			if (!_buttons.WithinRange(index))
				return;

			var button = _buttons[index];
			OnButtonClicked(button);
		}
	}
}
