using System;
using Fusumity.Attributes.Specific;
using Fusumity.Editor.Utilities;
using UnityEditor;
using UnityEngine;

namespace Fusumity.Editor.Drawers.Specific
{
	[CustomPropertyDrawer(typeof(ReferenceSelectionAttribute))]
	public class SerializeReferenceSelectorAttributeDrawer : FusumityPropertyDrawer
	{
		private Type[] _currentTypes;
		private Type _selectedType;

		public override void ModifyPropertyData()
		{
			base.ModifyPropertyData();

			propertyData.labelIntersectSubBody = false;
			propertyData.hasFoldout = _selectedType != null;
			propertyData.hasSubBody = true;
			propertyData.hasBody = true;
		}

		public override void DrawSubBody(Rect position)
		{
			var property = propertyData.property;
			var fieldType = fieldInfo.FieldType.IsArray ? fieldInfo.FieldType.GetElementType() : fieldInfo.FieldType;

			if (property.propertyType != SerializedPropertyType.ManagedReference)
			{
				Debug.LogError($"The Property Type {fieldType.Name} is not Managed Reference.");
				return;
			}

			var attr = (ReferenceSelectionAttribute)attribute;
			var targetType = attr.type ?? fieldType;
			var currentType = property.GetManagedReferenceType();

			_selectedType = currentType;
			_currentTypes ??= targetType.GetInheritorTypes(attr.insertNull);

			var typeName = currentType == null ? "None" : currentType.Name;
			if (EditorGUI.DropdownButton(position, new GUIContent(typeName), default))
			{
				position = new Rect(position.x, position.y + position.height, position.width, 200f);
				var v = GUIUtility.GUIToScreenPoint(new Vector2(position.x, position.y));
				position.x = v.x;
				position.y = v.y;

				var popup = new Popup()
				{
					Title = targetType.Name,
					AutoClose = true,
					ScreenRect = position,
					Separator = '.',
					AutoHeight = false,
				};
				var i = 0;
				if (attr.insertNull)
				{
					popup.Item("None", item => { Select(item.order); }, false, i++);
				}
				for (; i < _currentTypes.Length; i++)
				{
					popup.Item(ToCamelCaseSpace(_currentTypes[i].Name), item => { Select(item.order); }, true, i);
				}
				popup.Show();
			}
		}

		private void Select(int newSelected)
		{
			var newType = GetType(newSelected);

			if (_selectedType == newType)
				return;

			var property = propertyData.property;

			property.managedReferenceValue = newType == null ? null : Activator.CreateInstance(newType, true);
			property.serializedObject.ApplyModifiedProperties();

			if (property.serializedObject.context != null)
				EditorUtility.SetDirty(property.serializedObject.context);
		}

		private Type GetType(int typeIndex)
		{
			if (typeIndex < 0 | typeIndex >= _currentTypes.Length)
				return null;
			return _currentTypes[typeIndex];
		}

		private static string ToCamelCaseSpace(string caption)
		{
			if (string.IsNullOrEmpty(caption))
				return string.Empty;
			var str = System.Text.RegularExpressions.Regex.Replace(caption, "[A-Z]", " $0").Trim();
			return char.ToUpper(str[0]) + str.Substring(1);
		}
	}
}
