using System;
using Fusumity.Attributes.Odin;
using Sirenix.OdinInspector.Editor;
using Sirenix.OdinInspector.Editor.ValueResolvers;
using Sirenix.Utilities.Editor;
using UnityEngine;

namespace Fusumity.Editor.Drawers
{
	public sealed class MaximumDrawer<T> : OdinAttributeDrawer<MaximumAttribute, T>
		where T : struct
	{
		private static readonly bool IsNumber = GenericNumberUtility.IsNumber(typeof(T));
		private static readonly bool IsVector = GenericNumberUtility.IsVector(typeof(T));

		private ValueResolver<double> maxValueGetter;

		public override bool CanDrawTypeFilter(Type type)
		{
			return IsNumber || IsVector;
		}

		protected override void Initialize()
		{
			this.maxValueGetter = ValueResolver.Get<double>(this.Property, this.Attribute.Expression, this.Attribute.MaxValue);
		}

		protected override void DrawPropertyLayout(GUIContent label)
		{
			if (this.maxValueGetter.HasError)
			{
				SirenixEditorGUI.ErrorMessageBox(this.maxValueGetter.ErrorMessage);
				this.CallNextDrawer(label);
			}
			else
			{
				this.CallNextDrawer(label);

				T value = this.ValueEntry.SmartValue;
				var max = this.maxValueGetter.GetValue();

				if (!GenericNumberUtility.NumberIsInRange(value, double.MinValue, max))
				{
					this.ValueEntry.SmartValue = GenericNumberUtility.Clamp(value, double.MinValue, max);
				}
			}
		}
	}
}