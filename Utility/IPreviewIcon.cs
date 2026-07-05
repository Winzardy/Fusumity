using UnityEngine;

namespace Sapientia
{
	/// <summary>
	/// Иконка для превью в редакторских списках/пикерах контента (селектор <see cref="ContentReferenceAttribute"/> и т.п.)
	/// Можно повесить либо на <see cref="IContentEntrySource"/> (config ScriptableObject), либо на сам Value
	/// (тип T содержимого записи). Если реализован в обоих местах — используется иконка от IContentEntrySource,
	/// а в лог пишется предупреждение о конфликте (см. Content.Editor.ContentEntryIconUtility)
	/// </summary>
	public interface IPreviewIcon
	{
		Sprite PreviewIcon { get; }
	}
}
