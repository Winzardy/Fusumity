using Fusumity.MVVM;
using Fusumity.MVVM.UI;
using Game.UI;
using System;

namespace UI
{
	public class UIAnemicStatefulButtonViewModel : IStatefulButtonViewModel
	{
		private LabelViewModel _label;
		private bool? _interactable;
		private string _style;

		public ILabelViewModel Label { get => _label; }

		public string Style { get => _style; set { _style = value; StyleChanged?.Invoke(); } }
		public bool? Interactable { get => _interactable; set { _interactable = value; InteractableChanged?.Invoke(); } }

		public UISpriteInfo Icon { get; set; }
		public ILabeledIconViewModel LabeledIcon { get; set; }
		public IStatefulViewModel Indicator { get; set; }

		public string LabelText { get => (_label ??= new LabelViewModel()).Value; set => (_label ??= new LabelViewModel()).Value = value; }
		public Action ClickAction { get; set; }

		public event Action StyleChanged;
		public event Action InteractableChanged;

		public void Click()
		{
			ClickAction?.Invoke();
		}
	}
}
