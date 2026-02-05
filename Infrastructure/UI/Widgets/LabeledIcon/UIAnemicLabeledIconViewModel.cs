using System;
using Fusumity.MVVM.UI;
using JetBrains.Annotations;
using UI;
using UnityEngine;

namespace Game.UI
{
	public class UIAnemicLabeledIconViewModel : ILabeledIconViewModel
	{
		private UISpriteInfo _icon;
		private Color? _iconColor;
		private Color? _labelColor;

		private string _label;

		private string _labelStyle;
		private string _subLabelStyle;

		public UISpriteInfo Icon
		{
			get => _icon;
			set
			{
				_icon = value;
				IconChanged?.Invoke();
			}
		}

		[NotNull]
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

		public event Action IconChanged;
		public event Action IconColorChanged;
		public event Action LabelColorChanged;

		public event Action LabelStyleChanged;
		public event Action LabelChanged;

		public UIAnemicLabeledIconViewModel(string label)
		{
			_label = label;
		}

		public UIAnemicLabeledIconViewModel()
		{
		}

		public void Clear()
		{
			if (!Icon.IsEmptyOrInvalid())
				Icon = default;

			if (_iconColor.HasValue)
				IconColor = default;

			if (LabelColor.HasValue)
				LabelColor = default;
		}

		/// <summary>
		/// Resets silently to a default state,
		/// does not produce events (view will not react).
		/// </summary>
		public void Reset()
		{
			_icon = default;
			_iconColor = default;
			_labelColor = default;
			_label = default;
		}
	}
}
