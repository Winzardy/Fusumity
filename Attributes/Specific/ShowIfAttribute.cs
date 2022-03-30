namespace Fusumity.Attributes.Specific
{
	public class ShowIfAttribute : GenericDrawerAttribute
	{
		public string boolPath;

		public ShowIfAttribute(string boolPath)
		{
			this.boolPath = boolPath;
		}
	}
}
