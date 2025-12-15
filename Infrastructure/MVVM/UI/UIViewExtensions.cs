using TMPro;

namespace Fusumity.MVVM.UI
{
	public static class UIViewExtensions
	{
		public static void Bind(this TMP_Text label, ILabelViewModel viewModel)
		{
			viewModel.Bind((x) => label.text = x);
		}

		public static void Release(this TMP_Text label, ILabelViewModel viewModel)
		{
			viewModel.Release();
		}
	}
}
