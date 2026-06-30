using System;
using System.Collections.Generic;
using System.Reflection;
using Content.Editor;
using Fusumity.Attributes;
using Fusumity.Editor;
using Fusumity.Utility;
using Sapientia;
using Sapientia.Collections;
using Sapientia.Extensions;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;
using Clipboard = Fusumity.Utility.Clipboard;

namespace Content.ScriptableObjects.Editor
{
	public class ContentScriptableObjectAttributeProcessor : OdinAttributeProcessor<ContentScriptableObject>
	{
		private static readonly string LABEL = "Guid";

		private static readonly string TOOLTIP_PREFIX = $"{LABEL}:\n".ColorText(Color.gray).SizeText(12);

		private const string NAME_SEPARATOR = "_";
		private const string SMART_NAME_SEPARATOR = "/";
		private const string ERROR_MESSAGE = "Can only set a new ID in the root inspector!";

		public override void ProcessChildMemberAttributes(InspectorProperty parentProperty,
			MemberInfo member, List<Attribute> attributes)
		{
			base.ProcessChildMemberAttributes(parentProperty, member, attributes);

			var rootClass = nameof(ContentScriptableObjectAttributeProcessor);
			switch (member.Name)
			{
				case ContentEntryScriptableObject.CUSTOM_ID_FIELD_NAME:

					attributes.Add(new LabelTextAttribute("Id"));
					attributes.Add(new PropertyOrderAttribute(-99));
					attributes.Add(new VerticalGroupAttribute("Identifier"));
					attributes.Add(new ShowInInspectorAttribute());
					attributes.Add(new TooltipAttribute($"@{rootClass}.{nameof(GetTooltip)}($property)"));
					attributes.Add(new ContentIdConflictAttribute());

					if (parentProperty.SerializationRoot.ValueEntry.WeakSmartValue is not IUniqueContentEntryScriptableObject so)
						return;

					var scriptable = so.ScriptableContentEntry.ScriptableObject;

					var classHelperName = nameof(FusumityEditorGUIHelper);
					var memberHelperName = nameof(FusumityEditorGUIHelper.disableInlineEditorIdEditing);
					attributes.Add(new DisableIfAttribute($"@{classHelperName}.{memberHelperName}"));

					if (scriptable && !scriptable.name.IsNullOrEmpty())
					{
						var split = scriptable.name.Split(NAME_SEPARATOR);

						for (var i = 1; i < split.Length; i++)
						{
							attributes.Add(new CustomContextMenuAttribute(
								$"Set/{GetSmartName(scriptable.name, i, true)}",
								$"@{rootClass}.{nameof(SetSmart)}($property, {i})"));
						}
					}

					//attributes.Add(new SuffixLabelAttribute($"@{rootClass}.{nameof(Suffix)}($property)"));

					attributes.Add(new CustomContextMenuAttribute(
						$"Copy Guid",
						$"@{rootClass}.{nameof(CopyGuid)}($property)"));
					attributes.Add(new DelayedPropertyAttribute());
					attributes.Add(new OnValueChangedAttribute(
						$"@{rootClass}.{nameof(OnIdChanged)}($property)"));
					break;

				case nameof(ContentScriptableObject.CreationTimeStr):
					attributes.Add(new TooltipAttribute(ContentScriptableObject.CREATION_TIME_TOOLTIP));
					attributes.Add(new LabelTextAttribute(nameof(ContentScriptableObject.CreationTime), true));
					attributes.Add(new PropertyOrderAttribute(-1));
					attributes.Add(new ShowInInspectorAttribute());
					attributes.Add(new ShowIfAttribute($"@{nameof(ContentScriptableObjectAttributeProcessor)}.{nameof(IsDebugMode)}()"));
					attributes.Add(new ReadOnlyAttribute());
					attributes.Add(new CustomContextMenuAttribute(
						"Force Update",
						$"@{rootClass}.{nameof(ForceUpdateTimeCreated)}($property)"));
					break;

				case ContentEntryScriptableObject.USE_CUSTOM_ID_FIELD_NAME:
				case ContentScriptableObject.TIME_CREATED_FILED_NAME:
					attributes.Add(new HideInInspector());
					break;

				case ContentEntryScriptableObject.GUID_FIELD_NAME:
					attributes.Add(new PropertySpaceAttribute(-1.5f));
					attributes.Add(new ShowIfAttribute($"@{nameof(ContentScriptableObjectAttributeProcessor)}.{nameof(IsDebugMode)}()"));
					attributes.Add(new ShowInInspectorAttribute());
					attributes.Add(new VerticalGroupAttribute("Identifier"));
					break;
				case IContentEntrySource.ENTRY_FIELD_NAME:
					attributes.Add(new HideLabelAttribute());
					attributes.Add(new PropertySpaceAttribute(4));

					attributes.Add(new DisableIfAttribute(nameof(ContentScriptableObject.UseRedirect)));
					break;

				case nameof(ContentScriptableObject.techDescription):
					attributes.Add(new ContentTechDescriptionEditModeAttribute());
					attributes.Add(new TextAreaAttribute(1, 3));
					break;

				case ContentEntryScriptableObject.REDIRECT_FIELD_NAME:
					attributes.Add(new PropertyOrderAttribute(-1));

					attributes.Add(new InfoBoxAttribute("Используются данные из другого конфига", InfoMessageType.Info, nameof(ContentScriptableObject.UseRedirect)));

					AddRedirectAttributes();
					break;

				case ContentEntryScriptableObject.EMPTY_REDIRECT_FIELD_NAME:
					attributes.Add(new LabelTextAttribute(ContentEntryScriptableObject.REDIRECT_FIELD_NAME));
					AddRedirectAttributes();
					break;

					void AddRedirectAttributes()
					{
						attributes.Add(new ShowInInspectorAttribute());
						attributes.Add(new PropertySpaceAttribute(4));
						attributes.Add(new CanBeEmptyAttribute());
						attributes.Add(new TooltipAttribute("Использует данные из указанного конфига вместо текущего"));
					}
			}
		}

