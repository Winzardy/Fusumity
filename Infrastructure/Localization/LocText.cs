using System;
using System.Collections.Generic;
using Sapientia.Collections;
using Sapientia.Extensions;
using Sapientia.Pooling;

namespace Localization
{
	public struct LocText
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
		public Dictionary<string, object> tagToValue;

		/// <summary>
		/// Когда в переводе есть теги, например:
		/// Вы получили предмет {name}!
		/// {name} - тег
		/// Тут используется функция, решает кейс когда значение тоже переведено
		/// и его нужно перевести при изменении языка
		/// </summary>
		public Dictionary<string, Func<object>> tagToFunc;

		public bool trim;

		/// <summary>
		/// <see cref="String.ToUpper()"/>
		/// </summary>
		public bool upperCase;

		public string defaultValue;

		public CompositeLocText composite;

		public LocText(string key) : this()
		{
			this.key = key;
		}

		public LocText(string key, string tag, object value) : this(key)
		{
			this.key = key;
			tagToValue = DictionaryPool<string, object>.Get();
			tagToValue[tag] = value;
		}

		public LocText(string key, params (string tag, object value)[] tagToValue) : this(key)
		{
			this.tagToValue = DictionaryPool<string, object>.Get();
			foreach (var (tag, value) in tagToValue)
				this.tagToValue[tag] = value;
		}

		public LocText(string key, string tag, Func<object> func) : this(key)
		{
			this.key = key;
			tagToFunc = DictionaryPool<string, Func<object>>.Get();
			tagToFunc[tag] = func;
		}

		public LocText(string key, params (string tag, Func<object>)[] tagToFunc) : this(key)
		{
			this.tagToFunc = DictionaryPool<string, Func<object>>.Get();
			foreach (var (tag, value) in tagToFunc)
				this.tagToFunc[tag] = value;
		}

		public readonly bool IsEmpty() => LocUtility.IsEmptyKey(key);

		public override string ToString()
		{
			if (composite)
				return composite.ToString();

			if (key.IsNullOrEmpty())
				return defaultValue;

			var text = LocManager.Get(key, defaultValue);

			if (text.IsNullOrEmpty())
				return string.Empty;

			if (args.IsNullOrEmpty() &&
			    tagToValue.IsNullOrEmpty() &&
			    tagToFunc.IsNullOrEmpty())
				return upperCase ? text.ToUpper() : text;

			using (StringBuilderPool.Get(out var builder))
			{
				if (!args.IsNullOrEmpty())
					builder.AppendFormat(text, args);
				else
					builder.Append(text);

				if (!tagToValue.IsNullOrEmpty())
				{
					foreach (var pair in tagToValue)
						builder.Replace(pair.Key, pair.Value.ToString());
				}

				if (!tagToFunc.IsNullOrEmpty())
				{
					foreach (var pair in tagToFunc)
						builder.Replace(pair.Key, pair.Value?.Invoke()?.ToString());
				}

				var str = builder.ToString();

				if (upperCase)
					str = str.ToUpper();

				if (trim)
					str = str.Trim();

				return str;
			}
		}

		public static implicit operator LocText(string key) => new(key);
		public static implicit operator LocText(LocKey key) => new(key);
		public static implicit operator bool(in LocText args) => args.IsEmpty();
	}

	public class CompositeLocText
	{
		public string separator;
		public string format;

		public LocText[] args;

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

		public static implicit operator bool(CompositeLocText args) => args != null;

		public static implicit operator LocText(CompositeLocText args) => new(string.Empty)
		{
			composite = args
		};
	}
}
