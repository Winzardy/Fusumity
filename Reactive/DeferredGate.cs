using System;
using UnityEngine;

namespace Fusumity.Reactive
{
	/// <summary>
	/// Окно после возвращения приложения из фона: сначала задержка, потом лимит на кадр.
	/// Нужно, чтобы накопившаяся за время в фоне работа не легла в один кадр (борьба с ANR)
	/// </summary>
	public class DeferredGate : IDisposable
	{
		private const float DEFAULT_DELAY = 0.5f;
		private const float DEFAULT_WINDOW_TIME = 5f;
		private const int DEFAULT_PER_FRAME = 4;

		/// <summary>
		/// Задержка перед тем как начать разбирать накопленное, считается от возвращения из фона (в секундах)
		/// </summary>
		public float delay;

		/// <summary>
		/// Сколько после возвращения из фона действует лимит (в секундах)
		/// </summary>
		public float windowTime;

		/// <summary>
		/// Сколько элементов пропускаем за кадр
		/// </summary>
		public int perFrame;

		private float _openTime;
		private float _closeTime;

		private int _frameNumber;
		private int _passedInFrame;

		private bool _closing;

		/// <summary>
		/// Окно закрылось — накопленное нужно разобрать немедленно
		/// </summary>
		public event Action Closed;

		/// <summary>
		/// Лимит сейчас действует
		/// </summary>
		public bool IsOpen { get => Time.realtimeSinceStartup < _closeTime; }

		/// <summary>
		/// Задержка после возвращения из фона уже прошла
		/// </summary>
		public bool IsHoldElapsed { get => Time.realtimeSinceStartup - _openTime >= delay; }

		public DeferredGate(float delay = DEFAULT_DELAY, float windowTime = DEFAULT_WINDOW_TIME, int perFrame = DEFAULT_PER_FRAME)
		{
			this.delay = delay;
			this.windowTime = windowTime;
			this.perFrame = perFrame;

			UnityLifecycle.ApplicationResumeEvent += Open;

			// Уходим в фон: обновлений больше не будет, поэтому просим разобрать накопленное сразу.
			// Потеря фокуса приходит раньше паузы, так что хватает её одной
			UnityLifecycle.ApplicationUnfocusEvent += Close;
			UnityLifecycle.ApplicationShutdown += Close;
		}

		public void Dispose()
		{
			UnityLifecycle.ApplicationResumeEvent -= Open;

			UnityLifecycle.ApplicationUnfocusEvent -= Close;
			UnityLifecycle.ApplicationShutdown -= Close;

			Closed = null;
		}

		public void Open()
		{
			_openTime = Time.realtimeSinceStartup;
			_closeTime = _openTime + windowTime;
		}

		public void Close()
		{
			// Подписчик в ответ на закрытие сливает накопленное и может сам позвать Close
			if (_closing)
				return;

			_closing = true;

			try
			{
				_closeTime = 0f;
				Closed?.Invoke();
			}
			finally
			{
				_closing = false;
			}
		}

		/// <summary>
		/// Занять место в бюджете кадра
		/// </summary>
		public bool TryTakeBudget()
		{
			var frame = Time.frameCount;
			if (_frameNumber != frame)
			{
				_frameNumber = frame;
				_passedInFrame = 0;
			}

			if (_passedInFrame >= perFrame)
				return false;

			_passedInFrame++;
			return true;
		}
	}
}
