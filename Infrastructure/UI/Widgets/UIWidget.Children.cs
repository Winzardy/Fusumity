using System;
using System.Collections.Generic;
using Sapientia.Collections;
using Sapientia.Pooling;
using UnityEngine;

namespace UI
{
	/// <summary>
	/// Cтроительный блок-кирпичик, который может иметь внутри себя такие же вложенные widget<br/>
	/// Widget = Controller (MVC family) название взято для удобствоа, чтобы при использовании
	/// было сразу ясно что идет речь именно о UI контроллере<br/>
	/// </summary>
	public abstract partial class UIWidget
	{
		protected HashSet<UIWidget> _children;

		public IEnumerable<UIWidget> Children => _children;

		private void DisposeChildren()
		{
			DisposeAndClearChildren();

			StaticObjectPoolUtility.ReleaseAndSetNullSafe(ref _children);
		}

		protected void SetActiveInHierarchyForChildren(bool value)
		{
			if (_children.IsNullOrEmpty())
				return;

			foreach (var child in _children)
				child.SetActiveInHierarchy(value);
		}

		protected void DisposeAndClearChildren()
		{
			if (_children.IsNullOrEmpty())
				return;

			foreach (var child in _children)
			{
				OnChildDispose(child);
				child.Dispose();
			}

			_children.Clear();
		}

		protected virtual void OnChildDispose(UIWidget child)
		{
		}

		/// <summary>
		/// Создает виджет если Layout != null, устанавливает верстку в этот виджет и активирует (опционально)
		/// <br/>
		/// Регистрирует созданный виджет ребенком (<see cref="RegisterChildWidget{TLayout}"/>)
		/// </summary>
		protected T CreateWidgetByLayoutSafe<T, TLayout>(out T widget,
			TLayout layout,
			bool autoActivation = false,
			bool autoInitialization = true,
			bool immediateActivation = true)
			where T : UIWidget<TLayout>
			where TLayout : UIBaseLayout
		{
			widget = null;

			if (!layout)
				return widget;

			widget = CreateWidget<T, TLayout>(layout, autoActivation, autoInitialization, immediateActivation);
			return widget;
		}

		//TODO: подумать над лучшим названием и поведением...
		/// <summary>
		/// Создает виджет если он не создан далее пытается создать виджет по Layout <see cref="CreateWidgetByLayoutSafe{T,TLayout}"/>
		/// <br/>
		/// Регистрирует созданный виджет ребенком (<see cref="RegisterChildWidget{TLayout}"/>)
		/// </summary>
		protected bool TryCreateWidget<T, TLayout>(ref T widget,
			TLayout layout,
			bool autoActivation = false,
			bool autoInitialization = true,
			bool immediateActivation = true)
			where T : UIWidget<TLayout>
			where TLayout : UIBaseLayout
		{
			if (widget != null)
				return true;

			return CreateWidgetByLayoutSafe(out widget, layout, autoActivation, autoInitialization, immediateActivation);
		}

		/// <summary>
		/// Создает виджет, устанавливает верстку в этот виджет и активирует (опционально)
		/// <br/>
		/// Регистрирует созданный виджет ребенком (<see cref="RegisterChildWidget{TLayout}"/>)
		/// </summary>
		protected T CreateWidget<T, TLayout>(out T widget,
			TLayout layout,
			bool autoActivation = false,
			bool autoInitialization = true,
			bool immediateActivation = true)
			where T : UIWidget<TLayout>
			where TLayout : UIBaseLayout
		{
			widget = CreateWidget<T, TLayout>(layout, autoActivation, autoInitialization, immediateActivation);
			return widget;
		}

		/// <summary>
		/// Создает виджет, создает новую верстку, устанавливает в виджет и активирует (опционально)
		/// <br/>
		/// Регистрирует созданный виджет ребенком (<see cref="RegisterChildWidget{TLayout}"/>)
		/// </summary>
		protected T CreateWidgetByTemplate<T, TLayout>(out T widget,
			TLayout template,
			bool autoActivation = true,
			bool autoInitialization = true,
			bool immediateActivation = true)
			where T : UIWidget<TLayout>
			where TLayout : UIBaseLayout
		{
			return CreateWidgetByTemplate(out widget, template, null, autoActivation, autoInitialization,
				immediateActivation);
		}

