using System;
using System.Collections.Generic;

namespace UI
{
	public class UIToggleDotsViewModel : IToggleBarViewModel
	{
		private List<UIDefaultToggleButtonViewModel> _dots = new List<UIDefaultToggleButtonViewModel>();
		public IEnumerable<IToggleButtonViewModel> Buttons { get => _dots; }

		public event Action ButtonsChanged;

		public UIToggleDotsViewModel(int count)
		{
			for (int i = 0; i < count; i++)
			{
				_dots.Add(new UIDefaultToggleButtonViewModel(i, i == 0));
			}
		}

		public void SetSelected(int index)
		{
			for (int i = 0; i < _dots.Count; i++)
			{
				var dot = _dots[i];
				dot.SetToggled(i == index);
			}
		}
	}
}
