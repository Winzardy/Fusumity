using System;
using System.Diagnostics;

namespace Fusumity.Attributes.Odin
{
	/// <summary>
	/// <para>ByteSize is used on integer fields holding a size in bytes. It draws the value as three
	/// sub-fields — MB / KB / B — packed into the single field: value = mb * 1024^2 + kb * 1024 + b.</para>
	/// <para>The attribute does not clamp the value — combine it with
	/// <see cref="MinimumAttribute"/>/<see cref="MaximumAttribute"/> for range constraints.</para>
	/// </summary>
	/// <example>
	/// <code>
	/// public class Budget : MonoBehaviour
	/// {
	///		// Draws as "[64] MB [0] KB [0] B" and stores 67108864.
	///		[ByteSize, Minimum(0)]
	///		public long maxResidentBytes = 64L * 1024L * 1024L;
	/// }
	/// </code>
	/// </example>
	/// <seealso cref="MinimumAttribute"/>
	/// <seealso cref="MaximumAttribute"/>
	[AttributeUsage(AttributeTargets.All, Inherited = true)]
	[Conditional("UNITY_EDITOR")]
	public class ByteSizeAttribute : Attribute
	{
	}
}
