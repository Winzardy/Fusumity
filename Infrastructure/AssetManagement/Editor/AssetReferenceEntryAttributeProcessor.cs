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
using UnityEngine.Pool;

namespace AssetManagement.Editor
{
	public class AssetReferenceEntryAttributeProcessor : BaseAssetReferenceEntryAttributeProcessor<IAssetReferenceEntry>
	{
		protected override string FieldName => nameof(AssetReferenceEntry.assetReference);

		public override void ProcessChildMemberAttributes(InspectorProperty parentProperty, MemberInfo member, List<Attribute> attributes)
		{
			base.ProcessChildMemberAttributes(parentProperty, member, attributes);

			if (member.Name != IAssetReferenceEntry.CUSTOM_EDITOR_NAME)
				return;

			if (!IsGameObjectEntry(parentProperty))
				return;

			if (!parentProperty.Attributes.HasAttribute<AssetReferenceRequiredComponentAttribute>())
				return;

			var className = nameof(AssetReferenceEntryAttributeProcessor);
			var dropdown = new ValueDropdownAttribute($"@{className}.{nameof(FilterByRequiredComponent)}($property)")
			{
				AppendNextDrawer = true
			};
			attributes.Add(dropdown);
			attributes.Add(new ValidateRequiredAssetComponentAttribute());
		}

		public static IEnumerable<ValueDropdownItem<GameObject>> FilterByRequiredComponent(InspectorProperty property)
		{
			if (!TryGetRequirement(property, out var requirement, out var componentType))
				return null;

			if (componentType == null)
				return Array.Empty<ValueDropdownItem<GameObject>>();

			using (ListPool<GameObject>.Get(out var list))
			{
				foreach (var prefab in AssetDatabaseUtility.LoadPrefabs())
				{
					if (prefab != null && HasRequiredComponent(prefab, componentType, requirement.IncludeChildren))
						list.Add(prefab);
				}

				return list.Select(x => new ValueDropdownItem<GameObject>(x.name, x)).ToArray();
			}
		}

		internal static bool ValidateRequiredComponent(InspectorProperty property, out string message)
		{
			message = null;

			if (!TryGetRequirement(property, out var requirement, out var componentType))
				return true;

			if (componentType == null)
				return false;

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
				.FirstOrDefault(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(IAssetReferenceEntry<>));

			return interfaceType?.GetGenericArguments()[0] == typeof(GameObject);
		}

		private static bool TryGetRequirement(InspectorProperty property,
			out AssetReferenceRequiredComponentAttribute attribute,
			out Type componentType)
		{
			attribute     = null;
			componentType = null;

			if (!TryGetRequiredComponentAttribute(property, out attribute))
				return false;

			componentType = attribute.ComponentType;

			if (!attribute.ComponentTypeName.IsNullOrEmpty())
				ReflectionUtility.TryGetType(attribute.ComponentTypeName, out componentType);

			return true;
		}

		private static bool TryGetRequiredComponentAttribute(InspectorProperty property,
			out AssetReferenceRequiredComponentAttribute attribute)
		{
			attribute = null;
			var current = property;

			while (current != null)
			{
				if (current.Attributes.HasAttribute<AssetReferenceRequiredComponentAttribute>())
				{
					attribute = current.Attributes.GetAttribute<AssetReferenceRequiredComponentAttribute>();
					return true;
				}

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
			var isValid = AssetReferenceEntryAttributeProcessor.ValidateRequiredComponent(Property, out var message);
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
