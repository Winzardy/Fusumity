using System;
using TMPro;
using UI;

namespace Fusumity.MVVM.UI
{
	public static class UIViewExtensions
	{
		public static void Bind(this TMP_Text label, ILabelViewModel viewModel)
		{
			Bind(label, viewModel, x => label.text = x);
		}

		public static void Bind(this TMP_Text label, ILabelViewModel viewModel, Action<string> labelTextSetter)
		{
			viewModel.Bind(labelTextSetter);
		}

		public static void BindSafe(this TMP_Text label, ILabelViewModel viewModel, Action<string> labelTextSetter)
		{
			if (!label)
				return;

			if (viewModel == null)
				return;

			label.Bind(viewModel, labelTextSetter);
		}

		public static void BindSafe(this TMP_Text label, ILabelViewModel viewModel)
		{
			if (!label)
				return;

			if (viewModel == null)
				return;

			label.Bind(viewModel);
		}

		public static void Unbind(this TMP_Text label, ILabelViewModel viewModel)
		{
			viewModel.Release();
		}

		public static void UnbindSafe(this TMP_Text label, ILabelViewModel viewModel)
		{
			if (!label)
				return;
			if (viewModel == null)
				return;
			label.Unbind(viewModel);
		}

		public static void Bind(this UILabelLayout label, ILabelViewModel viewModel)
		{
			viewModel.Bind(label.SetLabel);
		}

		public static void Unbind(this UILabelLayout label, ILabelViewModel viewModel)
		{
			viewModel.Release();
		}
	}
}
