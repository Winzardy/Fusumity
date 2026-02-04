using System;

namespace Fusumity.MVVM.UI
{
	public class StylizedLabelView : View<IStylizedLabelViewModel, StylizedLabelLayout>
	{
		public StylizedLabelView(StylizedLabelLayout layout) : base(layout)
		{
		}

		protected override void OnUpdate(IStylizedLabelViewModel viewModel)
		{
			_layout.label.Bind(viewModel);

			UpdateStyle();
			viewModel.StyleChanged += HandleStyleChanged;
		}

		protected override void OnClear(IStylizedLabelViewModel viewModel)
		{
			_layout.label.Unbind(viewModel);

			viewModel.StyleChanged -= HandleStyleChanged;
		}

		protected override void OnNullViewModel()
		{
			_layout.label.text = "";
		}

		private void UpdateStyle()
		{
			_layout.styleSwitcher.Switch(ViewModel?.Style ?? string.Empty);
		}

		private void HandleStyleChanged()
		{
			UpdateStyle();
		}
	}

	public interface IStylizedLabelViewModel : ILabelViewModel
	{
		string Style { get; set; }

		event Action StyleChanged;
	}
}