		/// <summary>
		/// Создает виджет, создает новую верстку, устанавливает в виджет и активирует (опционально)
		/// <br/>
		/// Регистрирует созданный виджет ребенком (<see cref="RegisterChildWidget{TLayout}"/>)
		/// </summary>
		protected T CreateWidgetByTemplate<T, TLayout>(
			TLayout template,
			bool autoActivation = true,
			bool autoInitialization = true,
			bool immediateActivation = true)
			where T : UIWidget<TLayout>
			where TLayout : UIBaseLayout
		{
			return CreateWidgetByTemplate<T, TLayout>(template, null, autoActivation, autoInitialization,
				immediateActivation);
		}

		/// <summary>
		/// Создает виджет, создает новую верстку, устанавливает в виджет и активирует (опционально)
		/// <br/>
		/// Регистрирует созданный виджет ребенком (<see cref="RegisterChildWidget{TLayout}"/>)
		/// </summary>
		/// <param name="parent">Куда засунуть новый инстанс, если не назначен, автоматически устанавливает root родителя<br/>
		/// Если задан "parent" вне виджета то удаление верстки лежит на плечах разработчика
		/// </param>
		protected T CreateWidgetByTemplate<T, TLayout>(out T widget,
			TLayout template,
			RectTransform parent,
			bool autoActivation = true,
			bool autoInitialization = true,
			bool immediateActivation = true)
			where T : UIWidget<TLayout>
			where TLayout : UIBaseLayout
		{
			widget = CreateWidgetByTemplate<T, TLayout>(template, parent, autoActivation, autoInitialization,
				immediateActivation);
			return widget;
		}

		/// <summary>
		/// Создает виджет, создает новую верстку, устанавливает в виджет и активирует (опционально)
		/// <br/>
		/// Регистрирует созданный виджет ребенком (<see cref="RegisterChildWidget{TLayout}"/>)
		/// </summary>
		/// <param name="parent">Куда засунуть новый инстанс, если не назначен, автоматически устанавливает root родителя<br/>
		/// Если задан "parent" вне виджета то удаление верстки лежит на плечах разработчика
		/// </param>
		protected T CreateWidgetByTemplate<T, TLayout>(
			TLayout template,
			RectTransform parent,
			bool autoActivation = true,
			bool autoInitialization = true,
			bool immediateActivation = true)
			where T : UIWidget<TLayout>
			where TLayout : UIBaseLayout
		{
			var layout = CreateLayoutByTemplate(template, parent);
			return CreateWidget<T, TLayout>(layout, autoActivation, autoInitialization, immediateActivation);
		}

		/// <summary>
		/// Cоздает новую верстку по шаблону
		/// </summary>
		/// <param name="template">Шаблон (префаб у обычных работяг)</param>
		/// <param name="parent">Куда засунуть новый инстанс, если не назначен, автоматически устанавливает root родителя<br/>
		/// Если задан "parent" вне виджета то удаление верстки лежит на плечах разработчика
		/// </param>
		/// <typeparam name="TLayout">Тип верстки</typeparam>
		/// <returns></returns>
		protected TLayout CreateLayoutByTemplate<TLayout>(TLayout template, RectTransform parent = null)
			where TLayout : UIBaseLayout
		{
			if (!parent)
				parent = RectTransform;

			return UIFactory.CreateLayout(template, parent);
		}

