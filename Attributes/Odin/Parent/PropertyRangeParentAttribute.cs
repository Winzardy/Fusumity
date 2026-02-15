using System;
using System.Diagnostics;
using Sirenix.OdinInspector;

namespace Fusumity.Attributes
{
	[Conditional("UNITY_EDITOR")]
	public class PropertyRangeParentAttribute : Attribute, IAttributeConvertible
	{
		/// <summary>The minimum value.</summary>
		public double Min;

		/// <summary>The maximum value.</summary>
		public double Max;

		/// <summary>
		/// A resolved string that should evaluate to a float value, and will be used as the min bounds.
		/// </summary>
		public string MinGetter;

		/// <summary>
		/// A resolved string that should evaluate to a float value, and will be used as the max bounds.
		/// </summary>
		public string MaxGetter;

		/// <summary>
		/// Creates a slider control to set the value of the property to between the specified range..
		/// </summary>
		/// <param name="min">The minimum value.</param>
		/// <param name="max">The maximum value.</param>
		public PropertyRangeParentAttribute(double min, double max)
		{
			this.Min = min < max ? min : max;
			this.Max = max > min ? max : min;
		}

		/// <summary>
		/// Creates a slider control to set the value of the property to between the specified range..
		/// </summary>
		/// <param name="minGetter">A resolved string that should evaluate to a float value, and will be used as the min bounds.</param>
		/// <param name="max">The maximum value.</param>
		public PropertyRangeParentAttribute(string minGetter, double max)
		{
			this.MinGetter = minGetter;
			this.Max = max;
		}

		/// <summary>
		/// Creates a slider control to set the value of the property to between the specified range..
		/// </summary>
		/// <param name="min">The minimum value.</param>
		/// <param name="maxGetter">A resolved string that should evaluate to a float value, and will be used as the max bounds.</param>
		public PropertyRangeParentAttribute(double min, string maxGetter)
		{
			this.Min = min;
			this.MaxGetter = maxGetter;
		}

		/// <summary>
		/// Creates a slider control to set the value of the property to between the specified range..
		/// </summary>
		/// <param name="minGetter">A resolved string that should evaluate to a float value, and will be used as the min bounds.</param>
		/// <param name="maxGetter">A resolved string that should evaluate to a float value, and will be used as the max bounds.</param>
		public PropertyRangeParentAttribute(string minGetter, string maxGetter)
		{
			this.MinGetter = minGetter;
			this.MaxGetter = maxGetter;
		}

		public Attribute Convert()
			=> new PropertyRangeAttribute(Min, Max)
			{
				MinGetter = MinGetter,
				MaxGetter = MaxGetter
			};
	}
}
