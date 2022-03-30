namespace Fusumity.Attributes.Specific
{
	public class DisableIfAttribute : GenericDrawerAttribute
	{
		public string boolPath;

		public DisableIfAttribute(string boolPath)
		{
			this.boolPath = boolPath;
		}
	}
}
