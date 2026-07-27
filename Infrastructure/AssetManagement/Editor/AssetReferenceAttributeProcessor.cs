using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Fusumity.Editor;
using Fusumity.Editor.Utility;
using Sapientia.Extensions;
using Sapientia.Utility;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace AssetManagement.Editor
{
	public class AssetReferenceAttributeProcessor : BaseAssetReferenceAttributeProcessor<IAssetReference>
	{
		protected override string FieldName => nameof(AnyAssetReference.assetReference);

		public override void ProcessChildMemberAttributes(InspectorProperty parentProperty, MemberInfo member, List<Attribute> attributes)
		{
			base.ProcessChildMemberAttributes(parentProperty, member, attributes);

			if (member.Name != IAssetReference.CUSTOM_EDITOR_NAME)
				return;

			if (!TryGetPickerComponentType(parentProperty, out var componentType, out var includeChildren))
				return;

			attributes.Add(new AssetReferenceComponentPickerAttribute(componentType, includeChildren));
		}

		public static IEnumerable<ValueDropdownItem<GameObject>> FilterByRequiredComponent(InspectorProperty property)
		{
			if (!TryGetRequirement(property, out _, out var componentType, out _))
				yield break;

			if (componentType == null)
				yield break;

			foreach (var obj in EnumeratePrefabsOfType(componentType, false))
				yield return new ValueDropdownItem<GameObject>(obj.name, obj);
		}

		internal static bool ValidateRequiredComponent(InspectorProperty property, out string message)
		{
			message = null;

			if (!TryGetRequirement(property, out var requirement, out var componentType, out var error))
				return true;

			if (!string.IsNullOrEmpty(error))
			{
				message = error;
				return false;
			}

			if (componentType == null)
			{
				message = "Required component type is not set";
				return false;
			}

			if (property?.ValueEntry?.WeakSmartValue is not GameObject gameObject || gameObject == null)
				return true;

			var includeChildren = requirement?.IncludeChildren ?? false;
			if (HasRequiredComponent(gameObject, componentType, includeChildren))
				return true;

			message = includeChildren
				? $"GameObject [ {gameObject.name} ] does not contain component [ {componentType.FullName} ] on itself or children"
				: $"GameObject [ {gameObject.name} ] does not contain component [ {componentType.FullName} ]";
			return false;
		}

		internal static IEnumerable<GameObject> EnumeratePrefabsOfType(Type componentType, bool includeChildren)
		{
			foreach (var prefab in AssetDatabaseUtility.EnumeratePrefabsOfType(componentType))
			{
				if (HasRequiredComponent(prefab, componentType, includeChildren))
					yield return prefab;
			}
		}

		private static bool TryGetPickerComponentType(InspectorProperty property, out Type componentType, out bool includeChildren)
		{
			componentType = null;
			includeChildren = false;

			if (!TryGetReferenceValueType(property, out var referenceValueType))
				return false;

			if (referenceValueType == typeof(GameObject))
			{
				if (!TryGetRequirement(property, out var requirement, out componentType, out _, false))
					return false;

				includeChildren = requirement?.IncludeChildren ?? false;
				return true;
			}

			if (!IsComponentReferenceValue(referenceValueType))
				return false;

			componentType = referenceValueType;
			return true;
		}

		private static bool TryGetRequirement(InspectorProperty property,
			out AssetReferenceRequiredComponentAttribute attribute,
			out Type componentType,
			out string error,
			bool includeParents = true)
		{
			attribute = null;
			componentType = null;
			error = null;

			if (!TryGetRequiredComponentAttribute(property, out attribute, includeParents))
			{
				if (TryGetReferenceValueType(property, out var referenceValueType) && IsComponentReferenceValue(referenceValueType))
				{
					componentType = referenceValueType;
					return true;
				}

				return false;
			}

			componentType = attribute.ComponentType;

			if (!attribute.ComponentTypeName.IsNullOrEmpty())
			{
				if (!ReflectionUtility.TryGetType(attribute.ComponentTypeName, out componentType))
					error = $"Unable to resolve required component type [ {attribute.ComponentTypeName} ]";
			}

			if (componentType != null && !typeof(Component).IsAssignableFrom(componentType))
				error = $"Required type [ {componentType.FullName} ] is not a Unity component";

			return true;
		}

		private static bool TryGetRequiredComponentAttribute(InspectorProperty property,
			out AssetReferenceRequiredComponentAttribute attribute,
			bool includeParents = true)
		{
			attribute = null;
			var current = property;

			while (current != null)
			{
				attribute = current.Attributes.GetAttribute<AssetReferenceRequiredComponentAttribute>() ??
					current.GetAttribute<AssetReferenceRequiredComponentAttribute>();

				if (attribute != null)
					return true;

				if (!includeParents)
					break;

				current = current.Parent;
			}

			return false;
		}

		private static bool TryGetReferenceValueType(InspectorProperty property, out Type valueType)
		{
			valueType = null;

			if (TryGetReferenceValueType(property?.ValueEntry?.TypeOfValue, out valueType))
				return true;

			return TryGetReferenceValueType(property?.ParentValueProperty?.ValueEntry?.TypeOfValue, out valueType);
		}

		private static bool TryGetReferenceValueType(Type type, out Type valueType)
		{
			valueType = null;
			if (type == null)
				return false;

			if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IAssetReference<>))
			{
				valueType = type.GetGenericArguments()[0];
				return true;
			}

			var interfaceType = type
				.GetInterfaces()
				.FirstOrDefault(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(IAssetReference<>));

			valueType = interfaceType?.GetGenericArguments()[0];
			return valueType != null;
		}

		private static bool IsComponentReferenceValue(Type type)
		{
			return type != null && !type.ContainsGenericParameters && typeof(Component).IsAssignableFrom(type);
		}

		private static bool HasRequiredComponent(GameObject gameObject, Type componentType, bool includeChildren)
		{
			if (gameObject == null || componentType == null)
				return false;

			if (includeChildren)
				return gameObject.GetComponentInChildren(componentType, true) != null;

			return gameObject.TryGetComponent(componentType, out _);
		}
	}

	public class ValidateRequiredAssetComponentAttribute : Attribute
	{
	}

	public class ValidateRequiredAssetComponentAttributeDrawer : OdinAttributeDrawer<ValidateRequiredAssetComponentAttribute>
	{
		protected override void DrawPropertyLayout(GUIContent label)
		{
			var isValid = AssetReferenceAttributeProcessor.ValidateRequiredComponent(Property, out var message);
			var originColor = GUI.backgroundColor;

			if (!isValid)
				GUI.backgroundColor = Color.Lerp(originColor, SirenixGUIStyles.RedErrorColor, 0.5f);

			if (!isValid && !string.IsNullOrEmpty(message))
				SirenixEditorGUI.ErrorMessageBox(message);

			CallNextDrawer(label);
			GUI.backgroundColor = originColor;
		}
	}

	public class AssetReferenceComponentPickerAttribute : Attribute
	{
		public Type ComponentType { get; }
		public bool IncludeChildren { get; }

		public AssetReferenceComponentPickerAttribute(Type type, bool includeChildren)
		{
			ComponentType = type;
			IncludeChildren = includeChildren;
		}
	}

	public class AssetReferenceComponentPickerDrawer : OdinAttributeDrawer<AssetReferenceComponentPickerAttribute>
	{
		private Rect? _rect;

		protected override void DrawPropertyLayout(GUIContent label)
		{
			if (Property.ParentValueProperty?.ValueEntry?.WeakSmartValue is not IAssetReference reference)
			{
				CallNextDrawer(label);
				return;
			}

			using (new EditorGUI.DisabledScope(Attribute.ComponentType == null))
			{
				if (_rect.HasValue)
				{
					if (GUI.Button(_rect.Value, GUIContent.none))
					{
						var selector = new GenericSelector<Object>("Select",
							AssetReferenceAttributeProcessor.EnumeratePrefabsOfType(Attribute.ComponentType, Attribute.IncludeChildren),
							false,
							x => x.name);
						selector.SetSelection(GetSelectionAsset(reference.EditorAsset));
						selector.EnableSingleClickToSelect();
						selector.SelectionConfirmed += selection =>
						{
							var prefab = selection.FirstOrDefault();
							if (!prefab)
								return;
							Property.ValueEntry.WeakSmartValue = prefab;
						};
						var rect = Property.LastDrawnValueRect;
						rect.width -= EditorGUIUtility.labelWidth;
						rect.x += EditorGUIUtility.labelWidth;
						selector.ShowInPopup(rect); // вот тут нужно чтобы он селектор открывал под полем object
					}
				}
			}

			CallNextDrawer(label);
			_rect = Property.LastDrawnValueRect.AlignRight(18);
		}

		private static Object GetSelectionAsset(Object asset)
		{
			return asset is Component component ? component.gameObject : asset;
		}
	}
}
