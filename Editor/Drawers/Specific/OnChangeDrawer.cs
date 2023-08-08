using Fusumity.Attributes.Specific;
using Fusumity.Editor.Extensions;
using UnityEditor;

namespace Fusumity.Editor.Drawers.Specific
{
	[CustomPropertyDrawer(typeof(OnChangeAttribute))]
	public class OnChangeDrawer : FusumityPropertyDrawer
	{
		public override void OnPropertyChanged()
		{
			base.OnPropertyChanged();

			var onValidateAttribute = (OnChangeAttribute)attribute;
			if (string.IsNullOrEmpty(onValidateAttribute.methodPath))
				return;

			Undo.RecordObject(currentPropertyData.property.serializedObject.targetObject, onValidateAttribute.methodPath);
			currentPropertyData.property.InvokeMethodByLocalPath(onValidateAttribute.methodPath);
			currentPropertyData.property.serializedObject.targetObject.SaveChanges();
		}
	}
}