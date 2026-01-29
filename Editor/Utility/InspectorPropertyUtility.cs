using System;
using Sapientia.Collections;
using Sirenix.OdinInspector.Editor;

namespace Fusumity.Editor.Utility
{
	public static class InspectorPropertyUtility
	{
		public static bool AnyParentHasAttribute<TAttribute>(this InspectorProperty property)
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

		public static bool AnyParentHasAttribute<T1, T2>(this InspectorProperty property)
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

		public static void AddAttribute(this InspectorProperty property, Attribute attribute, bool unique = true, bool replace = false)
		{
			var editableAtts = property.Info.GetEditableAttributesList();
			var type = attribute.GetType();

			if (replace)
			{
				editableAtts.RemoveAll(x => x.GetType() == type);
				editableAtts.Add(attribute);

				property.RefreshSetup();
			}
			else
			if (!unique ||
				editableAtts.IsNullOrEmpty() ||
			   !editableAtts.Any(x => x.GetType() == type))
			{
				editableAtts.Add(attribute);
				property.RefreshSetup();
			}
		}

		public static bool TryGetChild(this InspectorProperty property, string name, out InspectorProperty childProperty)
		{
			childProperty = property.Children.Get(name);
			return childProperty != null;
		}
	}
}
