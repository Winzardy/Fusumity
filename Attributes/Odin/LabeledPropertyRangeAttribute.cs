using System;
using System.Diagnostics;

namespace Fusumity.Attributes
{
	[AttributeUsage(AttributeTargets.All, AllowMultiple = false, Inherited = true)]
	[Conditional("UNITY_EDITOR")]
	public class LabeledPropertyRangeAttribute : Attribute
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

		public string MinLabel;
		public string MaxLabel;

		/// <summary>
		/// The name of a field, property or method to get the min value from. Obsolete; use the MinGetter member instead.
		/// </summary>
		[Obsolete("Use the MinGetter member instead.", false)]
		public string MinMember { get => this.MinGetter; set => this.MinGetter = value; }

		/// <summary>
		/// The name of a field, property or method to get the max value from. Obsolete; use the MaxGetter member instead.
		/// </summary>
		[Obsolete("Use the MaxGetter member instead.", false)]
		public string MaxMember { get => this.MaxGetter; set => this.MaxGetter = value; }

		/// <summary>
		/// Creates a slider control to set the value of the property to between the specified range..
		/// </summary>
		/// <param name="min">The minimum value.</param>
		/// <param name="max">The maximum value.</param>
		public LabeledPropertyRangeAttribute(double min, double max, string minLabel = null, string maxLabel = null)
		{
			this.Min = min < max ? min : max;
			this.Max = max > min ? max : min;
			MinLabel = minLabel;
			MaxLabel = maxLabel;
		}

		/// <summary>
		/// Creates a slider control to set the value of the property to between the specified range..
		/// </summary>
		/// <param name="minGetter">A resolved string that should evaluate to a float value, and will be used as the min bounds.</param>
		/// <param name="max">The maximum value.</param>
		public LabeledPropertyRangeAttribute(string minGetter, double max, string minLabel = null, string maxLabel = null)
		{
			this.MinGetter = minGetter;
			this.Max = max;
			MinLabel = minLabel;
			MaxLabel = maxLabel;
		}

		/// <summary>
		/// Creates a slider control to set the value of the property to between the specified range..
		/// </summary>
		/// <param name="min">The minimum value.</param>
		/// <param name="maxGetter">A resolved string that should evaluate to a float value, and will be used as the max bounds.</param>
		public LabeledPropertyRangeAttribute(double min, string maxGetter, string minLabel = null, string maxLabel = null)
		{
			this.Min = min;
			this.MaxGetter = maxGetter;
			MinLabel = minLabel;
			MaxLabel = maxLabel;
		}

		/// <summary>
		/// Creates a slider control to set the value of the property to between the specified range..
		/// </summary>
		/// <param name="minGetter">A resolved string that should evaluate to a float value, and will be used as the min bounds.</param>
		/// <param name="maxGetter">A resolved string that should evaluate to a float value, and will be used as the max bounds.</param>
		public LabeledPropertyRangeAttribute(string minGetter, string maxGetter, string minLabel = null, string maxLabel = null)
		{
			this.MinGetter = minGetter;
			this.MaxGetter = maxGetter;
			MinLabel = minLabel;
			MaxLabel = maxLabel;
		}
	}
}
