using Fusumity.Attributes.Specific;
using Fusumity.Editor.Extensions;
using UnityEditor;

namespace Fusumity.Editor.Drawers.Specific
{
	[CustomPropertyDrawer(typeof(EnableIfAttribute))]
	public class EnableIfDrawer : FusumityPropertyDrawer
	{
		public override void ModifyPropertyData()
		{
			base.ModifyPropertyData();

			var property = currentPropertyData.property;
			var enableIfAttribute = (EnableIfAttribute)attribute;

			if (enableIfAttribute.equalsAny.Length == 0)
			{
				var boolProperty = property.GetPropertyByLocalPath(enableIfAttribute.checkPath);
				currentPropertyData.isEnabled = boolProperty.boolValue;
				return;
			}

			var checkObject = property.GetPropertyObjectByLocalPath(enableIfAttribute.checkPath);
			foreach (var equalsObject in enableIfAttribute.equalsAny)
			{
				if (checkObject.Equals(equalsObject))
				{
					currentPropertyData.isEnabled = true;
					return;
				}
			}
			currentPropertyData.isEnabled = false;
		}
	}
}