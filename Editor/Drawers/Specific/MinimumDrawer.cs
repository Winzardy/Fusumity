using System;
using Fusumity.Attributes.Odin;
using Sirenix.OdinInspector.Editor;
using Sirenix.OdinInspector.Editor.ValueResolvers;
using Sirenix.Utilities.Editor;
using UnityEngine;

namespace Fusumity.Editor.Drawers.Specific
{
	[DrawerPriority(0, 9000, 0)]
	public class MinimumDrawer<T> : OdinAttributeDrawer<MinimumAttribute, T>
		where T : struct
	{
		private static readonly bool IsNumber = GenericNumberUtility.IsNumber(typeof(T));
		private static readonly bool IsVector = GenericNumberUtility.IsVector(typeof(T));

		private ValueResolver<double> minValueGetter;

		public override bool CanDrawTypeFilter(Type type)
		{
			return IsNumber || IsVector;
		}

		protected override void Initialize()
		{
			this.minValueGetter = ValueResolver.Get<double>(this.Property, this.Attribute.Expression, this.Attribute.MinValue);
		}

		protected override void DrawPropertyLayout(GUIContent label)
		{
			if (this.minValueGetter.HasError)
			{
				SirenixEditorGUI.ErrorMessageBox(this.minValueGetter.ErrorMessage);
				this.CallNextDrawer(label);
			}
			else
			{
				this.CallNextDrawer(label);

				T value = this.ValueEntry.SmartValue;
				var min = this.minValueGetter.GetValue();

				if (!GenericNumberUtility.NumberIsInRange(value, min, double.MaxValue))
				{
					this.ValueEntry.SmartValue = GenericNumberUtility.Clamp(value, min, double.MaxValue);
				}
			}
		}
	}
}