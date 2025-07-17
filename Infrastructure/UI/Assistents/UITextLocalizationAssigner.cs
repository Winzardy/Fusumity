using System;
using System.Collections.Generic;
using Fusumity.Utility;
using Localization;
using Sapientia.Collections;
using Sapientia.Pooling;
using TMPro;

namespace UI
{
	// TODO: добавить пуллинг TextLocalizationArgsPool...
	// TODO: мини проблема, что в выключенных виджетах при изменений языка дергается так же обновление placeholders,
	// хотя на самом деле это не нужно, потому что возможно верстка удалится вообще и данное действие было лишним
	// сжиранием ресурсом (байтоебство)
	// Нужно эти Assigner присоединять к виджетам как ребенка. Чтобы у них были такие же дефолтные методы OnShow/OnHide
	// Только хочется убрать у них возможность иметь своих детей (children) и всю эту локику, как у виджета

	/// <summary>
	/// Отдельный контроллер, который занимается доставкой перевода из локализации в 'placeholder'
	/// Обрабатывает сложные кейсы с аргументами, ключ-парой {tag} - value
	///
	/// После использование нужно обязательно вызвать Dispose
	///
	/// Так же, обновит перевод для 'placeholder' если язык поменяли!
	///
	/// _assigner.SetText(placeholder, locKey)
	/// </summary>
	/// <remarks>Важно! любые tags, tagsWithFunc (Dictionary) которые попадают в аргументы сами улетают в статический пул!</remarks>
	public class UITextLocalizationAssigner : IDisposable
	{
		//Чтобы для случаев с одиночным переводом на аллоцировать целый Dictionary
		private (TMP_Text placeholder, TextLocalizationArgs args) _single;
		private Dictionary<TMP_Text, TextLocalizationArgs> _placeholderToArgs;

		public event Action<TMP_Text> Updated;

		public UITextLocalizationAssigner()
		{
			LocManager.CurrentLocaleCodeUpdated += OnCurrentLocaleCodeUpdated;
		}

		public void Dispose()
		{
			LocManager.CurrentLocaleCodeUpdated -= OnCurrentLocaleCodeUpdated;

			ClearSafe(ref _single.args);
			if (!_placeholderToArgs.IsNullOrEmpty())
			{
				foreach (var args in _placeholderToArgs.Values)
				{
					Clear(args);
					args.args = null;
				}

				_placeholderToArgs.ReleaseToStaticPool();
				_placeholderToArgs = null;
			}
		}

		public void SetText(TMP_Text placeholder, string key)
		{
			SetText(placeholder, new TextLocalizationArgs
			{
				key = key
			});
		}

		public void SetCompositeText(TMP_Text placeholder, CompositeTextLocalizationArgs args)
		{
			SetText(placeholder, args);
		}

		public void SetFormatText(TMP_Text placeholder, string key, params object[] args)
		{
			SetText(placeholder, new TextLocalizationArgs
			{
				key = key, args = args,
			});
		}

		public void SetText(TMP_Text placeholder, string key, string tag, Func<object> value)
		{
			var dictionary = DictionaryPool<string, Func<object>>.Get();

			dictionary[tag] = value;

			var args = new TextLocalizationArgs
			{
				key = key, tagsWithFunc = dictionary
			};

			SetText(placeholder, args);
		}

		public void SetText(TMP_Text placeholder, string key, string tag, object value)
		{
			var dictionary = DictionaryPool<string, object>.Get();
			dictionary[tag] = value;

			var args = new TextLocalizationArgs
			{
				key = key, tags = dictionary
			};

			SetText(placeholder, args);
		}

		public void SetText(TMP_Text placeholder, string key, params (string name, Func<object> value)[] tags)
		{
			var args = new TextLocalizationArgs
			{
				key = key,
			};

			// TODO: не понимаю где возвращение в пул... не критично, но надо будет проследить и пофиксить.
			// Утечки нет так как пулл отдает объект и не хранит его, но все же
			args.tagsWithFunc ??= DictionaryPool<string, Func<object>>.Get();

			foreach (var info in tags)
			{
				args.tagsWithFunc[info.name] = info.value;
			}

			SetText(placeholder, args);
		}

