using Sapientia.Extensions;
using System;
using UI;
using UnityEngine;

namespace Game.UI
{
	public class UIAnemicLabeledIconViewModel : ILabeledIconViewModel
	{
		private UISpriteInfo _icon;
		private string _label;
		private string _labelStyle;
		private Color? _iconColor;
		private Color? _labelColor;

		public UISpriteInfo Icon
		{
			get => _icon;
			set
			{
				_icon = value;
				IconChanged?.Invoke();
			}
		}

		public string Label
		{
			get => _label;
			set
			{
				_label = value;
				LabelChanged?.Invoke();
			}
		}

		public Color? IconColor
		{
			get => _iconColor;
			set
			{
				_iconColor = value;
				IconColorChanged?.Invoke();
			}
		}

		public Color? LabelColor
		{
			get => _labelColor;
			set
			{
				_labelColor = value;
				LabelColorChanged?.Invoke();
			}
		}

		public string LabelStyle
		{
			get => _labelStyle;
			set
			{
				_labelStyle = value;
				LabelStyleChanged?.Invoke();
			}
		}

		public event Action LabelChanged;
		public event Action IconChanged;
		public event Action IconColorChanged;
		public event Action LabelColorChanged;
		public event Action LabelStyleChanged;

		public event Action LabelClicked;
		public event Action IconClicked;

		public void Clear()
		{
			if (!Icon.IsEmptyOrInvalid())
				Icon = default;

			if (!Label.IsNullOrEmpty())
				Label = default;

			if (_iconColor.HasValue)
				IconColor = default;

			if (LabelColor.HasValue)
				LabelColor = default;

			if (!LabelStyle.IsNullOrEmpty())
				LabelStyle = default;
		}

		/// <summary>
		/// Resets silently to a default state,
		/// does not produce events (view will not react).
		/// </summary>
		public void Reset()
		{
			_icon = default;
			_label = default;
			_iconColor = default;
			_labelColor = default;
			_labelStyle = default;
		}

		public void LabelClick()
		{
			LabelClicked?.Invoke();
		}

		public void IconClick()
		{
			IconClicked?.Invoke();
		}
	}
}
