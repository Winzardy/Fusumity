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

		[NotNull] public ILabelViewModel Label { get; set; } = new LabelViewModel();

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

		[NotNull] public ILabelViewModel SubLabel { get; } = new LabelViewModel();

		public string LabelStyle
		{
			get => _labelStyle;
			set
			{
				_labelStyle = value;
				LabelStyleChanged?.Invoke();
			}
		}

		public string SubLabelStyle
		{
			get => _subLabelStyle;
			set
			{
				_subLabelStyle = value;
				SubLabelStyleChanged?.Invoke();
			}
		}

		public event Action IconChanged;
		public event Action IconColorChanged;
		public event Action LabelColorChanged;

		public event Action LabelStyleChanged;
		public event Action SubLabelStyleChanged;

		public UIAnemicLabeledIconViewModel(string label)
		{
			Label!.Value = label;
		}

		public UIAnemicLabeledIconViewModel(string label, string subLabel) : this(label)
		{
			SubLabel!.Value = subLabel;
		}

		public UIAnemicLabeledIconViewModel()
		{
		}

		public void Clear()
		{
			if (!Icon.IsEmptyOrInvalid())
				Icon = default;

			Label.ClearSafe();
			SubLabel.ClearSafe();

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

			Label.ResetSafe();
			SubLabel.ResetSafe();
		}
	}
}
