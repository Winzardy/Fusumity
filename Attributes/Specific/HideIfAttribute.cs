namespace Fusumity.Attributes.Specific
{
	public class HideIfAttribute : GenericDrawerAttribute
	{
		public string boolPath;

		public HideIfAttribute(string boolPath)
		{
			this.boolPath = boolPath;
		}
	}
}
