using System;
using System.Collections.Generic;
using System.Reflection;
using Content;
using Fusumity.Editor;
using Fusumity.Editor.Utility;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Trading.Inventory;
using UnityEngine;

namespace Trading.Editor
{
	public class TraderOfferEntryAttributeProcessor : OdinAttributeProcessor<TraderOfferEntry>
	{
		private const int ICON_SIZE = 64;

		public override void ProcessChildMemberAttributes(InspectorProperty parentProperty, MemberInfo member, List<Attribute> attributes)
		{
			base.ProcessChildMemberAttributes(parentProperty, member, attributes);

			switch (member.Name)
			{
				case nameof(TraderOfferEntry.Preview):
					attributes.Add(new PropertyOrderAttribute(-1));
					attributes.Add(new ShowInInspectorAttribute());
					attributes.Add(new HorizontalGroupAttribute("row", width: ICON_SIZE));
					attributes.Add(new VerticalGroupAttribute("row/left"));
					attributes.Add(new PreviewFieldAttribute(ICON_SIZE, ObjectFieldAlignment.Left));
					attributes.Add(new HideLabelAttribute());

					break;

				case nameof(TraderOfferEntry.icon):
				case nameof(TraderOfferEntry.name):
				case nameof(TraderOfferEntry.label):
					attributes.Add(new VerticalGroupAttribute("row/right"));
					break;
				case nameof(TraderOfferEntry.UseConfirmation):
					attributes.Add(new VerticalGroupAttribute("row/right"));
					attributes.Add(new ShowInInspectorAttribute());
					attributes.Add(new ShowIfAttribute("@TraderOfferEntryAttributeProcessor.ShowIfConfirmation($property)"));
					attributes.Add(new TooltipAttribute("Показывать окно подтверждения перед сделкой\n" +
						"\nP.S Для рекламы и IAP не используется окно подтверждения"));
					break;
			}

			/*
			    [HorizontalGroup("row", width: ICON_SIZE)]
			    [VerticalGroup("row/left")]
			    [PreviewField(ICON_SIZE, ObjectFieldAlignment.Left), HideLabel]
			    public Sprite icon;

			    [VerticalGroup("row/right")]
			    public LocKey nameLocKey;
			 */
		}

		public static bool ShowIfConfirmation(InspectorProperty property)
		{
			if (property?.ParentValueProperty.ValueEntry.WeakSmartValue is TraderOfferEntry entry)
			{
				if (entry.trade.Read().cost is ItemTradeCost _)
					return true;
			}

			return false;
		}

		public override void ProcessSelfAttributes(InspectorProperty property, List<Attribute> attributes)
		{
			base.ProcessSelfAttributes(property, attributes);
			attributes.Add(new PropertySpaceAttribute(-4));
		}
	}
}
