using System;
using Sirenix.OdinInspector;

namespace Fusumity.Attributes.Odin
{
	[DontApplyToListElements]
	[AttributeUsage(AttributeTargets.All, AllowMultiple = true, Inherited = true)]
	[System.Diagnostics.Conditional("UNITY_EDITOR")]
	public class UrlButtonAttribute : Attribute
	{
		public string label;
		public string url;
		public SdfIconType icon;
		public float width = -1;

		public UrlButtonAttribute(string label, string url, SdfIconType icon = SdfIconType.Link)
		{
			this.label = label;
			this.url = url;
			this.icon = icon;
		}

		public UrlButtonAttribute(string url, SdfIconType icon = SdfIconType.Link)
		{
			this.label = url;
			this.url = url;
			this.icon = icon;
		}
	}
}
