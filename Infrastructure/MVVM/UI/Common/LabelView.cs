using Sapientia.Extensions;
using TMPro;

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

	// This is basically a strongly typed, non-generic reactive property.
	// I don't like the idea of reactive approach in general, but in the case of labels
	// it is more or less a necessity, especially if you can change game language on the fly.
	public interface ILabelViewModel : IBinding<string>
	{
		string Value { get; set; }

		bool IsEmpty { get => Value.IsNullOrEmpty(); }
	}
}
