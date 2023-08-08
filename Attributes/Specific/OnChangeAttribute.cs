namespace Fusumity.Attributes.Specific
{
	public class OnChangeAttribute : FusumityDrawerAttribute
	{
		public readonly string methodPath = "";

		public OnChangeAttribute(string methodPath)
		{
			this.methodPath = methodPath;
		}
	}
}