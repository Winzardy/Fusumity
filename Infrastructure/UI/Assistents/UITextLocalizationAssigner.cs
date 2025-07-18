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
	/// _assigner.Place(placeholder, locKey)
	/// </summary>
	/// <remarks>Важно! любые tags, tagsWithFunc (Dictionary) которые попадают в аргументы сами улетают в статический пул!</remarks>
	public class UITextLocalizationAssigner : IDisposable
	{
		//Чтобы для случаев с одиночным переводом на аллоцировать целый Dictionary
		private (TMP_Text placeholder, LocText args) _single;
		private HashMap<TMP_Text, LocText> _placeholderToArgs;

		public event Action<TMP_Text> Updated;

		public UITextLocalizationAssigner()
		{
			LocManager.CurrentLocaleCodeUpdated += OnCurrentLocaleCodeUpdated;
		}

		public void Dispose()
		{
			LocManager.CurrentLocaleCodeUpdated -= OnCurrentLocaleCodeUpdated;

			if (_single.placeholder)
			{
				Release(ref _single.args);
				_single = default;
			}

			if (_placeholderToArgs.IsNullOrEmpty())
				return;

			foreach (var args in _placeholderToArgs.Keys)
				Release(ref _placeholderToArgs[args]);

			_placeholderToArgs.ReleaseToStaticPool();
			_placeholderToArgs = null;
		}

		public void Assign(TMP_Text placeholder, in LocText text)
		{
			if (!placeholder)
			{
				GUIDebug.LogError($"{DEBUG_PREFIX} Placeholder is null!");
				return;
			}

			if (_single.placeholder != null)
			{
				if (_single.placeholder == placeholder)
				{
					Release(ref _single.args);

					_single.args = text;
					ForceUpdateInternal(placeholder);
					return;
				}
			}
			else
			{
				_single = (placeholder, text);
				ForceUpdateInternal(placeholder);
				return;
			}

			_placeholderToArgs ??= HashMapPool<TMP_Text, LocText>.Get();

			if (_placeholderToArgs.Contains(placeholder))
				Release(ref _placeholderToArgs[placeholder]);

			_placeholderToArgs.SetOrAdd(placeholder, in text);

			ForceUpdateInternal(placeholder);
			placeholder.text = text.ToString();
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

			if (!_placeholderToArgs.Contains(placeholder))
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
				sArgs.tagToValue[tag] = value;
				ForceUpdateInternal(placeholder);
				return;
			}

			if (_placeholderToArgs == null)
				return;

			if (!_placeholderToArgs.Contains(placeholder))
			{
				GUIDebug.LogWarning($"{DEBUG_PREFIX}Not found arguments for placeholder!", placeholder);
				return;
			}

			_placeholderToArgs[placeholder]
			   .tagToValue[tag] = value;
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
				AssignInternal(_single.placeholder, _single.args);
				return;
			}

			if (!_placeholderToArgs.Contains(placeholder))
			{
				GUIDebug.LogWarning("Not found arguments for placeholder!", placeholder);
				return;
			}

			AssignInternal(placeholder, in _placeholderToArgs[placeholder]);
		}

		private void AssignInternal(TMP_Text placeholder, in LocText text)
		{
			placeholder.text = text.ToString();
			Updated?.Invoke(placeholder);
		}

		private void Release(ref LocText args)
		{
			args.tagToFunc?.ReleaseToStaticPool();
			args.tagToFunc = null;

			args.tagToValue?.ReleaseToStaticPool();
			args.tagToValue = null;
		}

		private static readonly string DEBUG_PREFIX = $"[ {nameof(UITextLocalizationAssigner)} ]";
	}

	public static class UITextLocalizationAssignerExtensions
	{
		public static void SetText(this UITextLocalizationAssigner assigner, TMP_Text placeholder, string key)
		{
			assigner.Assign(placeholder, new LocText(key));
		}

		public static void SetCompositeText(this UITextLocalizationAssigner assigner, TMP_Text placeholder, CompositeLocText args)
		{
			assigner.Assign(placeholder, args);
		}

		public static void SetFormatText(this UITextLocalizationAssigner assigner, TMP_Text placeholder, string key, params object[] args)
		{
			assigner.Assign(placeholder, new LocText(key)
			{
				args = args
			});
		}

		public static void SetText(this UITextLocalizationAssigner assigner, TMP_Text placeholder, string key, string tag,
			Func<object> value)
		{
			assigner.Assign(placeholder, new LocText(key, tag, value));
		}

		public static void SetText(this UITextLocalizationAssigner assigner, TMP_Text placeholder, string key, string tag, object value)
		{
			assigner.Assign(placeholder, new LocText(key, tag, value));
		}

		public static void SetText(this UITextLocalizationAssigner assigner, TMP_Text placeholder, string key,
			params (string name, Func<object> value)[] tags)
		{
			assigner.Assign(placeholder, new LocText(key, tags));
		}

		public static void SetText(this UITextLocalizationAssigner assigner, TMP_Text placeholder, string key,
			params (string name, object value)[] tags)
		{
			assigner.Assign(placeholder, new LocText(key, tags));
		}
	}
}
