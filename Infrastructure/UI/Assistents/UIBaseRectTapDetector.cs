using System;
using System.Collections.Generic;
using Fusumity.Utility;
using InputManagement;
using UnityEngine;

namespace UI
{
	using UnityRectTransformUtility = UnityEngine.RectTransformUtility;

	/// <summary>
	/// Детектор тапа по области (rect)
	/// </summary>
	public class UIBaseRectTapDetector : IDisposable
	{
		private IInputReader _inputReader;

		private HashSet<RectTransform> _rects = new();

		private TouchPhase _detectPhase;

		private Action _insideCallback;
		private Action _outsideCallback;

		private bool _subscribed;
		private bool _active = true;

		public UIBaseRectTapDetector(
			IInputReader inputReader,
			RectTransform rect,
			Action outOfBoundsCallback = null,
			Action inBoundsCallback = null,
			TouchPhase phase = TouchPhase.Began,
			bool autoActivation = true,
			params RectTransform[] rects) : this(rect, outOfBoundsCallback, inBoundsCallback, phase, autoActivation, rects)
		{
			SetInputReader(inputReader);
		}

		public UIBaseRectTapDetector(
			RectTransform rect,
			Action outOfBoundsCallback = null,
			Action inBoundsCallback = null,
			TouchPhase phase = TouchPhase.Began,
			bool autoActivation = true,
			params RectTransform[] rects)
		{
			Add(rect);
			Add(rects);

			_detectPhase = phase;

			_outsideCallback = outOfBoundsCallback;
			_insideCallback = inBoundsCallback;

			if (!autoActivation)
				SetActive(false);
		}

		public void Dispose()
		{
			TryClearInputReader();

			_rects = null;

			SetActive(false);
		}

		public void SetActive(bool active)
		{
			_active = active;

			if (_inputReader == null)
				return;

			if (_active)
				SubscribeSafe();
			else
				UnsubscribeSafe();
		}

		public void Add(params RectTransform[] rects)
		{
			foreach (var rect in rects)
				_rects.Add(rect);
		}

		public void Remove(params RectTransform[] rects)
		{
			foreach (var rect in rects)
				_rects.Remove(rect);
		}

		protected void SetInputReader(IInputReader inputReader)
		{
			TryClearInputReader();

			_inputReader = inputReader;

			if (_active)
				SubscribeSafe();
		}

		private void TryClearInputReader()
		{
			if (_inputReader != null)
				UnsubscribeSafe();
		}

		private void OnTap(TapInfo info)
		{
			if (!_active || info.touchPhase != _detectPhase)
				return;

			if (Contains(info.position))
			{
				_insideCallback?.Invoke();
			}
			else
			{
				_outsideCallback?.Invoke();
			}
		}

		private bool Contains(Vector2 position)
		{
			foreach (var rect in _rects)
			{
				if (!rect.gameObject.IsActive())
					continue;

				if (UnityRectTransformUtility.RectangleContainsScreenPoint(rect, position))
					return true;
			}

			return false;
		}

		private void SubscribeSafe()
		{
			if (_subscribed)
				return;

			_inputReader.Tapped += OnTap;
			_subscribed = true;
		}

		private void UnsubscribeSafe()
		{
			if (!_subscribed)
				return;

			_inputReader.Tapped -= OnTap;
			_subscribed = false;
		}
	}
}
