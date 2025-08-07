using System;
using System.Diagnostics;

namespace Fusumity.Attributes.Odin
{
	/// <summary>
	/// Инлайнит значение для единственного поля отрисовки
	/// </summary>
	[AttributeUsage(AttributeTargets.All, AllowMultiple = false, Inherited = true)]
	[Conditional("UNITY_EDITOR")]
	public class SingleFieldInlineAttribute : Attribute {}
}
