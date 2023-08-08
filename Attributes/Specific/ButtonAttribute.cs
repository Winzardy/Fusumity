using System;

namespace Fusumity.Attributes.Specific
{
	public class ButtonAttribute : FusumityDrawerAttribute
	{
		public readonly string buttonName = "";
		public readonly string methodPath = "";
		// Hide label, body and subBody
		public bool hidePropertyField = false;
		public bool drawBefore = true;

		public ButtonAttribute(string buttonName, string methodPath, bool hidePropertyField = false, bool drawBefore = true)
		{
			this.buttonName = buttonName;
			this.methodPath = methodPath;
			this.hidePropertyField = hidePropertyField;
			this.drawBefore = drawBefore;
		}

		public ButtonAttribute(string methodPath, bool hidePropertyField = false, bool drawBefore = true)
		{
			this.buttonName = null;
			this.methodPath = methodPath;
			this.hidePropertyField = hidePropertyField;
			this.drawBefore = drawBefore;
		}
	}
}