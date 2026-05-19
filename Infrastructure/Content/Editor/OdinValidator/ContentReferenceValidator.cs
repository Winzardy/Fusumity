using Content.Editor;
using Sirenix.OdinInspector.Editor.Validation;

[assembly: RegisterValidationRule(typeof(ContentReferenceValidator),
	Name = "Content Reference Check",
	Description = "Validates that content references point to existing and valid content entries")]

namespace Content.Editor
{
	public class ContentReferenceValidator : ValueValidator<IContentReference>
	{
		protected override void Validate(ValidationResult result)
		{
			if (!Value.IsEmpty() && !Value.IsValid())
				result.AddError($"Content Reference '{Property.NiceName}' invalid!");
		}
	}
}
