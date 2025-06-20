using System;
using Sirenix.OdinInspector.Editor;

namespace Content.Editor
{
	public static class ContentInspectorPropertyUtility
	{
		public static bool IsAnyParentHasAttribute<TAttribute>(this InspectorProperty property)
			where TAttribute : Attribute
		{
			while (property != null)
			{
				if (property.Attributes.HasAttribute<TAttribute>())
					return true;
				if (property.GetAttribute<TAttribute>() != null)
					return true;

				property = property.Parent;
			}

			return false;
		}

		public static bool IsAnyParentHasAttribute<T1, T2>(this InspectorProperty property)
			where T1 : Attribute
			where T2 : Attribute
		{
			while (property != null)
			{
				if (property.Attributes.HasAttribute<T1>() || property.Attributes.HasAttribute<T2>())
					return true;

				if (property.GetAttribute<T1>() != null)
					return true;
				if (property.GetAttribute<T2>() != null)
					return true;

				property = property.Parent;
			}

			return false;
		}
	}
}
