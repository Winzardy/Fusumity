namespace Fusumity.Attributes.Specific
{
	public class EnableIfAttribute : GenericDrawerAttribute
	{
		public string boolPath;

		public EnableIfAttribute(string boolPath)
		{
			this.boolPath = boolPath;
		}
	}
}
