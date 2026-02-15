using System;

namespace Fusumity.Attributes
{
	/// <summary>
	/// Решает вопрос вложенных элементов в структурах по типу: Pack, Range и т.д
	/// Только для отображения
	/// </summary>
	public interface IAttributeConvertible
	{
		Attribute Convert();
	}
}