		/// <summary>
		/// Создает виджет, устанавливает верстку в этот виджет и активирует (опционально)
		/// <br/>
		/// Регистрирует созданный виджет ребенком (<see cref="RegisterChildWidget{TLayout}"/>)
		/// </summary>
		protected T CreateWidget<T, TLayout>(
			TLayout layout,
			bool autoActivation = true,
			bool autoInitialization = true,
			bool immediateActivation = true)
			where T : UIWidget<TLayout>
			where TLayout : UIBaseLayout
		{
			var widget = CreateRawWidget<T>(false, autoInitialization);

			RegisterChildWidget(widget);

#if UNITY_EDITOR
			if (this is IWidget<TLayout> parent && parent.Layout == layout)
				throw new Exception("Trying to set parent layout to widget");
#endif

			widget.SetupLayout(layout);

			if (autoActivation)
				widget.SetActive(true, immediateActivation);

			return widget;
		}

		/// <summary>
		/// Создать только виджет без верстки
		/// </summary>
		protected void CreateRawWidget<T>(out T widget, bool autoRegister = true, bool autoInitialization = false)
			where T : UIWidget
		{
			widget = CreateRawWidget<T>(autoRegister, autoInitialization);
		}

		/// <summary>
		/// Создать только виджет без верстки
		/// </summary>
		protected T CreateRawWidget<T>(bool autoRegister = true, bool autoInitialization = false)
			where T : UIWidget
		{
			var widget = UIFactory.CreateWidget<T>(autoInitialization);

			if (autoRegister)
				RegisterChildWidget(widget);

			return widget;
		}

		/// <summary>
		/// Регистрирует виджет в качестве ребенка:<br/>
		/// - Вызывает <paramref name="widget"/>.<see cref="Dispose"/> при собественном <see cref="Dispose"/> <br/>
		/// - Вызывает <paramref name="widget"/>.<see cref="Dispose"/> при очистки верстки! <br/><br/>
		/// Важно, сам корневой виджет остается жить, но при очистке верстки, он вызовет Dispose у своих детей (childs)!
		/// Есть возможность использовать изберательную логику при вызове OnLayoutCleared у детей (childs), но это усложняет
		/// и удорожает простую очистку верстки, лучше просто дольше держать активной ту верстку которая нужна по логике.
		/// (подробнее в <see cref="LayoutEntry"/>)
		/// </summary>
		protected internal void RegisterChildWidget<TLayout>(UIWidget<TLayout> widget, bool autoActivation,
			bool immediateActivation = true)
			where TLayout : UIBaseLayout
		{
			RegisterChildWidget(widget);

			if (autoActivation)
				widget.SetActive(true, immediateActivation);
		}

		/// <summary>
		/// Регистрирует виджет в качестве ребенка:<br/>
		/// - Вызывает <paramref name="widget"/>.<see cref="Dispose"/> при собественном <see cref="Dispose"/> <br/>
		/// - Вызывает <paramref name="widget"/>.<see cref="Dispose"/> при очистки верстки
		/// Важно, сам корневой виджет остается жить, но при очистке верстки, он вызовет Dispose у своих детей (childs)!
		/// Есть возможность использовать изберательную логику при вызове OnLayoutCleared у детей (childs), но это усложняет
		/// и удорожает простую очистку верстки, лучше просто дольше держать активной ту верстку которая нужна по логике.
		/// (подробнее в <see cref="LayoutEntry"/>)
		/// </summary>
		internal void RegisterChildWidget(UIWidget widget, bool soft = false)
		{
			_children ??= HashSetPool<UIWidget>.Get();

			if (!_children.Add(widget))
			{
				if (!soft)
					GUIDebug.LogError("Attempt to add a child that already exists!");
				return;
			}

			widget.SetActiveInHierarchy(_activeInHierarchy);
			widget.SetActiveInHierarchyForChildren(_activeInHierarchy);

			OnChildWidgetRegistered(widget);
		}

		protected virtual void OnChildWidgetRegistered(UIWidget child)
		{
			child.SetLayer(Layer);
		}

		protected internal void SetLayer(string layer) => Layer = layer;

		internal void UnregisterChildWidget(UIWidget widget)
		{
			if (_children.IsNullOrEmpty())
				return;

			if (!_children.Remove(widget))
				GUIDebug.LogError("Attempt to remove a child that already not exists!");
		}
	}
}
