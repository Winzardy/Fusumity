using System;
using TMPro;
using UI;

namespace Fusumity.MVVM.UI
{
	public static class UIViewExtensions
	{
		public static void Bind(this TMP_Text label, ILabelViewModel viewModel)
		{
			viewModel.Bind(x => label.text = x);
		}

		public static void Bind(this TMP_Text label, ILabelViewModel viewModel, Action<string> labelTextSetter)
		{
			//TODO: remove
			if (label == null)
				return;

			if (viewModel == null)
				return;

			viewModel.Bind(labelTextSetter);
		}

		public static void Unbind(this TMP_Text label, ILabelViewModel viewModel)
		{
			viewModel.Release();
		}

		public static void Bind(this UILabelLayout layout, ILabelViewModel viewModel)
		{
			viewModel.Bind(layout.SetLabel);
		}

		public static void Unbind(this UILabelLayout layout, ILabelViewModel viewModel)
		{
			viewModel.Release();
		}
	}
}
