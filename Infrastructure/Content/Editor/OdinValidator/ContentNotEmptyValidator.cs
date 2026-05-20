using Content.Editor;
using JetBrains.Annotations;
using Sirenix.OdinInspector.Editor;
using Sirenix.OdinInspector.Editor.Validation;

[assembly: RegisterValidationRule(typeof(ContentNotEmptyValidator),
	Name = "Content Not Empty",
	Description = "Validates that content references and entries are not empty, null, or invalid")]
// [assembly: RegisterValidationRule(typeof(ContentNotNullJetbrainsValidator),
// 	Name = "Content Not Empty (NotNull from JetBrains)",
// 	Description = "Validates that content references and entries are not empty, null, or invalid")]
// [assembly: RegisterValidationRule(typeof(ContentNotNullValidator),
// 	Name = "Content Not Empty (NotNull from CodeAnalysis)",
// 	Description = "Validates that content references and entries are not empty, null, or invalid")]

namespace Content.Editor
{
	public class ContentNotEmptyValidator : AttributeValidator<NotEmptyAttribute>
	{
		protected override void Validate(ValidationResult result) => Validate(Property, result);

		internal static void Validate(InspectorProperty property, ValidationResult result)
		{
			var valueEntry = property.ValueEntry;

			if (valueEntry == null)
				return;

			if (valueEntry.WeakSmartValue is IContentReference reference)
			{
				if (reference.IsEmpty())
					result.AddError($"Content Reference '{property.NiceName}' must not be empty!");
				if (!reference.IsValid())
					result.AddError($"Content Reference '{property.NiceName}' invalid!");
			}

			if (valueEntry.WeakSmartValue is IContentEntry entry)
				if (ContentNotEmptyUtility.IsNull(entry))
					result.AddError($"Content Entry '{property.NiceName}' must not be null");
		}
	}

	// public class ContentNotNullJetbrainsValidator : AttributeValidator<NotNullAttribute>
	// {
	// 	protected override void Validate(ValidationResult result)
	// 	{
	// 		ContentNotEmptyValidator.Validate(Property, result);
	// 	}
	// }
	//
	// public class ContentNotNullValidator : AttributeValidator<System.Diagnostics.CodeAnalysis.NotNullAttribute>
	// {
	// 	protected override void Validate(ValidationResult result)
	// 	{
	// 		ContentNotEmptyValidator.Validate(Property, result);
	// 	}
	// }
}
