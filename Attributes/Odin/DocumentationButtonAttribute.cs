using System;
using Sirenix.OdinInspector;

namespace Fusumity.Attributes.Odin
{
	[DontApplyToListElements]
	[AttributeUsage(AttributeTargets.All, AllowMultiple = true, Inherited = true)]
	[System.Diagnostics.Conditional("UNITY_EDITOR")]
	public class DocumentationButtonAttribute : UrlButtonAttribute
	{
		public DocumentationButtonAttribute(string url) : base("Documentation", url, SdfIconType.Book)
		{
			width = 130f;
		}
	}
}
