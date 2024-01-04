using System;
using System.Collections.Generic;
using System.Reflection;
using Fusumity.Attributes;
using Fusumity.Editor.Extensions;
using UnityEditor;
using UnityEngine;

namespace Fusumity.Editor.Drawers
{
	[CustomPropertyDrawer(typeof(FusumityDrawerAttribute))]
	[CustomPropertyDrawer(typeof(IFusumitySerializable), true)]
	public class FusumityPropertyDrawer : PropertyDrawer
	{
		public static readonly GUIContent SUB_BODY_GUI_CONTENT = new (" ");

		private static readonly Type BASE_DRAWER_TYPE = typeof(FusumityPropertyDrawer);
		private static readonly Type ATTRIBUTE_TYPE = typeof(FusumityDrawerAttribute);

		private static Dictionary<Type, Type> _typeToDrawerType;
		private static HashSet<string> _currentPropertyPath = new HashSet<string>();

		private List<FusumityDrawerAttribute> _fusumityAttributes;
		private List<FusumityPropertyDrawer> _fusumityDrawers;
		// If not use this arrays will suck (very long story).
		private Dictionary<string, PropertyData> _pathToPropertyData;

		public PropertyData currentPropertyData;

		public virtual bool OverrideMethods => true;

		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			var parentPropertyPath = property.GetParentPropertyPath();
			if (_currentPropertyPath.Contains(property.propertyPath))
			{
				// When you open the unity Object selection field the _currentPropertyPath will not cleaned in that frame :(
				if (_currentPropertyPath.Contains(parentPropertyPath))
				{
					var height = property.GetPropertyHeight_Cached();
					_currentPropertyPath.Clear();
					return height;
				}
				_currentPropertyPath.Clear();
			}
			else if (!_currentPropertyPath.Contains(parentPropertyPath))
			{
				_currentPropertyPath.Add(parentPropertyPath);
			}

			_currentPropertyPath.Add(property.propertyPath);

			LazyInitializeAttributes();
			LazyInitializeDrawers(property);
			LazyInitializePropertyData(property.propertyPath);
			SetupPropertyData(property.propertyPath);

			var originalProperty = property;
			property = ExecuteModifySerializedProperty(originalProperty);
			currentPropertyData.ResetData(property, originalProperty, label);
			ExecuteModifyPropertyData();

			_currentPropertyPath.Remove(property.propertyPath);
			_currentPropertyPath.Remove(parentPropertyPath);

			return currentPropertyData.GetTotalHeight();
		}

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			var propertyPath = property.propertyPath;
			if (_currentPropertyPath.Contains(propertyPath))
			{
				property.PropertyField_Cached();
				return;
			}

			SetupPropertyData(propertyPath);

			if (currentPropertyData == null || !currentPropertyData.drawProperty || currentPropertyData.forceBreak)
				return;

			_currentPropertyPath.Add(propertyPath);

			var oldBackgroundColor = GUI.backgroundColor;
			var oldGuiEnabled = GUI.enabled;
			var lastIndentLevel = EditorGUI.indentLevel;
			var lastLabelWidth = EditorGUIUtility.labelWidth;

			GUI.backgroundColor = currentPropertyData.backgroundColor;
			switch (currentPropertyData.enableState)
			{
				case EnableState.ForceDisable:
				case EnableState.Disable:
					GUI.enabled = false;
					break;
				case EnableState.ForceEnable:
					GUI.enabled = true;
					break;
			}

			EditorGUI.BeginChangeCheck();
			EditorGUI.BeginProperty(position, label, property);

			EditorGUI.indentLevel += currentPropertyData.indent;

			position.yMin += currentPropertyData.drawOffsetY;
			position.xMin += currentPropertyData.drawOffsetX;

