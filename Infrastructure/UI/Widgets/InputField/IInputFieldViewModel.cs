using System;
using Localization;
using Sapientia;

namespace UI
{
	public enum InputFieldContentType
	{
		Standard,
		Autocorrected,
		IntegerNumber,
		DecimalNumber,
		Alphanumeric,
		Name,
		EmailAddress,
		Password,
		Pin,
		Custom
	}

	public enum InputFieldInputType
	{
		Standard,
		AutoCorrect,
		Password,
	}

	public enum InputFieldCharacterValidation
	{
		None,
		Digit,
		Integer,
		Decimal,
		Alphanumeric,
		Name,
		Regex,
		EmailAddress,
		CustomValidator
	}

	public class DefaultInputViewModel : IInputFieldViewModel
	{
		public string Text { get; set; }
		public event Action<string> TextChanged;

		public Range<int> CharacterRange { get; set; }
		public InputFieldContentType ContentType { get; set; }
		public InputFieldInputType? InputType { get; set; }
		public InputFieldCharacterValidation? CharacterValidation { get; set; }

		public string Style { get; set; }
		public event Action<string> StyleChanged;

		public string Placeholder { get; set; }
		public LocText PlaceholderLoc { get; set; }
		public event Action<string> PlaceholderChanged;

		public void SetText(string text)
		{
			Text = text;
			TextChanged?.Invoke(text);
		}
	}

	public interface IInputFieldViewModel
	{
		public string Text { get; set; }
		public event Action<string> TextChanged;

		/// <summary>
		/// При max == 0, нет ограничений на ввод символов
		/// </summary>
		public Range<int> CharacterRange { get; }

		public InputFieldContentType ContentType { get; }
		public InputFieldInputType? InputType { get; }
		public InputFieldCharacterValidation? CharacterValidation { get; }

		public string Style => null;
		public event Action<string> StyleChanged;

		public string Placeholder => null;
		public LocText PlaceholderLoc => null;
		public event Action<string> PlaceholderChanged;

		public void OnSubmitted()
		{
		}

		public void OnEndEdit()
		{
		}

		public void SetText(string text);

		public sealed void OnValueChanged(string text)
		{
			SetText(text);

			OnValueChanged();
		}

		public void OnValueChanged()
		{
		}
		// string CharacterRangeErrorMessage => $"Minimum chars: {CharacterRange.min}";
		//
		// public string DefaultValidationFunc(string text)
		// {
		// 	if (_layout.inputField.text.IsNullOrWhiteSpace())
		// 	{
		// 		return "Empty"; //TODO: переделать на локализированную версию
		// 	}
		//
		// 	if (_args.CharacterRange.min > text.Length)
		// 	{
		// 		return _args.CharacterRangeErrorMessage;
		// 	}
		//
		// 	return string.Empty;
		// }
	}
}
