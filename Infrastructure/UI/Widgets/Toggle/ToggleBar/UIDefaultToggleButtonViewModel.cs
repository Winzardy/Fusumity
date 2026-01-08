using AssetManagement;
using System;
using UnityEngine;

namespace UI
{
	public class UIDefaultToggleButtonViewModel : IToggleButtonViewModel
	{
		public int Index { get; }
		public IAssetReferenceEntry<Sprite> Icon { get; private set; }
		public string Label { get; private set; }

		public bool IsToggled { get; private set; }
		public string Style { get; private set; }

		public event Action ToggleStateChanged;
		public event Action IconChanged;
		public event Action LabelChanged;
		public event Action StyleChanged;

		public event Action<UIDefaultToggleButtonViewModel> Clicked;

		public UIDefaultToggleButtonViewModel(int index, bool isToggled)
		{
			Index = index;
			IsToggled = isToggled;
		}

		public UIDefaultToggleButtonViewModel(int index, bool isToggled, IAssetReferenceEntry<Sprite> icon, string label) : this(index, isToggled)
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
