using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using AssetManagement.AddressableAssets.Editor;
using Fusumity.Attributes;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace AssetManagement.Editor
{
	public abstract class BaseAssetRefAttributeProcessor<T> : OdinAttributeProcessor<T>
	{
		protected abstract string FieldName { get; }
		protected virtual bool NeedHandleAssetReferenceT(InspectorProperty property) => property.ValueEntry.TypeOfValue.IsGenericType;

		public override void ProcessChildMemberAttributes(InspectorProperty parentProperty,
			MemberInfo member, List<Attribute> attributes)
		{
			base.ProcessChildMemberAttributes(parentProperty, member, attributes);

			var isAssetReferenceT = NeedHandleAssetReferenceT(parentProperty);
			if (isAssetReferenceT)
			{
				if (member.Name == IAssetRef.CUSTOM_EDITOR_NAME)
				{
					attributes.Add(new PropertyOrderAttribute(-2));
					AddParentLabelAttribute(parentProperty, attributes);
					attributes.Add(new ShowInInspectorAttribute());
					attributes.Add(new InvalidAssetReferenceAttribute());
				}
			}

			if (member.Name == FieldName)
			{
				if (isAssetReferenceT)
				{
					const string tooltipMessage = "Odin не умеет работать с <b>AssetReference<T></b>, поэтому вот такой хак!";

					attributes.Add(new LabelTextAttribute(nameof(Addressables), true));
					attributes.Add(new ToggleAttribute(tooltipMessage));
					attributes.Add(new PropertyOrderAttribute(-1));
				}
				else
				{
					attributes.Add(new PropertyOrderAttribute(-2));
					AddParentLabelAttribute(parentProperty, attributes);
					OnProcessFieldAttributes(parentProperty, member, attributes);
				}

				return;
			}

			switch (member.Name)
			{
				case nameof(AssetRef.releaseDelayMs):
					attributes.Add(new TooltipAttribute("Задержка перед Release"));
					attributes.Add(new LabelTextAttribute("Release Delay"));
					attributes.Add(new UnitAttribute(Units.Millisecond));
					attributes.Add(new TimeFromMsSuffixLabelAttribute());

					break;
			}
		}

		public override void ProcessSelfAttributes(InspectorProperty property, List<Attribute> attributes)
		{
			base.ProcessSelfAttributes(property, attributes);

			attributes.Add(new FoldoutContainerAttribute());
		}

		protected virtual void OnProcessFieldAttributes(InspectorProperty parentProperty,
			MemberInfo member, List<Attribute> attributes)
		{
		}

		private static void AddParentLabelAttribute(InspectorProperty parentProperty, List<Attribute> attributes)
		{
			if (IsCollectionElement(parentProperty))
				return;

			if (parentProperty.Attributes.HasAttribute<HideLabelAttribute>() ||
				parentProperty.GetAttribute<HideLabelAttribute>() != null)
			{
				attributes.Add(new HideLabelAttribute());
				return;
			}

			var label = GetParentLabel(parentProperty);
			if (!string.IsNullOrEmpty(label))
				attributes.Add(new LabelTextAttribute(label));
		}

		private static bool IsCollectionElement(InspectorProperty property)
		{
			var parentType = property?.ValueEntry?.ParentType;
			return parentType != null && typeof(IList).IsAssignableFrom(parentType);
		}

		private static string GetParentLabel(InspectorProperty parentProperty)
		{
			var labelAttribute = parentProperty.Attributes.GetAttribute<LabelTextAttribute>() ??
				parentProperty.GetAttribute<LabelTextAttribute>();
			if (labelAttribute != null)
				return labelAttribute.Text;

			var label = parentProperty.Label?.text;
			return !string.IsNullOrEmpty(label) ? label : parentProperty.NiceName;
		}
	}

	public class InvalidAssetReferenceAttribute : Attribute
	{
	}

	public class InvalidAssetReferenceEntryDrawer : OdinAttributeDrawer<InvalidAssetReferenceAttribute>
	{
		protected override void DrawPropertyLayout(GUIContent label)
		{
			if (Property.ParentValueProperty.ValueEntry.WeakSmartValue is not IAssetRef assetReferenceEntry)
			{
				CallNextDrawer(label);
				return;
			}

			var value = assetReferenceEntry.EditorAsset;
			if (value == null)
			{
				CallNextDrawer(label);
				return;
			}

			var originColor = GUI.color;
			if (!assetReferenceEntry.AssetReference.IsPopulated())
				GUI.color = Color.Lerp(originColor, SirenixGUIStyles.RedErrorColor, 0.8f);
			CallNextDrawer(label);
			GUI.color = originColor;
		}
	}
}
