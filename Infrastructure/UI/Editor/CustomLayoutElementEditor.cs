using System;
using UnityEditor;
using UnityEditor.UI;
using UnityEngine;

namespace UI.Editor
{
	[CustomEditor(typeof(CustomLayoutElement), false)]
	[CanEditMultipleObjects]
	public class CustomLayoutElementEditor : LayoutElementEditor
	{
		private CustomLayoutElement _target;

		private SerializedProperty _useMaxWidthProperty;
		private SerializedProperty _maxWidthRectProperty;
		private SerializedProperty _maxWidthProperty;

		private SerializedProperty _useMaxHeightProperty;
		private SerializedProperty _maxHeightRectProperty;
		private SerializedProperty _maxHeightProperty;

		//TODO: доделать
		private SerializedProperty _useMinWidthProperty;
		private SerializedProperty _minWidthRectProperty;
		private SerializedProperty _minWidthProperty;

		private SerializedProperty _useMinHeightProperty;
		private SerializedProperty _minHeightRectProperty;
		private SerializedProperty _minHeightProperty;

		protected override void OnEnable()
		{
			m_IgnoreLayout = serializedObject.FindProperty("m_IgnoreLayout");

			_target = target as CustomLayoutElement;

			_maxWidthRectProperty = serializedObject.FindProperty("_maxWidthRect");
			_useMaxWidthProperty = serializedObject.FindProperty("_useMaxWidth");
			_maxWidthProperty = serializedObject.FindProperty("_maxWidth");

			_maxHeightRectProperty = serializedObject.FindProperty("_maxHeightRect");
			_useMaxHeightProperty = serializedObject.FindProperty("_useMaxHeight");
			_maxHeightProperty = serializedObject.FindProperty("_maxHeight");

			_minWidthProperty = serializedObject.FindProperty("m_MinWidth");
			_minWidthRectProperty = serializedObject.FindProperty("_minWidthRect");
			_useMinWidthProperty = serializedObject.FindProperty("_useMinWidth");

			_minHeightProperty = serializedObject.FindProperty("m_MinHeight");
			_minHeightRectProperty = serializedObject.FindProperty("_minHeightRect");
			_useMinHeightProperty = serializedObject.FindProperty("_useMinHeight");

			m_PreferredWidth = serializedObject.FindProperty("m_PreferredWidth");
			m_PreferredHeight = serializedObject.FindProperty("m_PreferredHeight");
			m_FlexibleWidth = serializedObject.FindProperty("m_FlexibleWidth");
			m_FlexibleHeight = serializedObject.FindProperty("m_FlexibleHeight");
			m_LayoutPriority = serializedObject.FindProperty("m_LayoutPriority");
		}

		public override void OnInspectorGUI()
		{
			serializedObject.Update();

			EditorGUILayout.PropertyField(m_IgnoreLayout);

			if (!m_IgnoreLayout.boolValue)
			{
				EditorGUILayout.Space();

				LayoutElementField(_maxWidthProperty, _useMaxWidthProperty, _maxWidthRectProperty);
				LayoutElementField(_maxWidthRectProperty, _useMaxWidthProperty);

				LayoutElementField(_maxHeightProperty, _useMaxHeightProperty, _maxHeightRectProperty, false);
				LayoutElementField(_maxHeightRectProperty, _useMaxHeightProperty);

				LayoutElementField(_minWidthProperty, _useMinWidthProperty, _minWidthRectProperty);
				LayoutElementField(_minWidthRectProperty, _useMinWidthProperty);

				LayoutElementField(_minHeightProperty, _useMinHeightProperty, _minHeightRectProperty, false);
				LayoutElementField(_minHeightRectProperty, _useMinHeightProperty);

				var originEnabled = GUI.enabled;
				GUI.enabled = !_useMaxWidthProperty.boolValue;
				LayoutElementField(m_PreferredWidth, t => t.rect.width);
				GUI.enabled = !_useMaxHeightProperty.boolValue;
				LayoutElementField(m_PreferredHeight, t => t.rect.height);
				GUI.enabled = originEnabled;

				LayoutElementField(m_FlexibleWidth, 1);
				LayoutElementField(m_FlexibleHeight, 1);
			}

			EditorGUILayout.PropertyField(m_LayoutPriority);

			serializedObject.ApplyModifiedProperties();
		}

