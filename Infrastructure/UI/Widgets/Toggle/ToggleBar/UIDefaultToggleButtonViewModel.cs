using AssetManagement;
using System;
using UnityEngine;

namespace UI
{
	public class UIDefaultToggleButtonViewModel : IToggleButtonViewModel
	{
		public int Index { get; private set; }
		public IAssetReferenceEntry<Sprite> Icon { get; private set; }
		public string Label { get; private set; }

		public bool IsToggled { get; private set; }
		public string Style { get; private set; }

		public event Action<bool> ToggleStateChanged;
		public event Action IconChanged;
		public event Action LabelChanged;
		public event Action StyleChanged;

		public event Action<UIDefaultToggleButtonViewModel> Clicked;

		public UIDefaultToggleButtonViewModel() : this(false)
		{
		}

		public UIDefaultToggleButtonViewModel(bool isToggled)
		{
			IsToggled = isToggled;
		}

		public UIDefaultToggleButtonViewModel(int index, bool isToggled) : this(isToggled)
		{
			Index = index;
		}

		public UIDefaultToggleButtonViewModel(int index, bool isToggled, IAssetReferenceEntry<Sprite> icon, string label) : this(index,
			isToggled)
		{
			Icon = icon;
			Label = label;
		}

		public void Click() => Clicked?.Invoke(this);

		public void SetIcon(IAssetReferenceEntry<Sprite> icon)
		{
			Icon = icon;
			IconChanged?.Invoke();
		}

		public void SetLabel(string label)
		{
			Label = label;
			LabelChanged?.Invoke();
		}

		public void SetToggled(bool toggled, bool immediate = false)
		{
			IsToggled = toggled;
			ToggleStateChanged?.Invoke(immediate);
		}

		public void SetStyle(string style)
		{
			Style = style;
			StyleChanged?.Invoke();
		}

		public void SetAvailable(bool available)
		{
			var style = available ? ToggleButtonStyle.UNLOCKED : ToggleButtonStyle.LOCKED;

			SetStyle(style);
		}

		public void SetIndex(int index)
		{
			Index = index;
		}
	}
}
