using AssetManagement;
using System;
using UI;
using UnityEngine;

namespace Game.UI
{
	public class UIAnemicLabeledIconViewModel : ILabeledIconViewModel
	{
		private UISpriteInfo _icon;
		private string _label;
		private Color? _iconColor;
		private Color? _labelColor;

		public UISpriteInfo Icon { get => _icon; set { _icon = value; IconChanged?.Invoke(); } }
		public string Label { get => _label; set { _label = value; LabelChanged?.Invoke(); } }
		public Color? IconColor { get => _iconColor; set { _iconColor = value; IconColorChanged?.Invoke(); } }
		public Color? LabelColor { get => _labelColor; set { _labelColor = value; LabelColorChanged?.Invoke(); } }

		public event Action LabelChanged;
		public event Action IconChanged;
		public event Action IconColorChanged;
		public event Action LabelColorChanged;

		public void Reset()
		{
			_icon = default;
			_label = default;
			_iconColor = default;
			_labelColor = default;
		}
	}
}
