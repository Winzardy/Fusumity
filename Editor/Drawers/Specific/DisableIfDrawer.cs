using Fusumity.Attributes.Specific;
using Fusumity.Editor.Extensions;
using UnityEditor;

namespace Fusumity.Editor.Drawers.Specific
{
	[CustomPropertyDrawer(typeof(DisableIfAttribute))]
	public class DisableIfDrawer : FusumityPropertyDrawer
	{
		public override void ModifyPropertyData()
		{
			base.ModifyPropertyData();

			var property = currentPropertyData.property;
			var disableIfAttribute = (DisableIfAttribute)attribute;

			if (disableIfAttribute.equalsAny == null || disableIfAttribute.equalsAny.Length == 0)
			{
				var isDisabled = property.GetResultByLocalPath<bool>(disableIfAttribute.checkPath);

				if (isDisabled && currentPropertyData.enableState == EnableState.Enable)
				{
					currentPropertyData.enableState = EnableState.Disable;
				}
				return;
			}

			var checkObject = property.GetPropertyObjectByLocalPath(disableIfAttribute.checkPath);
			foreach (var equalsObject in disableIfAttribute.equalsAny)
			{
				if ((checkObject == null && equalsObject == null) || (checkObject != null && checkObject.Equals(equalsObject)))
				{
					currentPropertyData.enableState = EnableState.Disable;
					return;
				}
			}

			if (currentPropertyData.enableState == EnableState.Disable)
			{
				currentPropertyData.enableState = EnableState.Enable;
			}
		}
	}
}