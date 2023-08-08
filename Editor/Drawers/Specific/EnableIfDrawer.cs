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
				var isEnabled = property.GetResultByLocalPath<bool>(enableIfAttribute.checkPath);

				if (!isEnabled && currentPropertyData.enableState == EnableState.Enable)
				{
					currentPropertyData.enableState = EnableState.Disable;
				}
				return;
			}

			var checkObject = property.GetPropertyObjectByLocalPath(enableIfAttribute.checkPath);
			foreach (var equalsObject in enableIfAttribute.equalsAny)
			{
				if ((checkObject == null && equalsObject == null) || (checkObject != null && checkObject.Equals(equalsObject)))
				{
					if (!currentPropertyData.drawPropertyChanged)
					{
						currentPropertyData.drawProperty = true;
					}
					return;
				}
			}
			if (currentPropertyData.enableState == EnableState.Enable)
			{
				currentPropertyData.enableState = EnableState.Disable;
			}
		}
	}
}