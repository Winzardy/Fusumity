using System;
using System.Collections.Generic;
using Sapientia.Collections;
using Sapientia.Extensions;
using Sapientia.Pooling;

namespace Localizations
{
	// Cделал классом чтобы в dictionary не боксился
	// TODO: переделать в struct так как появился HashMap (аля Dictionary для struct) для работы со структурами
	// или пулить?
	public partial class TextLocalizationArgs
	{
		/// <summary>
		/// Ключ локали
		/// </summary>
		public string key;

		/// <summary>
		/// Обычные аргументы для string.Format
		/// </summary>
		public object[] args;

		/// <summary>
		/// Когда в переводе есть теги, например:
		/// Вы получили предмет {name}!
		/// {name} - тег
		/// </summary>
		public Dictionary<string, object> tags;

		/// <summary>
		/// Когда в переводе есть теги, например:
		/// Вы получили предмет {name}!
		/// {name} - тег
		/// Тут используется функция, решает кейс когда значение тоже переведено
		/// и его нужно перевести при изменении языка
		/// </summary>
		public Dictionary<string, Func<object>> tagsWithFunc;

		public bool autoReturnToPool;

		public bool trim;

		/// <summary>
		/// <see cref="String.ToUpper()"/>
		/// </summary>
		public bool upperCase;

		public string defaultValue;

		public CompositeTextLocalizationArgs composite;

		public TextLocalizationArgs()
		{
		}

		public TextLocalizationArgs(string key, string tag, object value)
		{
			this.key = key;
			tags = DictionaryPool<string, object>.Get();
			tags[tag] = value;
		}

		public TextLocalizationArgs(string key, params (string tag, object value)[] tags)
		{
			this.key = key;
			this.tags = DictionaryPool<string, object>.Get();
			foreach (var (tag, value) in tags)
				this.tags[tag] = value;
		}

		public override string ToString()
		{
			if (composite)
				return composite.ToString();

			var text = Localization.Get(key, defaultValue);

			if (text.IsNullOrEmpty())
				return text;

			if (args.IsNullOrEmpty() &&
			    tags.IsNullOrEmpty() &&
			    tagsWithFunc.IsNullOrEmpty())
				return upperCase ? text.ToUpper() : text;

			using (StringBuilderPool.Get(out var builder))
			{
				if (!args.IsNullOrEmpty())
					builder.AppendFormat(text, args);
				else
					builder.Append(text);

				if (!tags.IsNullOrEmpty())
				{
					foreach (var pair in tags)
						builder.Replace(pair.Key, pair.Value.ToString());

					if (autoReturnToPool)
						tags.ReleaseToStaticPool();
				}

				if (!tagsWithFunc.IsNullOrEmpty())
				{
					foreach (var pair in tagsWithFunc)
						builder.Replace(pair.Key, pair.Value?.Invoke().ToString());

					if (autoReturnToPool)
						tagsWithFunc.ReleaseToStaticPool();
				}

				var str = builder.ToString();

				if (upperCase)
					str = str.ToUpper();

				if (trim)
					str = str.Trim();

				return str;
			}
		}

		public static implicit operator TextLocalizationArgs(string key) => new() {key = key};
	}

	public class CompositeTextLocalizationArgs
	{
		public string separator;
		public string format;

		public TextLocalizationArgs[] args;

		public override string ToString()
		{
			using (StringBuilderPool.Get(out var builder))
			{
				foreach (var (x, i) in args.WithIndexSafe())
				{
					var text = !format.IsNullOrEmpty() ? format.Format(x.ToString()) : x.ToString();
					builder.Append(text);

					if (!separator.IsNullOrEmpty())
						continue;

					if (args.Length <= 1)
						continue;

					if (i != args.Length - 1)
						builder.Append(separator);
				}

				return builder.ToString();
			}
		}

		public static implicit operator bool(CompositeTextLocalizationArgs args) => args != null;

		public static implicit operator TextLocalizationArgs(CompositeTextLocalizationArgs args) => new()
		{
			composite = args
		};
	}
}
