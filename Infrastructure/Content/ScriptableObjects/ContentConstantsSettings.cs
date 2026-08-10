using System;

namespace Content.ScriptableObjects
{
	/// <summary>
	/// Настройки генерации констант конкретного контент-ассета. Живут на самом ассете
	/// (в отличие от проектных ContentConstantGeneratorSettings), потому что что именно
	/// генерировать решает не тип, а конкретная запись — см. <see cref="ContentScriptableObject.UseConstants"/>
	/// </summary>
	/// <remarks>Редакторные атрибуты — в ContentConstantsSettingsAttributeProcessor</remarks>
	[Serializable]
	public struct ContentConstantsSettings
	{
		public bool useConstants;

		/// <summary>
		/// Пусто — имя возьмётся из Id ассета ("{Id}Keys")
		/// </summary>
		public string className;

		/// <summary>
		/// Пусто — по умолчанию "Content.Constants"
		/// </summary>
		public string @namespace;

		/// <summary>
		/// Пусто — по умолчанию Assets/Scripts/Content/Constants
		/// </summary>
		public string outputPath;

		/// <summary>
		/// Слепок содержимого и настроек на момент последней генерации: по расхождению с текущим
		/// дровер понимает, что константы устарели, и подсвечивает кнопку
		/// </summary>
		public string generatedHash;
	}

	/// <summary>
	/// Одна генерируемая константа. Тип поля выводится из <see cref="value"/>: string или int
	/// </summary>
	public readonly struct ContentConstantEntry
	{
		/// <summary>
		/// Исходный текст для имени константы (нормализуется в SNAKE_CASE генератором)
		/// </summary>
		public readonly string name;

		/// <summary>
		/// Значение константы: string либо int
		/// </summary>
		public readonly object value;

		/// <summary>
		/// Путь группы для комментария-заголовка, например "hair" у ключа "hair/00"
		/// </summary>
		public readonly string groupPath;

		public readonly string summary;

		public ContentConstantEntry(string name, object value, string groupPath = null, string summary = null)
		{
			this.name = name;
			this.value = value;
			this.groupPath = groupPath;
			this.summary = summary;
		}
	}
}
