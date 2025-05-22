using System;
using System.Diagnostics;

namespace Fusumity.Attributes
{
	/// <summary>
	/// Решает вопрос вложенных элементов в структурах по типу: Pack, Range и т.д
	/// Только для отображения
	/// </summary>
	[Conditional("UNITY_EDITOR")]
	public abstract class ParentAttribute : Attribute
	{
		public abstract Attribute Convert();
	}
}