		private static Color GetSyncButtonColor()
		{
			Sirenix.Utilities.Editor.GUIHelper.RequestRepaint();
			return Color.HSVToRGB(Mathf.Cos((float) EditorApplication.timeSinceStartup + 1f) * 0.225f + 0.325f, 1, 1);
		}

		public static bool IsDebugMode() => ContentEntryDebugModeMenu.IsEnable;

		public static void OnIdChanged(InspectorProperty property)
		{
			if (property.SerializationRoot.ValueEntry.WeakSmartValue is not IUniqueContentEntryScriptableObject entryScriptableObject)
				return;

			ContentEditorCache.RefreshByValueType(entryScriptableObject.ValueType);

			if (!entryScriptableObject.UseCustomId)
				return;

			ContentAutoConstantsGenerator.ForceInvokeWithDelay(entryScriptableObject.GetType());
		}

		public static string GetTooltip(InspectorProperty property)
		{
			if (property.SerializationRoot.ValueEntry.WeakSmartValue is IUniqueContentEntrySource entryScriptableObject)
				return TOOLTIP_PREFIX + entryScriptableObject.UniqueContentEntry.Guid;

			return string.Empty;
		}

		public static void CopyGuid(InspectorProperty property)
		{
			if (property.SerializationRoot.ValueEntry.WeakSmartValue is not IUniqueContentEntryScriptableObject so)
				return;

			Clipboard.Copy(so.UniqueContentEntry.Guid.ToString());
		}

		public static string Suffix(InspectorProperty property)
		{
			if (property.SerializationRoot.ValueEntry.WeakSmartValue is not IUniqueContentEntryScriptableObject so)
				return string.Empty;

			if (!so.UseCustomId)
				return string.Empty;

			return so.UniqueContentEntry.Guid.ToString();
		}

		public static void ForceUpdateTimeCreated(InspectorProperty property)
		{
			if (property.SerializationRoot.ValueEntry.WeakSmartValue is not ContentScriptableObject so)
				return;

			so.ForceUpdateTimeCreated();
		}

		public static void SetSmart(InspectorProperty property, int depth)
		{
			if (property.SerializationRoot.ValueEntry.WeakSmartValue is not ContentScriptableObject so)
				return;

			if (property.ValueEntry.WeakSmartValue is Toggle<string> id)
			{
				var name = GetSmartName(so.name, depth);

				if (id == name)
					return;

				property.ValueEntry.WeakSmartValue = new Toggle<string>(name);
				EditorUtility.SetDirty(so);
			}
		}

