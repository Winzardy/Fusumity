using System;
using JetBrains.Annotations;
using TMPro;
using UI;

namespace Fusumity.MVVM.UI
{
	public static class UIViewExtensions
	{
		public static void Bind(this TMP_Text label, ILabelViewModel viewModel)
		{
			try
			{
				viewModel.Bind(x => label.text = x);
			}
			catch (Exception e)
			{
				GUIDebug.LogException(e, label);
				throw;
			}
		}

		public static void Unbind([CanBeNull] this TMP_Text _, ILabelViewModel viewModel)
		{
			viewModel.Release();
		}

		public static void Bind(this UILabelLayout layout, ILabelViewModel viewModel)
		{
			viewModel.Bind(layout.SetLabel);
		}

		public static void Unbind([CanBeNull] this UILabelLayout _, ILabelViewModel viewModel)
		{
			viewModel.Release();
		}
	}
}
