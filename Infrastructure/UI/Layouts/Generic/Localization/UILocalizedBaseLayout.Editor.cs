using System;
using Localization;
using Sapientia.Extensions;

namespace UI
{
	public abstract partial class UILocalizedBaseLayout
	{
		// TODO: идея пришла в голову сделать серилизуемый словарь для тегов или аргументов которые можно просто получить
		// Нужно только потом в виджете будет дособрать если требуется
		//
		// Напомню что идея была в том чтобы заполнять локаль данными которые можно выбрать прям в верстке (инспекторе)
		// У нас есть аргументы из локали и мы им выставляем значения или выбираем откуда взять
#if UNITY_EDITOR
		[NonSerialized]
		private string _languageEditor;

		public string languageEditor
		{
			get
			{
				if (_languageEditor.IsNullOrEmpty())
					return LocManager.CurrentLanguageEditor;

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

			if (UnityEngine.Application.isPlaying)
				return;

			if (Label && locInfo.enable)
				Label.text = LocManager.GetEditor(locInfo, _languageEditor.IsNullOrEmpty() ? null : _languageEditor);
		}
#endif
	}
}
