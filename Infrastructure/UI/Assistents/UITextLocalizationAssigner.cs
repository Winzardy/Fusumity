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
	// TODO: мини проблема, что в выключенных виджетах при изменений языка дергается так же обновление TMPs,
	// хотя на самом деле это не нужно, потому что возможно верстка удалится вообще и данное действие было лишним
	// сжиранием ресурсом (байтоебство)
	// Нужно эти Assigner присоединять к виджетам как ребенка. Чтобы у них были такие же дефолтные методы OnShow/OnHide
	// Только хочется убрать у них возможность иметь своих детей (children) и всю эту локику, как у виджета

	/// <summary>
	/// Отдельный контроллер, который занимается доставкой перевода из локализации в 'tmp'
	/// Обрабатывает сложные кейсы с аргументами, ключ-парой {tag} - value
	///
	/// После использование нужно обязательно вызвать Dispose
	///
	/// Так же, обновит перевод для 'tmp' если язык поменяли!
	///
	/// _assigner.Assign(tmp, locKey)
	/// </summary>
	/// <remarks>Важно! любые tags, tagsWithFunc (Dictionary) которые попадают в аргументы сами улетают в статический пул!</remarks>
	public class UITextLocalizationAssigner : IDisposable
	{
		//Чтобы для случаев с одиночным переводом на аллоцировать целый Dictionary
		private (TMP_Text tmp, LocText text) _single;
		private HashMap<TMP_Text, LocText> _tmpToText;

		public event Action<TMP_Text> Updated;

		public UITextLocalizationAssigner()
		{
			LocManager.CurrentLocaleCodeUpdated += OnCurrentLocaleCodeUpdated;
		}

		public void Dispose()
		{
			LocManager.CurrentLocaleCodeUpdated -= OnCurrentLocaleCodeUpdated;

			if (_single.tmp)
			{
				Release(ref _single.text);
				_single = default;
			}

			if (_tmpToText.IsNullOrEmpty())
				return;

			foreach (var args in _tmpToText.Keys)
				Release(ref _tmpToText[args]);

			_tmpToText.ReleaseToStaticPool();
			_tmpToText = null;
		}

		public void Assign(TMP_Text tmp, in LocText text)
		{
			if (!tmp)
			{
				GUIDebug.LogError($"{DEBUG_PREFIX} TMP is null!");
				return;
			}

			if (_single.tmp != null)
			{
				if (_single.tmp == tmp)
				{
					Release(ref _single.text);

					_single.text = text;
					ForceUpdateInternal(tmp);
					return;
				}
			}
			else
			{
				_single = (tmp: tmp, text);
				ForceUpdateInternal(tmp);
				return;
			}

			_tmpToText ??= HashMapPool<TMP_Text, LocText>.Get();

			if (_tmpToText.Contains(tmp))
				Release(ref _tmpToText[tmp]);

			_tmpToText.SetOrAdd(tmp, in text);

			ForceUpdateInternal(tmp);
			tmp.text = text.ToString();
		}

		public void ForceUpdate(TMP_Text tmp)
		{
			if (_single.tmp == tmp)
			{
				ForceUpdateInternal(tmp);
				return;
			}

			if (_tmpToText == null)
				return;

			if (!_tmpToText.Contains(tmp))
			{
				GUIDebug.LogWarning($"{DEBUG_PREFIX} Not found arguments for tmp!", tmp);
				return;
			}

			ForceUpdateInternal(tmp);
		}

		public void UpdateTag(TMP_Text tmp, string tag, string value)
		{
			if (_single.tmp == tmp)
			{
				var sArgs = _single.text;
				sArgs.tagToValue[tag] = value;
				ForceUpdateInternal(tmp);
				return;
			}

			if (_tmpToText == null)
				return;

			if (!_tmpToText.Contains(tmp))
			{
				GUIDebug.LogWarning($"{DEBUG_PREFIX}Not found arguments for tmp!", tmp);
				return;
			}

			_tmpToText[tmp]
			   .tagToValue[tag] = value;
			ForceUpdateInternal(tmp);
		}

		public void TryClear(TMP_Text tmp, bool useDeactivation = false)
		{
			if (!tmp)
				return;

			if (useDeactivation)
				tmp.SetActive(false);

			if (_single.tmp == tmp)
			{
				_single.tmp = null;
				_single.text = null;
				return;
			}

			if (_tmpToText.IsNullOrEmpty())
				return;

			_tmpToText.Remove(tmp);
		}

		private void OnCurrentLocaleCodeUpdated(string _)
		{
			if (_single.tmp)
				ForceUpdateInternal(_single.tmp);

			if (!_tmpToText.IsNullOrEmpty())
			{
				foreach (var tmp in _tmpToText.Keys)
				{
					ForceUpdateInternal(tmp);
				}
			}
		}

		private void ForceUpdateInternal(TMP_Text tmp)
		{
			if (_single.tmp == tmp)
			{
				AssignInternal(_single.tmp, _single.text);
				return;
			}

			if (!_tmpToText.Contains(tmp))
			{
				GUIDebug.LogWarning("Not found arguments for tmp!", tmp);
				return;
			}

			AssignInternal(tmp, in _tmpToText[tmp]);
		}

		private void AssignInternal(TMP_Text tmp, in LocText text)
		{
			tmp.text = text.ToString();
			Updated?.Invoke(tmp);
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
		public static void SetText(this UITextLocalizationAssigner assigner, TMP_Text tmp, string key)
		{
			assigner.Assign(tmp, new LocText(key));
		}

		public static void SetCompositeText(this UITextLocalizationAssigner assigner, TMP_Text tmp, CompositeLocText args)
		{
			assigner.Assign(tmp, args);
		}

		public static void SetFormatText(this UITextLocalizationAssigner assigner, TMP_Text tmp, string key, params object[] args)
		{
			assigner.Assign(tmp, new LocText(key)
			{
				args = args
			});
		}

		public static void SetText(this UITextLocalizationAssigner assigner, TMP_Text tmp, string key, string tag,
			Func<object> value)
		{
			assigner.Assign(tmp, new LocText(key, tag, value));
		}

		public static void SetText(this UITextLocalizationAssigner assigner, TMP_Text tmp, string key, string tag, object value)
		{
			assigner.Assign(tmp, new LocText(key, tag, value));
		}

		public static void SetText(this UITextLocalizationAssigner assigner, TMP_Text tmp, string key,
			params (string name, Func<object> value)[] tags)
		{
			assigner.Assign(tmp, new LocText(key, tags));
		}

		public static void SetText(this UITextLocalizationAssigner assigner, TMP_Text tmp, string key,
			params (string name, object value)[] tags)
		{
			assigner.Assign(tmp, new LocText(key, tags));
		}
	}
}