		private void LayoutElementField(SerializedProperty property, SerializedProperty useProperty)
		{
			if (!useProperty.boolValue)
				return;

			var originIndent = EditorGUI.indentLevel;
			EditorGUI.indentLevel++;
			EditorGUILayout.PropertyField(property, new GUIContent("Rect"));
			EditorGUI.indentLevel = originIndent;
		}

		private void LayoutElementField(SerializedProperty property, SerializedProperty useProperty, SerializedProperty rectProperty,
			bool width = true)
		{
			var position = EditorGUILayout.GetControlRect();

			var label = EditorGUI.BeginProperty(position, null, property);

			var fieldPosition = EditorGUI.PrefixLabel(position, label);

			var toggleRect = fieldPosition;
			toggleRect.width = 16;

			var floatFieldRect = fieldPosition;
			floatFieldRect.xMin += 16;

			var use = EditorGUI.Toggle(toggleRect, useProperty.boolValue);
			useProperty.boolValue = use;

			if (use)
			{
				EditorGUIUtility.labelWidth = 4;
				if (rectProperty.objectReferenceValue != null)
					GUI.enabled = false;

				property.floatValue = EditorGUI.FloatField(floatFieldRect, new GUIContent(" "),
					rectProperty.objectReferenceValue != null
						? width
							? ((RectTransform) rectProperty.objectReferenceValue).rect.width
							: ((RectTransform) rectProperty.objectReferenceValue).rect.height
						: property.floatValue);

				GUI.enabled = true;
				EditorGUIUtility.labelWidth = 0;
			}

			EditorGUI.EndProperty();
		}

		SerializedProperty m_IgnoreLayout;
		SerializedProperty m_MinWidth;
		SerializedProperty m_MinHeight;
		SerializedProperty m_PreferredWidth;
		SerializedProperty m_PreferredHeight;
		SerializedProperty m_FlexibleWidth;
		SerializedProperty m_FlexibleHeight;
		SerializedProperty m_LayoutPriority;

		private void LayoutElementField(SerializedProperty property, float defaultValue)
		{
			LayoutElementField(property, _ => defaultValue);
		}

		private void LayoutElementField(SerializedProperty property, Func<RectTransform, float> defaultValue)
		{
			Rect position = EditorGUILayout.GetControlRect();

			// Label
			GUIContent label = EditorGUI.BeginProperty(position, null, property);

			// Rects
			Rect fieldPosition = EditorGUI.PrefixLabel(position, label);

			Rect toggleRect = fieldPosition;
			toggleRect.width = 16;

			Rect floatFieldRect = fieldPosition;
			floatFieldRect.xMin += 16;

			// Checkbox
			EditorGUI.BeginChangeCheck();
			bool enabled = EditorGUI.ToggleLeft(toggleRect, GUIContent.none, property.floatValue >= 0);
			if (EditorGUI.EndChangeCheck())
			{
				// This could be made better to set all of the targets to their initial width, but mimizing code change for now
				property.floatValue = (enabled ? defaultValue(_target.transform as RectTransform) : -1);
			}

			if (!property.hasMultipleDifferentValues && property.floatValue >= 0)
			{
				// Float field
				EditorGUIUtility.labelWidth = 4; // Small invisible label area for drag zone functionality
				EditorGUI.BeginChangeCheck();
				float newValue = EditorGUI.FloatField(floatFieldRect, new GUIContent(" "), property.floatValue);
				if (EditorGUI.EndChangeCheck())
				{
					property.floatValue = Mathf.Max(0, newValue);
				}

				EditorGUIUtility.labelWidth = 0;
			}

			EditorGUI.EndProperty();
		}
	}
}
