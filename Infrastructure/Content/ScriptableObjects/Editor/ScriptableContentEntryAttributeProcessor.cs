using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Content.Editor;
using Sapientia.Collections;
using Sapientia.Reflection;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEngine;

namespace Content.ScriptableObjects.Editor
{
	public class ScriptableContentEntryAttributeProcessor : OdinAttributeProcessor<IContentEntry>
	{
		private const string ARRAY_DEFAULT_LABEL = "Array";
		private const string LIST_DEFAULT_LABEL = "Collection";

		public override bool CanProcessSelfAttributes(InspectorProperty property)
		{
			var type = property.ValueEntry.TypeOfValue;

			if (type.IsGenericType && type.GetGenericTypeDefinition() != typeof(ContentEntry<>))
				return true;

			return false;
		}

		public override void ProcessChildMemberAttributes(InspectorProperty parentProperty,
			MemberInfo member, List<Attribute> attributes)
		{
			base.ProcessChildMemberAttributes(parentProperty, member, attributes);

			if (parentProperty.ValueEntry.WeakSmartValue is not IContentEntry contentEntry)
				return;

			switch (member.Name)
			{
				case ContentConstants.VALUE_FIELD_NAME:

					//TODO: Добавить свой AssetSelector...
					if (typeof(Component).IsAssignableFrom(contentEntry.ValueType))
						attributes.Add(new Fusumity.Attributes.Specific.FastAssetSelectorAttribute());

					if (typeof(IList).IsAssignableFrom(contentEntry.ValueType))
						attributes.Add(new SearchableAttribute());

					if (contentEntry.ValueType.IsSerializeReference())
					{
						attributes.Add(new HideInInspector());
					}
					else
					{
						if (ContentEntryAttributeProcessor.propertyToGUIContent.TryGetValue(parentProperty, out var content))
						{
							if (!content.text.IsNullOrEmpty())
								attributes.Add(new LabelTextAttribute(content.text));
							else
								attributes.Add(new HideLabelAttribute());

							if (!content.tooltip.IsNullOrEmpty())
								attributes.Add(new TooltipAttribute(content.tooltip));
						}
						else
						{
							attributes.Add(new HideLabelAttribute());
						}
					}

					break;

				case ContentConstants.UNITY_VALUE_FIELD_NAME:
					if (!contentEntry.ValueType.IsSerializeReference())
					{
						attributes.Add(new HideInInspector());
					}
					else
					{
						attributes.Add(
							new HideIfAttribute(
								$"@{nameof(ScriptableContentEntryAttributeProcessor)}.{nameof(UseCustomInspector)}($property)"));
					}

					break;

				case ScriptableContentConstants.SCRIPTABLEOBJECT_FIELD_NAME:
				case ScriptableContentConstants.ID_FIELD_NAME:
				case ContentConstants.GUID_FIELD_NAME:
					attributes.Add(new HideInInspector());
					break;

				case ScriptableContentConstants.NESTED_FIELD_NAME:

					var color = Color.Lerp(ContentDebug.COLOR, Color.white, 0.8f);
					attributes.Add(new GUIColorAttribute(
						color.r,
						color.g,
						color.b,
						color.a));
					var expr = $"@{nameof(ScriptableContentEntryAttributeProcessor)}.{nameof(ShowIfNested)}($property)";
					attributes.Add(new ShowIfAttribute(expr));
					attributes.Add(new LabelTextAttribute("Nested Content Entries"));
					var dictionaryDrawerSettings = new DictionaryDrawerSettings
					{
						KeyLabel = "Guid",
						ValueLabel = "Reference",
						IsReadOnly = true,
						KeyColumnWidth = 150
					};
					attributes.Add(dictionaryDrawerSettings);
					attributes.Add(new SpaceAttribute());
					attributes.Add(new PropertyOrderAttribute(9999999));
					attributes.Add(new SearchableAttribute());
					attributes.Add(new CustomContextMenuAttribute("Refresh",
						$"@{nameof(ScriptableContentEntryAttributeProcessor)}.{nameof(OnContextMenuRefreshClicked)}($property)"));
					attributes.Add(new CustomContextMenuAttribute("Cache/Resolve",
						$"@{nameof(ScriptableContentEntryAttributeProcessor)}.{nameof(OnContextMenuCacheResolveClicked)}($property)"));
					attributes.Add(new CustomContextMenuAttribute("Cache/Clear",
						$"@{nameof(ScriptableContentEntryAttributeProcessor)}.{nameof(OnContextMenuCacheClearClicked)}($property)"));
					break;
			}
		}

		public override void ProcessSelfAttributes(InspectorProperty property, List<Attribute> attributes)
		{
			base.ProcessSelfAttributes(property, attributes);

			var guiContent = new GUIContent(property.Label);
			ContentEntryAttributeProcessor.propertyToGUIContent[property] = guiContent;
			var valueType = property.ValueEntry.TypeOfValue.GetGenericArguments()[0];
			attributes.Add(new HideReferenceObjectPickerAttribute());

			var isCollection = typeof(IList).IsAssignableFrom(valueType);
			var collectionLabel = valueType.IsArray ? ARRAY_DEFAULT_LABEL : LIST_DEFAULT_LABEL;
			if (attributes.GetAttribute<HideLabelAttribute>() != null ||
				typeof(IList).IsAssignableFrom(property.ValueEntry.ParentType))
				guiContent.text = isCollection ? collectionLabel : string.Empty;
			else if (attributes.GetAttribute<LabelTextAttribute>() != null)
				guiContent.text = attributes.GetAttribute<LabelTextAttribute>().Text;
			else if (property.Label.text.IsNullOrEmpty() && isCollection)
				guiContent.text = collectionLabel;

			attributes.Add(new HideLabelAttribute());
			attributes.RemoveAll(attr => attr is LabelTextAttribute);
		}

		public static bool ShowIfNested(InspectorProperty property)
		{
			if (!ContentEntryDebugModeMenu.IsEnable)
				return false;

			if (property.ValueEntry.WeakSmartValue is
				Dictionary<SerializableGuid, MemberReflectionReference<IUniqueContentEntry>> _)
				return true;

			return false;
		}

		public static void OnContextMenuRefreshClicked(InspectorProperty property)
		{
			var origin = ContentDebug.Logging.Nested.refresh;

			try
			{
				ContentDebug.Logging.Nested.refresh = true;

				if (property.Parent.Parent.ValueEntry.WeakSmartValue is ContentScriptableObject asset)
					asset.Refresh();
			}
			finally
			{
				ContentDebug.Logging.Nested.refresh = origin;
			}
		}

		public static void OnContextMenuCacheResolveClicked(InspectorProperty property)
		{
			if (property.Parent.Parent.ValueEntry.WeakSmartValue is IContentEntryScriptableObject scriptable)
				ContentEntryEditorUtility.ResolveCache(scriptable);
		}

		public static void OnContextMenuCacheClearClicked(InspectorProperty property)
		{
			if (property.Parent.Parent.ValueEntry.WeakSmartValue is IContentEntryScriptableObject scriptable)
				ContentEntryEditorUtility.ClearCache(scriptable);
		}

		public static bool UseCustomInspector(InspectorProperty property)
		{
			if (property.SerializationRoot.ValueEntry.WeakSmartValue is ContentScriptableObject asset)
				return asset.UseCustomInspector;

			return false;
		}
	}
}
