using Fusumity.Attributes.Specific;
using Fusumity.Editor.Extensions;
using UnityEditor;

namespace Fusumity.Editor.Drawers.Specific
{
	[CustomPropertyDrawer(typeof(HideIfAttribute))]
	public class HideIfDrawer : FusumityPropertyDrawer
	{
		public override void ModifyPropertyData()
		{
			base.ModifyPropertyData();

			var property = currentPropertyData.property;
			var disableIfAttribute = (HideIfAttribute)attribute;

			if (disableIfAttribute.equalsAny == null || disableIfAttribute.equalsAny.Length == 0)
			{
				var boolProperty = property.GetPropertyByLocalPath(disableIfAttribute.checkPath);

				bool isHide;
				if (boolProperty == null)
				{
					isHide = (bool)property.InvokeFuncByLocalPath(disableIfAttribute.checkPath);
				}
				else
				{
					isHide = boolProperty.boolValue;
				}

				currentPropertyData.drawProperty = !isHide;
				return;
			}

			var checkObject = property.GetPropertyObjectByLocalPath(disableIfAttribute.checkPath);
			foreach (var equalsObject in disableIfAttribute.equalsAny)
			{
				if ((checkObject == null && equalsObject == null) || (checkObject != null && checkObject.Equals(equalsObject)))
				{
					currentPropertyData.drawProperty = false;
					return;
				}
			}
			currentPropertyData.drawProperty = true;
		}
	}
}