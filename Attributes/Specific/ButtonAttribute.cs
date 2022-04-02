namespace Fusumity.Attributes.Specific
{
	public class ButtonAttribute : GenericDrawerAttribute
	{
		public string buttonName;
		public string methodPath;
		public bool hideProperty;

		public ButtonAttribute(string buttonName, string methodPath, bool hideProperty = false)
		{
			this.buttonName = buttonName;
			this.methodPath = methodPath;
			this.hideProperty = hideProperty;
		}

		public ButtonAttribute(string methodPath, bool hideProperty = false)
		{
			this.buttonName = null;
			this.methodPath = methodPath;
			this.hideProperty = hideProperty;
		}
	}
}
