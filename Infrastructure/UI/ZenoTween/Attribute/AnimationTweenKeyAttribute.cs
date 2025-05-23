using System;
using System.Diagnostics;

namespace ZenoTween
{
	[Conditional("UNITY_EDITOR")]
	public class AnimationTweenKeyAttribute : Attribute
	{
		public string FieldName { get; private set; }
		public string Filter { get; private set; }
		public bool DisableWarning { get; private set; }

		public AnimationTweenKeyAttribute(string filter = "", string fieldName = "customSequences", bool warning = true)
		{
			Filter = filter;
			DisableWarning = !warning;
			FieldName = fieldName;
		}
	}
}
