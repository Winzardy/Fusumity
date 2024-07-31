using System;
using System.Diagnostics;

namespace Fusumity.Attributes.Odin
{
	/// <summary>
	/// <para>Minimum is used on primitive fields. It caps value of the field to a minimum value.</para>
	/// <para>Use this to define a minimum value for the field.</para>
	/// </summary>
	/// <remarks>
	/// <note type="note">Note that this attribute only works in the editor! Values changed from scripting will not be capped at a minimum.</note>
	/// </remarks>
	/// <example>
	/// <para>The following example shows a player component that must have at least 1 life.</para>
	/// <code>
	/// public class Player : MonoBehaviour
	/// {
	///		// The life value must be set to at least 1.
	///		[Minimum(1)]
	///		public int Life;
	/// }
	/// </code>
	/// </example>
	/// <example>
	/// <para>The following example shows how Minimum can be combined with <see cref="MaximumAttribute"/></para>
	/// <code>
	/// public class Health : MonoBehaviour
	/// {
	///		// The health value must be between 0 and 100.
	///		[Minimum(0), Maximum(100)]
	///		public float Health;
	/// }
	/// </code>
	/// </example>
	/// <seealso cref="MaximumAttribute"/>
	[AttributeUsage(AttributeTargets.All, AllowMultiple = true, Inherited = true)]
	[Conditional("UNITY_EDITOR")]
	public class MinimumAttribute : Attribute
	{
		/// <summary>
		/// The minimum value for the property.
		/// </summary>
		public double MinValue;

		/// <summary>
		/// The string with which to resolve a minimum value. This could be a field, property or method name, or an expression.
		/// </summary>
		public string Expression;

		/// <summary>
		/// Sets a minimum value for the property in the inspector.
		/// </summary>
		/// <param name="minValue">The minimum value.</param>
		public MinimumAttribute(double minValue)
		{
			this.MinValue = minValue;
		}

		/// <summary>
		/// Sets a minimum value for the property in the inspector.
		/// </summary>
		/// <param name="expression">The string with which to resolve a minimum value. This could be a field, property or method name, or an expression.</param>
		public MinimumAttribute(string expression)
		{
			this.Expression = expression;
		}
	}
}