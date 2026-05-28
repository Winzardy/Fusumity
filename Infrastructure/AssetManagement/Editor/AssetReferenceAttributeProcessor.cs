using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Fusumity.Editor.Utility;
using Sapientia.Extensions;
using Sapientia.Utility;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;
using UnityEngine;

namespace AssetManagement.Editor
{
	public class AssetReferenceAttributeProcessor : BaseAssetReferenceAttributeProcessor<IAssetReference>
	{
		protected override string FieldName => nameof(AssetReference.assetReference);

		public override void ProcessChildMemberAttributes(InspectorProperty parentProperty, MemberInfo member, List<Attribute> attributes)
		{
			base.ProcessChildMemberAttributes(parentProperty, member, attributes);

			if (member.Name != IAssetReference.CUSTOM_EDITOR_NAME)
				return;

			if (!IsGameObjectEntry(parentProperty))
				return;

			if (!TryGetRequiredComponentAttribute(parentProperty, out var attribute, false))
				return;

			attributes.Add(new ComponentReferencePickerAttribute(attribute.ComponentType));
		}

		public static IEnumerable<ValueDropdownItem<GameObject>> FilterByRequiredComponent(InspectorProperty property)
		{
			if (!TryGetRequirement(property, out _, out var componentType, out _))
				yield break;

			if (componentType == null)
				yield break;

			foreach (var obj in AssetDatabaseUtility.EnumeratePrefabsOfType(componentType))
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

			if (HasRequiredComponent(gameObject, componentType, requirement.IncludeChildren))
				return true;

			message = requirement.IncludeChildren
				? $"GameObject [ {gameObject.name} ] does not contain component [ {componentType.FullName} ] on itself or children"
				: $"GameObject [ {gameObject.name} ] does not contain component [ {componentType.FullName} ]";
			return false;
		}

		private static bool IsGameObjectEntry(InspectorProperty property)
		{
			var type = property?.ValueEntry?.TypeOfValue;
			if (type == null)
				return false;

			var interfaceType = type
				.GetInterfaces()
				.FirstOrDefault(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(IAssetReference<>));

			return interfaceType?.GetGenericArguments()[0] == typeof(GameObject);
		}

		private static bool TryGetRequirement(InspectorProperty property,
			out AssetReferenceRequiredComponentAttribute attribute,
			out Type componentType,
			out string error)
		{
			attribute     = null;
			componentType = null;
			error         = null;

			if (!TryGetRequiredComponentAttribute(property, out attribute))
				return false;

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
}
