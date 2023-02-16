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

			if (enableIfAttribute.equalsAny == null || enableIfAttribute.equalsAny.Length == 0)
			{
				var boolProperty = property.GetPropertyByLocalPath(enableIfAttribute.checkPath);

				bool isEnabled;
				if (boolProperty == null)
				{
					isEnabled = (bool)property.InvokeFuncByLocalPath(enableIfAttribute.checkPath);
				}
				else
				{
					isEnabled = boolProperty.boolValue;
				}

				if (isEnabled || !currentPropertyData.isEnabledChanged)
				{
					currentPropertyData.isEnabled = isEnabled;
					currentPropertyData.isEnabledChanged = true;
				}
				return;
			}

			var checkObject = property.GetPropertyObjectByLocalPath(enableIfAttribute.checkPath);
			foreach (var equalsObject in enableIfAttribute.equalsAny)
			{
				if ((checkObject == null && equalsObject == null) || (checkObject != null && checkObject.Equals(equalsObject)))
				{
					currentPropertyData.isEnabled = true;
					currentPropertyData.isEnabledChanged = true;
					return;
				}
			}

			if (!currentPropertyData.isEnabledChanged)
			{
				currentPropertyData.isEnabled = false;
				currentPropertyData.isEnabledChanged = true;
			}
		}
	}
}