using TMPro;
using UI;

namespace Fusumity.MVVM.UI
{
	public static class UIViewExtensions
	{
		public static void Bind(this TMP_Text label, ILabelViewModel viewModel)
		{
			viewModel.Bind((x) => label.text = x);
		}

		public static void Unbind(this TMP_Text label, ILabelViewModel viewModel)
		{
			viewModel.Release();
		}

		public static void Bind(this UILabelLayout label, ILabelViewModel viewModel)
		{
			viewModel.Bind((x) => label.SetLabel(x));
		}

		public static void Unbind(this UILabelLayout label, ILabelViewModel viewModel)
		{
			viewModel.Release();
		}
	}
}
