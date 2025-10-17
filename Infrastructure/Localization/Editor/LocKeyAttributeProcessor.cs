using Fusumity.Editor;

namespace Localization.Editor
{
	public class LocKeyAttributeProcessor : ValueWrapperOdinAttributeProcessor<LocKey>
	{
		protected override string ValueFieldName => nameof(LocKey.value);
	}
}
