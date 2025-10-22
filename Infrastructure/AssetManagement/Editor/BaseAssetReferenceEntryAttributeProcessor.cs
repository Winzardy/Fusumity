using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using AssetManagement.AddressableAssets.Editor;
using Fusumity.Attributes;
using Fusumity.Editor;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace AssetManagement.Editor
{
	public abstract class BaseAssetReferenceEntryAttributeProcessor<T> : OdinAttributeProcessor<T>
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
				if (member.Name == IAssetReferenceEntry.CUSTOM_EDITOR_NAME)
				{
					attributes.Add(new PropertyOrderAttribute(-2));
					if (!typeof(IList).IsAssignableFrom(parentProperty.ValueEntry.ParentType))
						attributes.Add(new LabelTextAttribute(parentProperty.NiceName));
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
					if (!typeof(IList).IsAssignableFrom(parentProperty.ValueEntry.ParentType))
						attributes.Add(new LabelTextAttribute(parentProperty.NiceName));
					OnProcessFieldAttributes(parentProperty, member, attributes);
				}

				return;
			}

			switch (member.Name)
			{
				case nameof(AssetReferenceEntry.releaseDelayMs):
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
	}

	public class InvalidAssetReferenceAttribute : Attribute
	{
	}

	public class InvalidAssetReferenceEntryDrawer : OdinAttributeDrawer<InvalidAssetReferenceAttribute>
	{
		protected override void DrawPropertyLayout(GUIContent label)
		{
			if (Property.ParentValueProperty.ValueEntry.WeakSmartValue is not IAssetReferenceEntry assetReferenceEntry)
				return;

			var value = assetReferenceEntry.EditorAsset;
			if (value == null)
				return;

			var originColor = GUI.color;
			if (!assetReferenceEntry.AssetReference.IsPopulated())
				GUI.color = Color.Lerp(originColor, SirenixGUIStyles.RedErrorColor, 0.8f);
			CallNextDrawer(label);
			GUI.color = originColor;
		}
	}
}
