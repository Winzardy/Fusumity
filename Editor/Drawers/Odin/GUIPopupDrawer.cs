using System.Linq;
using System.Reflection;
using Sapientia.Extensions;
using UnityEditor;
using UnityEngine;

namespace Fusumity.Editor
{
	public class GUIPopupDrawer
	{
		private string _controlId;

		private string[] _values;

		private GUIPopupSelector<string> _selector;

		private SerializedProperty _currentProperty;
		private FieldInfo _fieldInfo;

		public int valuesCount { get { return _values?.Length ?? 0; } }

		public GUIPopupDrawer(string controlId, string[] values)
		{
			_controlId = controlId;
			_values = values;
		}

		public void UpdateValues(string[] values)
		{
			_selector = null;
			_values = values;
		}

		public void Update(SerializedProperty property, FieldInfo fieldInfo)
		{
			if (property.propertyType != SerializedPropertyType.String)
			{
				Debug.LogError($"Invalid property type for {_controlId}: [ {property.propertyType} ]");
				return;
			}

			_currentProperty = property;
			_fieldInfo = fieldInfo;
		}

		public void Clear()
		{
			_currentProperty = null;
			_fieldInfo = null;
		}

		public void Draw(GUIContent label)
		{
			var rect = EditorGUILayout.GetControlRect();
			Draw(rect, label);
		}

		public void Draw(Rect position, GUIContent label)
		{
			var selector = GetSelector();

			if (selector == null) return;

			if (selector.selectedValue == null)
			{
				DrawEmptyValue(position, _currentProperty);
			}
			else
			{
				DrawSelector(position, label, _currentProperty);
			}
		}

		public void DrawEmptyValue(Rect position, SerializedProperty property)
		{
			var cantBeEmpty =
				!_values.Contains(FusumityEditorGUILayout.NONE) &&
				property.stringValue.IsNullOrEmpty();

			if (cantBeEmpty)
			{
				_selector.SelectFirst();
			}
			else
			{
				if (GUI.Button(position, $"Detected invalid value: [ {property.stringValue} ] Press to clear."))
				{
					_selector.SelectFirst();
				}
			}
		}

		public void DrawSelector(Rect position, GUIContent label, SerializedProperty property)
		{
			if (!label.text.IsNullOrEmpty())
			{
				GUI.SetNextControlName(_controlId);

				var labelPos = position;
				labelPos.width = EditorGUIUtility.labelWidth;
				EditorGUI.SelectableLabel(labelPos, label.text);

				position.xMin += EditorGUIUtility.labelWidth;
			}

			_selector.DrawDropdown(position, drawFunctions: IsArray(property));

			bool IsArray(SerializedProperty property)
			{
				if (_fieldInfo != null)
				{
					return _fieldInfo.FieldType == typeof(string[]);
				}

				return property.isArray &&
					property.arrayElementType != "char";
			}
		}

		private void SelectedCallback(string value)
		{
			if (_currentProperty != null)
			{
				SaveValue(_currentProperty, value);
			}
		}

		private void SaveValue(SerializedProperty property, string value)
		{
			if (property.stringValue == value) return;

			if (value == FusumityEditorGUILayout.NONE)
			{
				property.stringValue = string.Empty;
				Debug.Log($"[<color=white>{GetFieldName(property)}</color>] {_controlId} has been cleared.");
			}
			else
			{
				property.stringValue = value;
				Debug.Log(
					$"[<color=white>{GetFieldName(property)}</color>] {_controlId} changed to: [ <color=yellow>{value}</color> ]");
			}

			property.serializedObject.ApplyModifiedProperties();

			string GetFieldName(SerializedProperty property)
			{
				if (_fieldInfo != null)
				{
					return _fieldInfo.Name;
				}

				return property.displayName;
			}
		}

		private GUIPopupSelector<string> GetSelector()
		{
			if (_currentProperty == null) return null;

			if (_selector == null)
			{
				_selector = CreateSelector(_currentProperty);
			}

			return _selector;
		}

		private GUIPopupSelector<string> CreateSelector(SerializedProperty property)
		{
			var selectedValue =
				property.stringValue.IsNullOrWhiteSpace() ? FusumityEditorGUILayout.NONE : property.stringValue;

			var selector = new GUIPopupSelector<string>(_values, selectedValue, SelectedCallback);

			return selector;
		}
	}
}
