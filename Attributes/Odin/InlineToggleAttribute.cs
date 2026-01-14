using Sirenix.OdinInspector;
using System;

namespace Fusumity.Attributes.Odin
{
	public class InlineToggleAttribute : Attribute
	{
		public string valueGetter;
		public string toggleAction;
		public string label;

		public int margins;
		public int width;
		public SdfIconType icon;
		public IconAlignment iconAlignment;
		public string showIf;

		public InlineToggleAttribute(string valueGetter, string label)
		{
			this.valueGetter = valueGetter;
			this.label = label;
		}

		public InlineToggleAttribute(string valueGetter, string toggleAction, string label)
		{
			this.valueGetter = valueGetter;
			this.toggleAction = toggleAction;
			this.label = label;
		}
	}
}
