namespace Fusumity.Attributes.Specific
{
	public class LabelAttribute : FusumityDrawerAttribute
	{
		public string label;

		public LabelAttribute(string label)
		{
			this.label = label;
		}
	}
}