		public void SetText(TMP_Text placeholder, string key, params (string name, object value)[] tags)
		{
			var args = new TextLocalizationArgs
			{
				key = key,
			};

			args.tags ??= DictionaryPool<string, object>.Get();

			foreach (var info in tags)
			{
				args.tags[info.name] = info.value;
			}

			SetText(placeholder, args);
		}

		public void SetText(TMP_Text placeholder, TextLocalizationArgs args)
		{
			if (!placeholder)
			{
				GUIDebug.LogError($"{DEBUG_PREFIX} Placeholder is null!");
				return;
			}

			if (args == null)
			{
				GUIDebug.LogError($"{DEBUG_PREFIX} LocalizationArgs is null!", placeholder);
				return;
			}

			if (_single.placeholder != null)
			{
				if (_single.placeholder == placeholder)
				{
					ClearSafe(ref _single.args);

					_single.args = args;
					ForceUpdateInternal(placeholder);
					return;
				}
			}
			else
			{
				_single = (placeholder, args);
				ForceUpdateInternal(placeholder);
				return;
			}

			_placeholderToArgs ??= DictionaryPool<TMP_Text, TextLocalizationArgs>.Get();

			if (_placeholderToArgs.TryGetValue(placeholder, out var prevArgs))
				Clear(prevArgs);

			_placeholderToArgs[placeholder] = args;

			ForceUpdateInternal(placeholder);
			placeholder.text = args.ToString();
		}

		public void ForceUpdate(TMP_Text placeholder)
		{
			if (_single.placeholder == placeholder)
			{
				ForceUpdateInternal(placeholder);
				return;
			}

			if (_placeholderToArgs == null)
				return;

			if (!_placeholderToArgs.TryGetValue(placeholder, out var args))
			{
				GUIDebug.LogWarning($"{DEBUG_PREFIX} Not found arguments for placeholder!", placeholder);
				return;
			}

			ForceUpdateInternal(placeholder);
		}

		public void UpdateTag(TMP_Text placeholder, string tag, string value)
		{
			if (_single.placeholder == placeholder)
			{
				var sArgs = _single.args;
				sArgs.tags[tag] = value;
				ForceUpdateInternal(placeholder);
				return;
			}

			if (_placeholderToArgs == null)
				return;

			if (!_placeholderToArgs.TryGetValue(placeholder, out var args))
			{
				GUIDebug.LogWarning($"{DEBUG_PREFIX}Not found arguments for placeholder!", placeholder);
				return;
			}

			args.tags[tag] = value;
			ForceUpdateInternal(placeholder);
		}

		public void TryClear(TMP_Text placeholder, bool useDeactivation = false)
		{
			if (!placeholder)
				return;

			if (useDeactivation)
				placeholder.SetActive(false);

			if (_single.placeholder == placeholder)
			{
				_single.placeholder = null;
				_single.args = null;
				return;
			}

			if (_placeholderToArgs.IsNullOrEmpty())
				return;

			_placeholderToArgs.Remove(placeholder);
		}

		private void OnCurrentLocaleCodeUpdated(string _)
		{
			if (_single.placeholder)
				ForceUpdateInternal(_single.placeholder);

			if (!_placeholderToArgs.IsNullOrEmpty())
			{
				foreach (var placeholder in _placeholderToArgs.Keys)
				{
					ForceUpdateInternal(placeholder);
				}
			}
		}

		private void ForceUpdateInternal(TMP_Text placeholder)
		{
			if (_single.placeholder == placeholder)
			{
				SetTextInternal(_single.placeholder, _single.args);
				return;
			}

			if (!_placeholderToArgs.TryGetValue(placeholder, out var args))
			{
				GUIDebug.LogWarning("Not found arguments for placeholder!", placeholder);
				return;
			}

			SetTextInternal(placeholder, args);
		}

		private void SetTextInternal(TMP_Text placeholder, TextLocalizationArgs args)
		{
			placeholder.text = args.ToString();
			Updated?.Invoke(placeholder);
		}

		private void ClearSafe(ref TextLocalizationArgs args)
		{
			if (args == null)
				return;

			Clear(args);

			args = null;
		}

		private void Clear(TextLocalizationArgs args)
		{
			args.tagsWithFunc?.ReleaseToStaticPool();
			args.tagsWithFunc = null;

			args.tags?.ReleaseToStaticPool();
			args.tags = null;
		}

		private static readonly string DEBUG_PREFIX = $"[ {nameof(UITextLocalizationAssigner)} ]";
	}
}