			var labelPrefixPosition = Rect.zero;
			if (currentPropertyData.ShouldDrawLabelPrefix())
			{
				labelPrefixPosition = new Rect(position.x, position.y, EditorGUIUtility.labelWidth, EditorGUIUtility.singleLineHeight);

				var offset = currentPropertyData.labelPrefixWidth;
				if (offset > EditorGUIUtility.labelWidth)
					offset = EditorGUIUtility.labelWidth;
				position.xMin += offset;
			}

			var propertyPosition = position;
			if (currentPropertyData.hasBeforeExtension)
				propertyPosition.yMin += currentPropertyData.beforeExtensionHeight;
			if (currentPropertyData.hasAfterExtension)
				propertyPosition.yMax -= currentPropertyData.afterExtensionHeight;

			var beforeExtensionPosition = currentPropertyData.hasBeforeExtension
				? new Rect(position.x, position.y, position.width, currentPropertyData.beforeExtensionHeight)
				: Rect.zero;

			var labelPosition = currentPropertyData.hasLabel
				? new Rect(propertyPosition.x, propertyPosition.y, EditorGUIUtility.labelWidth, EditorGUIUtility.singleLineHeight)
				: Rect.zero;
			var foldoutPosition = currentPropertyData.hasFoldout
				? new Rect(propertyPosition.x, propertyPosition.y, propertyPosition.width, EditorGUIUtility.singleLineHeight)
				: Rect.zero;
			var subBodyPosition = currentPropertyData.hasLabel & !currentPropertyData.labelIntersectSubBody
				? new Rect(propertyPosition.x + EditorGUIUtility.labelWidth, propertyPosition.y,
					propertyPosition.width - EditorGUIUtility.labelWidth, EditorGUIUtility.singleLineHeight)
				: new Rect(propertyPosition.x, propertyPosition.y, propertyPosition.width, EditorGUIUtility.singleLineHeight);
			var bodyPosition = (currentPropertyData.hasLabel | currentPropertyData.hasSubBody)
				? new Rect(propertyPosition.x, propertyPosition.y + EditorGUIUtility.singleLineHeight, propertyPosition.width,
					propertyPosition.height - EditorGUIUtility.singleLineHeight)
				: propertyPosition;

			var afterExtensionPosition = currentPropertyData.hasAfterExtension
				? new Rect(position.x, propertyPosition.yMax, position.width, currentPropertyData.afterExtensionHeight)
				: Rect.zero;

			ExecuteValidateBeforeDrawing();

			var indentDebt = 0;

			if (currentPropertyData.hasFoldout)
			{
#if UNITY_2022_3_OR_NEWER
				if (EditorGUI.indentLevel > 0)
				{
					EditorGUI.indentLevel--;
					indentDebt++;
				}
				if (currentPropertyData.isArrayElement)
				{
					EditorGUI.indentLevel--;
					indentDebt++;
				}
#endif
				EditorGUI.indentLevel += currentPropertyData.foldoutIndent;
				currentPropertyData.property.isExpanded = EditorGUI.Foldout(foldoutPosition, currentPropertyData.property.isExpanded, "");
				EditorGUI.indentLevel -= currentPropertyData.foldoutIndent;
#if UNITY_2022_3_OR_NEWER
				EditorGUI.indentLevel++;
#endif
			}

			if (currentPropertyData.hasBeforeExtension)
			{
				ExecuteDrawBeforeExtension(beforeExtensionPosition);
			}

			if (currentPropertyData.ShouldDrawLabelPrefix())
			{
				var isEnabled = GUI.enabled;
				GUI.enabled = false;

				DrawLabelPrefix(labelPrefixPosition);

				GUI.enabled = isEnabled;
			}

			if (currentPropertyData.hasLabel)
			{
				ExecuteDrawLabel(labelPosition);
			}

			if (currentPropertyData.ShouldDrawSubBody())
			{
				if (!currentPropertyData.hasLabel | !currentPropertyData.labelIntersectSubBody)
				{
					EditorGUIUtility.labelWidth = EditorExt.INDENT_WIDTH;
				}

				ExecuteDrawSubBody(subBodyPosition);

				EditorGUIUtility.labelWidth = lastLabelWidth;
			}

