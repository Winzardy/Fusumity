namespace Fusumity.Attributes.Specific
{
	public class ButtonAttribute : GenericDrawerAttribute
	{
		public string buttonName;
		public string methodPath;

		public ButtonAttribute(string buttonName, string methodPath)
		{
			this.buttonName = buttonName;
			this.methodPath = methodPath;
		}

		public ButtonAttribute(string methodPath)
		{
			this.buttonName = null;
			this.methodPath = methodPath;
		}
	}
}
