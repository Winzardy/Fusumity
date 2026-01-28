using Sapientia;

namespace Fusumity.Editor
{
	public class ScheduleSchemeAttributeProcessor : ValueWrapperOdinAttributeProcessor<ScheduleScheme>
	{
		protected override string ValueFieldName => nameof(ScheduleScheme.points);
	}
}
