namespace Fusumity.Attributes.Specific
{
	public class ButtonAttribute : GenericDrawerAttribute
	{
		public string buttonName;
		public string methodPath;
		// Hide label, body and subBody
		public bool hidePropertyField;

		public ButtonAttribute(string buttonName, string methodPath, bool hidePropertyField = false)
		{
			this.buttonName = buttonName;
			this.methodPath = methodPath;
			this.hidePropertyField = hidePropertyField;
		}

		public ButtonAttribute(string methodPath, bool hidePropertyField = false)
		{
			this.buttonName = null;
			this.methodPath = methodPath;
			this.hidePropertyField = hidePropertyField;
		}
	}
}
