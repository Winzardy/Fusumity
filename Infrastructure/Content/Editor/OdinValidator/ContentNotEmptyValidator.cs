using System;
using Content.Editor;
using JetBrains.Annotations;
using Sirenix.OdinInspector.Editor.Validation;

[assembly: RegisterValidationRule(typeof(ContentNotEmptyValidator),
	Name = "Content Not Empty",
	Description = "Validates that content references and entries are not empty, null, or invalid")]
[assembly: RegisterValidationRule(typeof(ContentNotNullJetbrainsValidator),
	Name = "Content Not Empty (NotNull from JetBrains)",
	Description = "Validates that content references and entries are not empty, null, or invalid")]
[assembly: RegisterValidationRule(typeof(ContentNotNullValidator),
	Name = "Content Not Empty (NotNull from CodeAnalysis)",
	Description = "Validates that content references and entries are not empty, null, or invalid")]

namespace Content.Editor
{
	public class ContentNotEmptyValidator : ContentNotEmptyValidator<NotEmptyAttribute>
	{
	}

	public class ContentNotNullJetbrainsValidator : ContentNotEmptyValidator<NotNullAttribute>
	{
	}

	public class ContentNotNullValidator : ContentNotEmptyValidator<System.Diagnostics.CodeAnalysis.NotNullAttribute>
	{
	}

	public class ContentNotEmptyValidator<T> : AttributeValidator<T>
		where T : Attribute
	{
		protected override void Validate(ValidationResult result)
		{
			var valueEntry = Property.ValueEntry;

			if (valueEntry == null)
				return;

			if (valueEntry.WeakSmartValue is IContentReference reference)
			{
				if (reference.IsEmpty())
					result.AddError($"Content Reference '{Property.NiceName}' must not be empty!");
				if (!reference.IsValid())
					result.AddError($"Content Reference '{Property.NiceName}' invalid!");
			}

			if (valueEntry.WeakSmartValue is IContentEntry entry)
				if (ContentNotEmptyUtility.IsNull(entry))
					result.AddError($"Content Entry '{Property.NiceName}' must not be null");
		}
	}
}
