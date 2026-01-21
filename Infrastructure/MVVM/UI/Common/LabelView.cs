using TMPro;
using UnityEngine;

namespace Fusumity.MVVM.UI
{
	public class LabelView : View<ILabelViewModel, TMP_Text>
	{
		public LabelView(TMP_Text layout) : base(layout)
		{
		}

		protected override void OnUpdate(ILabelViewModel viewModel)
		{
			_layout.Bind(ViewModel);
		}

		protected override void OnClear(ILabelViewModel viewModel)
		{
			_layout.Unbind(ViewModel);
		}

		protected override void OnNullViewModel()
		{
			_layout.text = "";
		}
	}
}
