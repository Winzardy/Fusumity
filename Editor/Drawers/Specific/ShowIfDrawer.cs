using Fusumity.Attributes.Specific;
using Fusumity.Editor.Extensions;
using UnityEditor;

namespace Fusumity.Editor.Drawers.Specific
{
	[CustomPropertyDrawer(typeof(ShowIfAttribute))]
	public class ShowIfDrawer : FusumityPropertyDrawer
	{
		public override void ModifyPropertyData()
		{
			base.ModifyPropertyData();

			var property = currentPropertyData.property;
			var enableIfAttribute = (ShowIfAttribute)attribute;

			if (enableIfAttribute.equalsAny == null || enableIfAttribute.equalsAny.Length == 0)
			{
				var boolProperty = property.GetPropertyByLocalPath(enableIfAttribute.checkPath);

				bool isShow;
				if (boolProperty == null)
				{
					isShow = (bool)property.InvokeFuncByLocalPath(enableIfAttribute.checkPath);
				}
				else
				{
					isShow = boolProperty.boolValue;
				}

				currentPropertyData.drawProperty = isShow;
				return;
			}

			var checkObject = property.GetPropertyObjectByLocalPath(enableIfAttribute.checkPath);
			foreach (var equalsObject in enableIfAttribute.equalsAny)
			{
				if ((checkObject == null && equalsObject == null) || (checkObject != null && checkObject.Equals(equalsObject)))
				{
					currentPropertyData.drawProperty = true;
					return;
				}
			}
			currentPropertyData.drawProperty = false;
		}
	}
}