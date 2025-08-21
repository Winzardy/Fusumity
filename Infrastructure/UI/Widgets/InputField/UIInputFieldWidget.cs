using System;
using Fusumity.Utility;
using TMPro;
using Sapientia.Extensions;

namespace UI
{
	[Serializable]
	public class InputFieldArgs
	{
		public bool checkEmpty;
		public int minimumCharacterAmount;
		public int characterLimit;
		public TMP_InputField.CharacterValidation characterValidation;
		public TMP_InputField.ContentType contentType;

		public bool autoHideScrollBar;

		public Func<string, string> correctionFunc;
	}

	//TODO: доделать
	public class UIInputFieldWidget : UIWidget<UIInputFieldWidgetLayout>
	{
		private bool _autoHideScrollBar;
		private InputFieldArgs _args;

		private Func<string, string> _correctionFunc;
		private bool _autoTrim;

		public event Action submitted;
		public event Action editEnded;
		public event Action valueChanged;

		protected override void OnLayoutInstalled()
		{
			HandleValueChanged();
			_layout.inputField.onValueChanged.AddListener(OnValueChanged);

			_layout.inputField.onSubmit.AddListener(OnSubmitted);
			_layout.inputField.onEndEdit.AddListener(OnEndEdit);

			UpdateCounter();
		}

		protected override void OnLayoutCleared()
		{
			_layout.inputField.onValueChanged.RemoveListener(OnValueChanged);

			_layout.inputField.onSubmit.RemoveListener(OnSubmitted);
			_layout.inputField.onEndEdit.RemoveListener(OnEndEdit);
		}

		public void UpdateArgs(InputFieldArgs args)
		{
			_args = args;

			if (Active)
				OnShow();
		}

		protected override void OnShow()
		{
			if (_args == null)
				return;

			_layout.inputField.characterLimit = _args.characterLimit;
			_layout.inputField.characterValidation = _args.characterValidation;
			_layout.inputField.contentType = _args.contentType;

			_autoHideScrollBar = _args.autoHideScrollBar;
			_correctionFunc = _args.correctionFunc;
		}

		public bool TryGetText(out string value)
		{
			return TryGetText(DefaultValidationFunc, false, out value);
		}

		public bool TryGetText(bool invalidState, out string value)
		{
			return TryGetText(DefaultValidationFunc, invalidState, out value);
		}

		public bool TryGetText(Func<string, string> validationFunc, bool invalidState, out string value)
		{
			value = null;

			var text = _layout.inputField.text;
			text = _autoTrim ? text.Trim() : text;

			var validationResult = validationFunc.Invoke(text);

			if (!validationResult.IsNullOrEmpty())
			{
				if (invalidState)
				{
					InvalidState(true, validationResult);
				}

				return false;
			}

			if (invalidState)
			{
				InvalidState(false);
			}

			value = text;
			return true;
		}

		public void InvalidState(bool value, string error = null)
		{
			_layout.invalidGroup.SetActive(value);

			if (_layout.errorMsg != null)
			{
				if (!error.IsNullOrEmpty() && value)
				{
					_layout.errorMsg.SetActive(true);
					_layout.errorMsg.text = error;
				}
				else
				{
					_layout.errorMsg.SetActive(false);
				}
			}
		}

		public void SetReadOnly(bool value)
		{
			_layout.inputField.readOnly = value;
			_layout.inputField.interactable = !value;
		}

		public void SetText(object text)
		{
			SetText(text.ToString());
		}

		public void SetText(string text)
		{
			_layout.inputField.text = text;
		}

		/// <summary>
		/// Placeholder is Graphic type...
		/// </summary>
		public void TrySetPlaceholderText(string text)
		{
			if (_layout.inputField.placeholder is TMP_Text tmp)
			{
				tmp.text = text;
			}
		}

		public string DefaultValidationFunc(string arg)
		{
			if (_layout == null)
				return "Layout not created";

			if (_args.checkEmpty && _layout.inputField.text.IsNullOrWhiteSpace())
			{
				return "Empty"; //TODO: переделать на локализированную версию
			}

			if (_args.minimumCharacterAmount > arg.Length)
			{
				return $"Minimum chars: {_args.minimumCharacterAmount}"; //TODO: переделать на локализированную версию
			}

			return string.Empty;
		}

		public string GetRawText()
		{
			return _layout.inputField.text;
		}

		public void ClearText()
		{
			_layout.inputField.text = null;
		}

		public void Activate()
		{
			_layout.inputField.ActivateInputField();
		}

		private void OnValueChanged(string _)
		{
			HandleValueChanged();
		}

		private void OnSubmitted(string _)
		{
			submitted?.Invoke();
		}

		private void HandleValueChanged()
		{
			UpdateCounter();

			if (_autoHideScrollBar)
			{
				var viewportRect = _layout.inputField.textViewport.rect;
				var view = viewportRect.height < _layout.inputField.textComponent.preferredHeight;
				_layout.inputField.verticalScrollbar.SetActive(view);
			}

			if (_correctionFunc != null)
			{
				_layout.inputField.text = _correctionFunc.Invoke(_layout.inputField.text);
			}

			valueChanged?.Invoke();
		}

		private void UpdateCounter()
		{
			if (_layout.inputField.characterLimit > 0 && _layout.characterCounter != null)
			{
				_layout.characterCounter.text =
					_layout.inputField.text.Length + "/" + _layout.inputField.characterLimit;
			}
		}

		private void OnEndEdit(string _)
		{
			editEnded?.Invoke();
		}

		public void SetAutoTrim(bool value)
		{
			_autoTrim = value;
		}
	}
}
