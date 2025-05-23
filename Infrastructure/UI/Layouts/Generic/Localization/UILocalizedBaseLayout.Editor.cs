using System;
using Localizations;
using Sapientia.Extensions;

namespace UI
{
	public abstract partial class UILocalizedBaseLayout
	{
		//TODO: идея пришла в голову сделать серилизуемый словарь для тегов или аргументов которые можно просто получить
		//нужно только потом в виджете будет дособрать если требуется
#if UNITY_EDITOR
		[NonSerialized]
		private string _languageEditor;

		public string languageEditor
		{
			get
			{
				if (_languageEditor.IsNullOrEmpty())
					return Localization.CurrentLanguageEditor;

				return _languageEditor;
			}
			set
			{
				_languageEditor = value;
				OnValidate();
			}
		}

		protected internal override void OnValidate()
		{
			base.OnValidate();

			if (Placeholder && locInfo.enable)
				Placeholder.text = Localization.GetEditor(locInfo, _languageEditor.IsNullOrEmpty() ? null : _languageEditor);
		}
#endif
	}
}
