﻿using System;
using System.ComponentModel;
using System.Threading;
using Sapientia.Collections;
using Sapientia.Extensions;
using UnityEngine;

namespace UI
{
	public interface IWidget : IDisposable
	{
		/// <summary>
		///Когда виджет активирован (начало анимации - начало закрывание)
		/// </summary>
		public bool Active { get; }

		/// <summary>
		///Когда виджет вообще виден на экране (начало анимации - конец анимации)
		/// </summary>
		public bool Visible { get; }

		/// <summary>
		/// Когда виджет проиграл анимацию открытия и уже полностью открыт
		/// </summary>
		public bool Open { get; }

		public void Initialize()
		{
		}

		public void Reset()
		{
		}

		public void Refresh()
		{
		}

		public void SetActive(bool active, bool immediate = false, bool useCacheImmediate = true)
		{
		}

		protected internal void SetVisible(bool value)
		{
		}

		public bool IsActive() => Active;
		public bool IsVisible() => Visible;
		public bool IsOpen() => Open;
	}

	/// <summary>
	/// Cтроительный блок-кирпичик, который может иметь внутри себя такие же вложенные widget<br/>
	/// Widget = Controller (MVC family) название взято для удобствоа, чтобы при использовании
	/// было сразу ясно что идет речь именно о UI контроллере<br/>
	/// </summary>
	public abstract partial class UIWidget : CompositeDisposable, IWidget
	{
		protected const string INITIALIZE_OVERRIDE_EXCEPTION_MESSAGE_FORMAT =
			"Initialization override for widget by type [ {0} ]";

		protected const string INITIALIZE_OVERRIDE_EXCEPTION_MESSAGE = "Initialization override for widget";

		private CancellationTokenSource _disposeCts;
		protected bool _activeInHierarchy = true;

		private bool _active;
		protected bool _immediate;

		/// <inheritdoc cref="IWidget{TLayout}.Active"/>
		public bool Active => _active;

		/// <inheritdoc cref="IWidget{TLayout}.Visible"/>
		public virtual bool Visible => Active;

		/// <inheritdoc cref="IWidget{TLayout}.Visible"/>
		public virtual bool Open => Active;

		protected virtual bool UseCustomReset => false;

		protected internal abstract RectTransform Root { get; }

		public string Layer { get; protected set; }

		/// <summary>
		/// Internal <br/><br/>
		///
		/// Чтобы Intellisense не отображал в Rider'е метод, нужно включить фильтрацию по этому аттрибуту:<br/>
		/// Settings -> Editor -> General -> Code Completion -> Filter members by [EditorBrowsable]
		/// </summary>
		//TODO: посмотреть удобно ли это в итоге, хотелось бы просто этот метод не светил перед глазами
		[EditorBrowsable(EditorBrowsableState.Advanced)]
		public abstract void Initialize();

		public sealed override void Dispose()
		{
			base.Dispose();
			OnDisposeInternal();
		}

		public virtual void Reset(bool deactivate = true)
		{
			if (!UseCustomReset)
				if (_children.Any())
					foreach (var child in _children)
						child.Reset(deactivate);

			OnReset(deactivate);
		}

		protected virtual void OnReset(bool deactivate)
		{
		}

		private protected virtual void OnDisposeInternal()
		{
			DisposeChildren();

			_disposeCts?.Trigger();
			_disposeCts = null;

			GC.SuppressFinalize(this);
		}

		public void SetActive(bool active, bool immediate = false, bool useCacheImmediate = true)
		{
			if (useCacheImmediate)
				_immediate = immediate;

			if (Active == active)
				return;

			_active = active;

			if (!_activeInHierarchy)
				return;

			SetActiveInternal(active, immediate);
		}

		protected internal virtual void SetActiveInternal(bool active, bool immediate)
		{
			if (active)
			{
				SetActiveInHierarchyForChildren(true);
				OnShow();
			}
			else
			{
				OnHide();
				SetActiveInHierarchyForChildren(false);
			}
		}

		private void SetActiveInHierarchy(bool active)
		{
			if (_activeInHierarchy == active)
				return;

			var prevActiveInHierarchy = _activeInHierarchy;

			if (!active && Active && prevActiveInHierarchy)
				SetActiveInternal(false, true);

			_activeInHierarchy = active;

			if (active && Active && !prevActiveInHierarchy)
				SetActiveInternal(true, true);
		}

		/// <summary>
		/// Вызывается при активации виджета (<see cref="SetActive"/> и только если есть верстка (<see cref="UIBaseLayout"/>)
		/// </summary>
		protected virtual void OnShow()
		{
		}

		/// <summary>
		/// Вызывается при деактивации виджета (<see cref="SetActive"/>)
		/// </summary>
		protected virtual void OnHide()
		{
		}

		protected CancellationTokenSource DisposeCancellationTokenSource
		{
			get
			{
				_disposeCts ??= new CancellationTokenSource();
				return _disposeCts;
			}
		}

		protected CancellationToken DisposeCancellationToken => DisposeCancellationTokenSource.Token;

		/// <inheritdoc cref="IWidget{TLayout}.Active"/>
		public bool IsActive() => Active;

		/// <inheritdoc cref="IWidget{TLayout}.Visible"/>
		public bool IsVisible() => Visible;

		/// <inheritdoc cref="IWidget{TLayout}.Open"/>
		public bool IsOpen() => Open;

		public static implicit operator bool(UIWidget widget) => widget != null;
	}
}
