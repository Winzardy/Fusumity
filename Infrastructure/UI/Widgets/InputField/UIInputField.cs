using System;
using Fusumity.Utility;
using Sapientia;
using Sapientia.Extensions;
using TMPro;

namespace UI
{
	public class UIInputField : UIWidget<UIInputFieldLayout, IInputFieldViewModel>
	{
		private bool _autoHideScrollBar;

		private bool _autoTrim;

		public event Action Submitted;
		public event Action EditEnded;
		public event Action ValueChanged;

		public UITextLocalizationAssigner _textLocAssinger;

		protected override void OnLayoutInstalled()
		{
			Create(out _textLocAssinger)
				.Assign(_layout.placeholder);

			OnValueChanged();
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

		protected override void OnShow(ref IInputFieldViewModel vm)
		{
			SetText(vm.Text);
			vm.TextChanged += SetText;

			_layout.inputField.characterLimit = vm.CharacterRange.max;

			ApplyContentType(vm.ContentType);
			if (vm.CharacterValidation.TryGetValue(out var characterValidation))
				ApplyCharacterValidation(characterValidation);
			if (vm.InputType.TryGetValue(out var inputType))
				ApplyInputType(inputType);

			_textLocAssinger.SetTextSafe(_layout.placeholder, vm.PlaceholderLoc, vm.Placeholder);

			// TODO: должно автоматически определятся
			// _layout.inputField.keyboardType = args.keyboardType;
			// _layout.inputField.shouldHideMobileInput = args.hideMobileInput;
		}

		protected override void OnHide(ref IInputFieldViewModel vm)
		{
			vm.TextChanged -= SetText;
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
			_layout.inputField.SetTextWithoutNotify(text);
		}

		public void ClearText()
		{
			_layout.inputField.text = null;
		}

		public void Activate()
		{
			_layout.inputField.ActivateInputField();
		}

		private void OnValueChanged(string text)
		{
			OnValueChanged();
			vm.OnValueChanged(text);
		}

		private void OnSubmitted(string _)
		{
			Submitted?.Invoke();
		}

		private void OnValueChanged()
		{
			UpdateCounter();

			if (_autoHideScrollBar)
			{
				var viewportRect = _layout.inputField.textViewport.rect;
				var view = viewportRect.height < _layout.inputField.textComponent.preferredHeight;
				_layout.inputField.verticalScrollbar.SetActive(view);
			}

			ValueChanged?.Invoke();
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
			EditEnded?.Invoke();
			vm.OnEndEdit();
		}

		public void SetAutoTrim(bool value)
		{
			_autoTrim = value;
		}

		private void ApplyContentType(InputFieldContentType type)
		{
			_layout.inputField.contentType = type switch
			{
				InputFieldContentType.Standard => TMP_InputField.ContentType.Standard,
				InputFieldContentType.Autocorrected => TMP_InputField.ContentType.Autocorrected,
				InputFieldContentType.IntegerNumber => TMP_InputField.ContentType.IntegerNumber,
				InputFieldContentType.DecimalNumber => TMP_InputField.ContentType.DecimalNumber,
				InputFieldContentType.Alphanumeric => TMP_InputField.ContentType.Alphanumeric,
				InputFieldContentType.Name => TMP_InputField.ContentType.Name,
				InputFieldContentType.EmailAddress => TMP_InputField.ContentType.EmailAddress,
				InputFieldContentType.Password => TMP_InputField.ContentType.Password,
				InputFieldContentType.Pin => TMP_InputField.ContentType.Pin,
				InputFieldContentType.Custom => TMP_InputField.ContentType.Custom,
				_ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
			};
		}

		private void ApplyCharacterValidation(InputFieldCharacterValidation type, string regexPattern = null)
		{
			_layout.inputField.regexValue = string.Empty;
			_layout.inputField.onValidateInput = null;

			if (_layout.inputField.contentType != TMP_InputField.ContentType.Custom)
			{
				GUIDebug.LogError("Can't apply character validation to non-custom input field");
				return;
			}

			_layout.inputField.characterValidation = type switch
			{
				InputFieldCharacterValidation.None => TMP_InputField.CharacterValidation.None,
				InputFieldCharacterValidation.Digit => TMP_InputField.CharacterValidation.Digit,
				InputFieldCharacterValidation.Integer => TMP_InputField.CharacterValidation.Integer,
				InputFieldCharacterValidation.Decimal => TMP_InputField.CharacterValidation.Decimal,
				InputFieldCharacterValidation.Alphanumeric => TMP_InputField.CharacterValidation.Alphanumeric,
				InputFieldCharacterValidation.Name => TMP_InputField.CharacterValidation.Name,
				InputFieldCharacterValidation.Regex => TMP_InputField.CharacterValidation.Regex,
				InputFieldCharacterValidation.EmailAddress => TMP_InputField.CharacterValidation.EmailAddress,
				InputFieldCharacterValidation.CustomValidator => TMP_InputField.CharacterValidation.CustomValidator,
				_ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
			};

			if (_layout.inputField.characterValidation == TMP_InputField.CharacterValidation.Regex)
				_layout.inputField.regexValue = regexPattern;
		}

		private void ApplyInputType(InputFieldInputType type)
		{
			if (_layout.inputField.contentType != TMP_InputField.ContentType.Custom)
			{
				GUIDebug.LogError("Can't apply input type to non-custom input field");
				return;
			}

			_layout.inputField.inputType = type switch
			{
				InputFieldInputType.Standard => TMP_InputField.InputType.Standard,
				InputFieldInputType.AutoCorrect => TMP_InputField.InputType.AutoCorrect,
				InputFieldInputType.Password => TMP_InputField.InputType.Password,
				_ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
			};
		}
	}
}