		private static string GetSmartName(string fullName, int depth, bool editor = false)
		{
			var split = fullName.Split(NAME_SEPARATOR);

			if (split.Length > depth)
			{
				var strings = split
					.ToList()
					.GetRange(split.Length - depth, depth)
					.ToArray();

				return string.Join(editor ? "  \u0338 " : SMART_NAME_SEPARATOR, strings);
			}

			return split[^1];
		}
	}

	internal sealed class ContentIdConflictAttribute : Attribute, IAttributeConvertible
	{
		public Attribute Convert()
			=> new ContentIdConflictValueAttribute();
	}

	internal sealed class ContentIdConflictValueAttribute : Attribute
	{
	}

	internal sealed class ContentIdConflictValueAttributeDrawer : OdinAttributeDrawer<ContentIdConflictValueAttribute, string>
	{
		private static readonly HashSet<string> _activeCustomIdConflicts = new();
		private static readonly Dictionary<string, string> _sourceGuidToActiveCustomIdConflict = new();

		protected override void DrawPropertyLayout(GUIContent label)
		{
			var originColor = GUI.color;
			if (HasIdConflict(Property))
				GUI.color = UnityObjectIsNullUtility.GetInvalidColor(originColor);

			CallNextDrawer(label);
			GUI.color = originColor;
		}

		public bool HasIdConflict(InspectorProperty property)
		{
			if (property?.SerializationRoot?.ValueEntry?.WeakSmartValue is not IUniqueContentEntryScriptableObject currentSource)
				return false;

			var currentEntry = currentSource.UniqueContentEntry;
			if (currentEntry == null)
				return false;

			var sourceKey = currentEntry.Guid.ToString();
			if (property.ValueEntry?.WeakSmartValue is not string id || id.IsNullOrEmpty())
			{
				ClearActiveConflict(sourceKey);
				return false;
			}

			var entry = ResolveEntry(currentSource.ValueType, id);
			if (entry == null || entry.Guid == currentEntry.Guid)
			{
				ClearActiveConflict(sourceKey);
				return false;
			}

			var conflictKey = GetConflictKey(currentEntry, entry, id);
			SetActiveConflict(sourceKey, conflictKey);
			LogCustomIdConflictOnce(conflictKey, currentSource, entry, id);
			return true;
		}

		private static IUniqueContentEntry ResolveEntry(Type valueType, string id)
		{
			if (!ContentEditorCache.TryGetSource(valueType, id, out var source) ||
				source is not IUniqueContentEntrySource uniqueSource)
				return null;

			return uniqueSource.UniqueContentEntry;
		}

		private static void SetActiveConflict(string sourceKey, string conflictKey)
		{
			if (_sourceGuidToActiveCustomIdConflict.TryGetValue(sourceKey, out var previousKey) &&
				previousKey != conflictKey)
			{
				_activeCustomIdConflicts.Remove(previousKey);
			}

			_sourceGuidToActiveCustomIdConflict[sourceKey] = conflictKey;
		}

		private static void ClearActiveConflict(string sourceKey)
		{
			if (!_sourceGuidToActiveCustomIdConflict.Remove(sourceKey, out var conflictKey))
				return;

			_activeCustomIdConflicts.Remove(conflictKey);
		}

		private static string GetConflictKey(IUniqueContentEntry currentEntry, IUniqueContentEntry otherEntry, string id)
		{
			var currentGuid = currentEntry.Guid.ToString();
			var otherGuid = otherEntry.Guid.ToString();

			var firstGuid = currentGuid;
			var secondGuid = otherGuid;
			if (string.CompareOrdinal(firstGuid, secondGuid) > 0)
			{
				firstGuid = otherGuid;
				secondGuid = currentGuid;
			}

			return $"{currentEntry.ValueType.FullName}:{id}:{firstGuid}:{secondGuid}";
		}

		private static void LogCustomIdConflictOnce(string conflictKey, IUniqueContentEntrySource currentSource,
			IUniqueContentEntry otherEntry, string id)
		{
			if (!_activeCustomIdConflicts.Add(conflictKey))
				return;

			var currentEntry = currentSource.UniqueContentEntry;
			var currentGuid = currentEntry.Guid.ToString();
			var otherGuid = otherEntry.Guid.ToString();

			ContentDebug.LogError(
				$"Content custom id conflict: id [ {id} ] is already used by an older entry of type [ {currentEntry.ValueType.Name} ]" +
				$"\nCurrent guid: [ {currentGuid} ]" +
				$"\nOther guid: [ {otherGuid} ]",
				currentSource as UnityEngine.Object);
		}
	}
}
