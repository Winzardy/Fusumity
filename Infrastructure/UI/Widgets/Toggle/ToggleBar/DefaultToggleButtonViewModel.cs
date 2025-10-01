using AssetManagement;
using System;

namespace UI
{
	public class DefaultToggleButtonViewModel : IToggleButtonViewModel
	{
		public int Index { get; }
		public IAssetReferenceEntry Icon { get; private set; }
		public string Label { get; private set; }

		public bool IsToggled { get; private set; }
		public string Style { get; private set; }

		public event Action ToggleStateChanged;
		public event Action IconChanged;
		public event Action LabelChanged;
		public event Action StyleChanged;

		public event Action<DefaultToggleButtonViewModel> Clicked;

		public DefaultToggleButtonViewModel(int index, bool isToggled, IAssetReferenceEntry icon, string label)
		{
			Index = index;
			Icon = icon;
			Label = label;
			IsToggled = isToggled;
		}

		public void Click() => Clicked?.Invoke(this);

		public void SetIcon(IAssetReferenceEntry icon)
		{
			Icon = icon;
			IconChanged?.Invoke();
		}

		public void SetLabel(string label)
		{
			Label = label;
			LabelChanged?.Invoke();
		}

		public void SetToggled(bool toggled)
		{
			IsToggled = toggled;
			ToggleStateChanged?.Invoke();
		}

		public void SetStyle(string style)
		{
			Style = style;
			StyleChanged?.Invoke();
		}

		public void SetAvailable(bool available)
		{
			var style = available ?
				ToggleButtonStyle.UNLOCKED :
				ToggleButtonStyle.LOCKED;

			SetStyle(style);
		}
	}
}