			if (currentPropertyData.ShouldDrawBody())
			{
				if (currentPropertyData.hasLabel)
				{
					EditorGUI.indentLevel++;
				}

				ExecuteDrawBody(bodyPosition);
			}

			if (!currentPropertyData.forceBreak && currentPropertyData.hasAfterExtension)
			{
				ExecuteDrawAfterExtension(afterExtensionPosition);
			}

			EditorGUI.indentLevel += indentDebt;

			EditorGUI.EndProperty();
			if (!currentPropertyData.forceBreak && EditorGUI.EndChangeCheck())
			{
				property.serializedObject.ApplyModifiedProperties();
				ExecuteOnPropertyChanged();
			}

			EditorGUIUtility.labelWidth = lastLabelWidth;
			EditorGUI.indentLevel = lastIndentLevel;
			GUI.enabled = oldGuiEnabled;
			GUI.backgroundColor = oldBackgroundColor;

			_currentPropertyPath.Remove(propertyPath);
		}

		#region Initialization

		private void LazyInitializeAttributes()
		{
			if (_fusumityAttributes != null && _fusumityAttributes.Count > 0)
				return;

			var attributes = new List<FusumityDrawerAttribute>();
			var customAttributes = fieldInfo.GetCustomAttributes();

			foreach (var customAttribute in customAttributes)
			{
				if (!(customAttribute is FusumityDrawerAttribute fusumityDrawerAttribute))
					continue;
				if (fusumityDrawerAttribute.Equals(attribute))
					continue;
				attributes.Add(fusumityDrawerAttribute);
			}

			_fusumityAttributes = attributes;
		}

		private void LazyInitializeDrawers(SerializedProperty property)
		{
			if (_fusumityDrawers != null && _fusumityDrawers.Count > 0)
				return;

			if (_typeToDrawerType == null)
			{
				var drawersTypes = BASE_DRAWER_TYPE.GetInheritorTypes();
				_typeToDrawerType = new Dictionary<Type, Type>(drawersTypes.Length * 3);

				foreach (var drawerType in drawersTypes)
				{
					var customAttributes = drawerType.GetCustomAttributes<CustomPropertyDrawer>();

					foreach (var customAttribute in customAttributes)
					{
						var customAttributeTypes = customAttribute.GetCustomPropertyDrawerTypes();
						foreach (var customAttributeType in customAttributeTypes)
						{
							if (!_typeToDrawerType.ContainsKey(customAttributeType))
							{
								_typeToDrawerType.Add(customAttributeType, drawerType);
							}
							else
							{
								Debug.Log(customAttributeType.Name);
								Debug.Log(drawerType.Name);
							}
						}
					}
				}
			}

			_fusumityDrawers = new List<FusumityPropertyDrawer>(_fusumityAttributes.Count);
			{
				var propertyType = property.GetPropertyType();
				if (propertyType != null && _typeToDrawerType.TryGetValue(propertyType, out var drawerType))
				{
					var drawer = (FusumityPropertyDrawer)Activator.CreateInstance(drawerType);
					drawer.SetFieldInfo(fieldInfo);

					_fusumityDrawers.Add(drawer);
				}
			}

			for (var i = 0; i < _fusumityAttributes.Count; i++)
			{
				var genericAttribute = _fusumityAttributes[i];

				if (!_typeToDrawerType.TryGetValue(genericAttribute.GetType(), out var drawerType))
				{
					drawerType = BASE_DRAWER_TYPE;
				}

				var drawer = (FusumityPropertyDrawer)Activator.CreateInstance(drawerType);
				drawer.SetAttribute(genericAttribute);
				drawer.SetFieldInfo(fieldInfo);

				_fusumityDrawers.Add(drawer);
			}
		}

		private void LazyInitializePropertyData(string propertyPath)
		{
			if (_pathToPropertyData == null)
				_pathToPropertyData = new Dictionary<string, PropertyData>();
			if (!_pathToPropertyData.TryGetValue(propertyPath, out var drawerData))
			{
				drawerData = new PropertyData();
				currentPropertyData = new PropertyData();
				_pathToPropertyData.Add(propertyPath, drawerData);
			}
		}

		private void SetupPropertyData(string propertyPath)
		{
			if (_pathToPropertyData == null || !_pathToPropertyData.TryGetValue(propertyPath, out currentPropertyData))
				return;

			foreach (var drawer in _fusumityDrawers)
			{
				drawer.currentPropertyData = currentPropertyData;
			}
		}

		#endregion

		#region Custom Executers

		private SerializedProperty ExecuteModifySerializedProperty(SerializedProperty originalProperty)
		{
			if (!this.IsReplaceSerializedPropertyOverriden())
			{
				foreach (var drawer in _fusumityDrawers)
				{
					if (drawer.IsReplaceSerializedPropertyOverriden())
					{
						return drawer.ReplaceSerializedProperty(originalProperty);
					}
				}
			}
			return ReplaceSerializedProperty(originalProperty);
		}

		private void ExecuteModifyPropertyData()
		{
			ModifyPropertyData();

			foreach (var drawer in _fusumityDrawers)
			{
				drawer.ModifyPropertyData();
			}
		}

		private void ExecuteValidateBeforeDrawing()
		{
			ValidateBeforeDrawing();

			foreach (var drawer in _fusumityDrawers)
			{
				drawer.ValidateBeforeDrawing();
			}
		}

		private void ExecuteDrawBeforeExtension(Rect position)
		{
			DrawBeforeExtension(ref position);
			foreach (var drawer in _fusumityDrawers)
			{
				drawer.DrawBeforeExtension(ref position);
			}
		}

		private void ExecuteDrawLabel(Rect position)
		{
			if (!this.IsDrawLabelOverriden())
			{
				foreach (var drawer in _fusumityDrawers)
				{
					if (drawer.IsDrawLabelOverriden())
					{
						drawer.DrawLabel(position);
						return;
					}
				}
			}

			DrawLabel(position);
		}

		private void ExecuteDrawSubBody(Rect position)
		{
			if (!this.IsDrawSubBodyOverriden())
			{
				foreach (var drawer in _fusumityDrawers)
				{
					if (drawer.IsDrawSubBodyOverriden())
					{
						drawer.DrawSubBody(position);
						return;
					}
				}
			}

			DrawSubBody(position);
		}

		private void ExecuteDrawBody(Rect position)
		{
			if (!this.IsDrawBodyOverriden())
			{
				foreach (var drawer in _fusumityDrawers)
				{
					if (drawer.IsDrawBodyOverriden())
					{
						drawer.DrawBody(position);
						return;
					}
				}
			}

			DrawBody(position);
		}

		private void ExecuteDrawAfterExtension(Rect position)
		{
			DrawAfterExtension(ref position);
			foreach (var drawer in _fusumityDrawers)
			{
				drawer.DrawAfterExtension(ref position);
			}
		}

		private void ExecuteOnPropertyChanged()
		{
			OnPropertyChanged();

			foreach (var drawer in _fusumityDrawers)
			{
				drawer.OnPropertyChanged();
			}
		}

		#endregion

		#region Custom

		public virtual SerializedProperty ReplaceSerializedProperty(SerializedProperty originalProperty) => originalProperty;

		public virtual void ModifyPropertyData() {}

		public virtual void ValidateBeforeDrawing() {}

		public virtual void DrawBeforeExtension(ref Rect position) {}

		public virtual void DrawLabelPrefix(Rect position)
		{
			EditorGUI.LabelField(position, currentPropertyData.labelPrefix, currentPropertyData.labelStyle);
		}

		public virtual void DrawLabel(Rect position)
		{
			EditorGUI.LabelField(position, currentPropertyData.label, currentPropertyData.labelStyle);
		}

		public virtual void DrawSubBody(Rect position)
		{
			currentPropertyData.property.DrawBody(position);
		}

		public virtual void DrawBody(Rect position)
		{
			currentPropertyData.property.DrawBody(position);
		}

		public virtual void DrawAfterExtension(ref Rect position) {}

		public virtual void OnPropertyChanged() {}

		#endregion
	}

	public enum EnableState
	{
		Enable,
		Disable,
		ForceEnable,
		ForceDisable
	}

	public class PropertyData
	{
		public bool forceBreak;

		public SerializedProperty property;
		public SerializedProperty originalProperty;
		public GUIContent label;

		public string labelPrefix;
		public float labelPrefixWidth;

		public bool drawPropertyChanged;
		public bool drawProperty;
		public EnableState enableState;

		public bool hasBeforeExtension;
		public bool hasFoldout;
		public bool hasLabel;
		public bool hasSubBody;
		public bool hasBody;
		public bool hasAfterExtension;

		public bool isArrayElement;

		public bool drawSubBodyWhenRollUp;
		public bool labelIntersectSubBody;

		public float beforeExtensionHeight;
		public float labelHeight;
		public float bodyHeight;
		public float afterExtensionHeight;

		public float drawOffsetY;
		public float drawOffsetX;
		public int indent;
		public int foldoutIndent;

		public GUIStyle labelStyle;

		public Color backgroundColor;

		public void ResetData(SerializedProperty property, SerializedProperty originalProperty, GUIContent label)
		{
			forceBreak = false;

			// GetPropertyHeight will singleLineHeight if no expanded
			var isExpanded = property.isExpanded;
			property.isExpanded = true;

			this.property = property;
			this.originalProperty = originalProperty;
			this.label = new GUIContent(label);

			labelPrefix = string.Empty;
			labelPrefixWidth = 0;

			drawPropertyChanged = false;
			drawProperty = true;
			enableState = EnableState.Enable;

			beforeExtensionHeight = 0f;
			labelHeight = EditorGUIUtility.singleLineHeight;
			bodyHeight = property.GetPropertyHeight(true);
			afterExtensionHeight = 0f;

			drawOffsetY = 0f;
			drawOffsetX = 0f;
			indent = 0;
			foldoutIndent = 0;

			isArrayElement = property.IsArrayElement();

			var hasChildren = bodyHeight > labelHeight && property.HasChildren();

			hasBeforeExtension = false;
			hasFoldout = hasChildren;
			hasLabel = true;
			hasSubBody = !hasChildren;
			hasBody = hasChildren;
			hasAfterExtension = false;

			drawSubBodyWhenRollUp = true;
			labelIntersectSubBody = true;
			if (hasChildren)
				bodyHeight -= labelHeight;

			labelStyle = EditorStyles.label;

			backgroundColor = GUI.backgroundColor;

			property.isExpanded = isExpanded;
		}

		public float GetTotalHeight()
		{
			var height = 0f;
			if (!drawProperty)
				return height;

			if (hasBeforeExtension)
			{
				height += beforeExtensionHeight;
			}
			if (hasLabel || ShouldDrawSubBody())
			{
				height += labelHeight;
			}
			if (ShouldDrawBody())
			{
				height += bodyHeight;
			}
			if (hasAfterExtension)
			{
				height += afterExtensionHeight;
			}

			if (hasFoldout && height == 0f)
				height = EditorGUIUtility.singleLineHeight;

			height += drawOffsetY;
			return height;
		}

		public bool ShouldDrawSubBody()
		{
			return hasSubBody && !forceBreak && (property.isExpanded || !hasFoldout || drawSubBodyWhenRollUp);
		}

		public bool ShouldDrawBody()
		{
			return hasBody && !forceBreak && (property.isExpanded || !hasFoldout);
		}

		public bool ShouldDrawLabelPrefix()
		{
			return labelPrefix != null && labelPrefixWidth > 0;
		}
	}
}