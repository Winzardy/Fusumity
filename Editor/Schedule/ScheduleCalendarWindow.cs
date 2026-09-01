using System;
using System.Collections.Generic;
using System.Globalization;
using Sapientia;
using Sapientia.Extensions;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Fusumity.Editor.Utility;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.UIElements;

namespace Fusumity.Editor
{
	/// <summary>
	/// Календарь расписания: точки <see cref="ScheduleScheme"/> закодированы в long, и по списку
	/// кодов не видно, на какие реальные даты они лягут. Окно раскладывает их по календарю,
	/// расшифровывает правила сбоку и позволяет завести новую точку кликом по дню
	/// </summary>
	/// <remarks>
	/// Отдельное окно, а не блок в инспекторе: инспектор Odin — это IMGUI, UIToolkit в него
	/// не встроить
	/// </remarks>
	public class ScheduleCalendarWindow : EditorWindow
	{
		private const int WEEKS = 6;
		private const int DAYS_IN_WEEK = 7;
		private const int HOURS_IN_DAY = 24;

		/// <summary>
		/// Потолок вхождений одной точки за период: от кривого кода можно получить бесконечный шаг
		/// </summary>
		private const int OCCURRENCES_LIMIT = 512;

		/// <summary>Как часто календарь сверяет схему с показанным снимком, мс</summary>
		private const long SYNC_INTERVAL = 250;

		private const ulong FNV_BASIS = 14695981039346656037;
		private const ulong FNV_PRIME = 1099511628211;

		private const int COMPACT_DOTS_LIMIT = 4;
		private const float SIDEBAR_WIDTH = 250f;
		private const float HOUR_GUTTER = 46f;
		private const float HOUR_HEIGHT = 26f;
		private const float DAY_HEIGHT = 44f;
		private const float COMPACT_DAY_HEIGHT = 26f;
		private const string TIME_FORMAT = "HH:mm";
		private const string TITLE = "Schedule Calendar";

		private static readonly string[] WEEK_DAYS = {"Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun"};

		/// <summary>Полные имена дней: неделя начинается с понедельника, как в кодировке Weekly</summary>
		private static readonly string[] WEEK_DAY_NAMES =
			{"Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday", "Sunday"};

		/// <summary>Enum.GetValues аллоцирует и боксит на каждый вызов — кэшируем один раз</summary>
		private static readonly SchedulePointKind[] KINDS =
			(SchedulePointKind[]) Enum.GetValues(typeof(SchedulePointKind));

		private static readonly Color BACKGROUND = new(0.22f, 0.22f, 0.22f);
		private static readonly Color OUT_OF_MONTH = new(0.17f, 0.17f, 0.17f);
		private static readonly Color BORDER = new(0.13f, 0.13f, 0.13f);
		private static readonly Color NEAREST = new(0.95f, 0.75f, 0.25f);
		private static readonly Color FOCUS = new(0.35f, 0.78f, 0.45f);
		private static readonly Color NOW = new(0.85f, 0.30f, 0.30f);

		private static readonly Color OVERLAY = new(0.16f, 0.16f, 0.16f, 0.95f);
		private static readonly Color OVERLAY_SELECTED = new(0.28f, 0.45f, 0.70f);
		private static readonly Color SIDEBAR = new(0.19f, 0.19f, 0.19f);
		private static readonly Color HOUR_LINE = new(0.15f, 0.15f, 0.15f);

		private InspectorProperty _property;

		private ViewMode _mode = ViewMode.Month;

		/// <summary>Опорный день: от него считается показанный период по текущему режиму</summary>
		private DateTime _anchor;

		/// <summary>
		/// «Сейчас» для календаря: от неё считается ближайшая точка. Отдельно от системного времени,
		/// чтобы можно было посмотреть расписание глазами будущей даты
		/// </summary>
		private DateTime _focus;

		private Label _rangeLabel;
		private Button _todayButton;
		private VisualElement _content;
		private VisualElement _sidebar;
		private VisualElement _rules;
		private Button _rulesButton;
		private Image _rulesIcon;
		private Label _footer;

		private bool _sidebarVisible = true;

		private static Texture2D _rulesIconNormal;
		private static Texture2D _rulesIconSelected;
		private static Texture2D _windowIcon;
		private static Texture2D _editIcon;

		// DontUnloadUnusedAsset переживает рекомпиляцию, а статические ссылки нет — без уборки
		// каждая пересборка кода оставляла бы в редакторе прошлый набор иконок
		static ScheduleCalendarWindow() => AssemblyReloadEvents.beforeAssemblyReload += ReleaseIcons;

		private readonly Dictionary<int, Button> _modeButtons = new();

		private DateTime? _nearest;

		/// <summary>
		/// Отпечаток схемы на момент последней перерисовки: по нему ловятся правки со стороны
		/// </summary>
		private ulong _signature;

		/// <summary>Длительности окон из схемы — параллельно точкам, снимок на момент Refresh</summary>
		private long[] _durations;

		/// <summary>Порядковый номер точки среди точек её типа — параллельно точкам, снимок на момент Refresh</summary>
		private int[] _ordinals;

		/// <summary>Сколько всего точек того же типа: метка «#N» нужна только группам из нескольких</summary>
		private int[] _kindTotals;

		/// <summary>Живые метки «сейчас»: их двигаем по таймеру, а не перестройкой окна</summary>
		private VisualElement _nowLine;

		private Label _nowChip;

		private int _nowHour = -1;

		/// <param name="pointsProperty">Свойство массива <see cref="ScheduleScheme.points"/></param>
		public static void Open(InspectorProperty pointsProperty)
		{
			if (pointsProperty == null)
				return;

			var window = GetWindow<ScheduleCalendarWindow>(utility: false, title: TITLE);
			window.titleContent = new GUIContent(TITLE, WindowIcon());
			window.minSize = new Vector2(760, 520);
			window._property = pointsProperty;
			window._focus = DateTime.UtcNow;
			window._anchor = window._focus.Date;
			window.Refresh();
			window.Show();
		}

		private void OnEnable()
		{
			Undo.undoRedoPerformed += SyncExternalChange;

			// Иконка ставится и здесь: после domain reload Open не вызывается,
			// а текстура прошлой сессии в titleContent уже мертва
			titleContent = new GUIContent(TITLE, WindowIcon());
		}

		private void OnDisable() => Undo.undoRedoPerformed -= SyncExternalChange;

		/// <summary>
		/// В фоне планировщик UIToolkit не тикает — на возврате сверяемся сразу, иначе окно
		/// показывает расписание на момент ухода
		/// </summary>
		private void OnFocus() => ReloadData();

		/// <summary>
		/// После undo или правки из окна точки дерево Odin держит прежние значения —
		/// без обновления календарь рисует то, чего в ассете уже нет
		/// </summary>
		private void SyncExternalChange()
		{
			// Через кадр: Odin успевает перечитать сериализацию таргета, иначе дерево отдаст
			// прежние значения и календарь перестроится по ним же
			EditorApplication.delayCall += () =>
			{
				if (this == null)
					return;

				ReloadData();
			};
		}

		private void CreateGUI()
		{
			// Окно переживает перезагрузку домена, а несериализуемые даты — нет: без этого сетка
			// встаёт на DateTime.MinValue, и листание назад бросает на каждом клике
			if (_anchor == default)
				_anchor = DateTime.UtcNow.Date;

			if (_focus == default)
				_focus = DateTime.UtcNow;

			var root = rootVisualElement;
			root.style.paddingLeft = 6;
			root.style.paddingRight = 6;
			root.style.paddingTop = 4;
			root.style.paddingBottom = 4;

			root.Add(BuildToolbar());

			var body = new VisualElement {style = {flexDirection = FlexDirection.Row, flexGrow = 1}};

			// Левая колонка: календарь и подпись к нему. Подпись именно здесь, а не под всем
			// окном — иначе она подрезает колонку правил по высоте
			var main = new VisualElement {style = {flexGrow = 1}};

			var scroll = new ScrollView {style = {flexGrow = 1}};

			// Без растяжки контейнера сетка держит только свою «естественную» высоту и
			// висит куском в верху окна
			_content = scroll.contentContainer;
			_content.style.flexGrow = 1;

			main.Add(scroll);

			var bottom = new VisualElement
			{
				style =
				{
					marginTop = 4,
					paddingLeft = 6,
					paddingRight = 6,
					paddingTop = 4,
					paddingBottom = 4,
					flexShrink = 0,
					backgroundColor = SIDEBAR
				}
			};

			SetRadius(bottom, 6);

			// Легенда слева, подсказка управления справа: ПКМ и двойной клик
			// в самом интерфейсе иначе никак не видны
			var info = new VisualElement
			{
				style = {flexDirection = FlexDirection.Row, alignItems = Align.Center}
			};

			info.Add(BuildLegend());
			info.Add(new VisualElement {style = {flexGrow = 1}});
			info.Add(BuildHints());
			bottom.Add(info);

			_footer = new Label
			{
				enableRichText = true,
				style =
				{
					color = Color.gray,
					fontSize = 11,
					marginTop = 2,
					whiteSpace = WhiteSpace.Normal
				}
			};

			bottom.Add(_footer);
			main.Add(bottom);

			body.Add(main);
			body.Add(BuildSidebar());
			root.Add(body);

			// «Сейчас» едет само: без тика линия замирает на времени открытия окна
			root.schedule.Execute(SyncNow).Every(20_000);

			// Точки правят и мимо календаря — из инспектора, из окна точки, скриптом, undo.
			// Уведомления есть не от каждого источника, поэтому календарь сам сверяет схему
			root.schedule.Execute(SyncData).Every(SYNC_INTERVAL);

			Refresh();
		}

		/// <summary>
		/// Двигает линию «сейчас» без перестройки. Перестройка нужна только на смене часа —
		/// линия переезжает в другую ячейку
		/// </summary>
		private void SyncNow()
		{
			if (_nowLine == null && _nowChip == null)
				return;

			var now = DateTime.UtcNow;

			if (now.Hour != _nowHour)
			{
				Refresh();
				return;
			}

			if (_nowLine != null)
			{
				_nowLine.style.top = Length.Percent(now.Minute / 60f * 100f);
				_nowLine.tooltip = NowTooltip(now);
			}

			if (_nowChip == null)
				return;

			_nowChip.text = now.ToString(TIME_FORMAT, CultureInfo.InvariantCulture);
			_nowChip.tooltip = NowTooltip(now);
		}

		private static string NowTooltip(DateTime now) => $"Сейчас: {now:yyyy-MM-dd HH:mm} UTC";

		/// <summary>
		/// Системные цвета сами по себе ничего не говорят — без подписи рамки читаются как декор
		/// </summary>
		private static VisualElement BuildLegend()
		{
			var legend = new VisualElement
			{
				style = {flexDirection = FlexDirection.Row, alignItems = Align.Center, marginBottom = 2}
			};

			legend.Add(Item(NOW, "now", "Системное время UTC", filled: true));
			legend.Add(Item(FOCUS, "focus",
				"Точка отсчёта календаря: от неё ищется ближайшее вхождение и от неё же " +
				"раскладывается Interval. Задаётся ПКМ по ячейке"));
			legend.Add(Item(NEAREST, "nearest from focus",
				"Первое вхождение строго после focus — то, что сработает следующим"));

			return legend;

			VisualElement Item(Color color, string text, string tooltip, bool filled = false)
			{
				var row = new VisualElement
				{
					tooltip = tooltip,
					style = {flexDirection = FlexDirection.Row, alignItems = Align.Center, marginRight = 10}
				};

				var swatch = new VisualElement
				{
					style =
					{
						width = 11,
						height = 11,
						marginRight = 4,
						backgroundColor = filled ? color : Color.clear
					}
				};

				// Заливка — у «сейчас», рамка — у focus и ближайшей: так же, как в самих ячейках
				if (!filled)
				{
					swatch.style.borderLeftWidth = swatch.style.borderRightWidth = 2;
					swatch.style.borderTopWidth = swatch.style.borderBottomWidth = 2;
					swatch.style.borderLeftColor = swatch.style.borderRightColor = color;
					swatch.style.borderTopColor = swatch.style.borderBottomColor = color;
				}

				SetRadius(swatch, 2);
				row.Add(swatch);
				row.Add(new Label(text) {style = {color = Color.gray, fontSize = 11}});

				return row;
			}
		}

		/// <summary>
		/// Подсказка управления — в духе превью кастомизации: контекстное меню и двойной клик
		/// в самом интерфейсе иначе никак не видны
		/// </summary>
		private static VisualElement BuildHints()
		{
			var row = new VisualElement
			{
				style = {flexDirection = FlexDirection.Row, alignItems = Align.Center, opacity = 0.55f, marginBottom = 2}
			};

			row.Add(MouseGlyph(right: true));
			row.Add(HintLabel("context menu — add, edit, focus", trailing: 14));
			row.Add(MouseGlyph(right: false));
			row.Add(HintLabel("double click — day view", trailing: 0));

			return row;
		}

		/// <summary>
		/// Мышь рисуется сама — как в превью кастомизации: готового глифа с подсвеченной
		/// нужной кнопкой нет
		/// </summary>
		private static VisualElement MouseGlyph(bool right)
		{
			var body = new VisualElement
			{
				style =
				{
					width = 9,
					height = 13,
					marginRight = 5,
					backgroundColor = new Color(1f, 1f, 1f, 0.45f)
				}
			};

			SetRadius(body, 3);

			var inner = new VisualElement
			{
				style =
				{
					position = Position.Absolute,
					left = 1,
					right = 1,
					top = 1,
					bottom = 1,
					backgroundColor = new Color(0f, 0f, 0f, 0.75f)
				}
			};

			SetRadius(inner, 2);
			body.Add(inner);

			var button = new VisualElement
			{
				style =
				{
					position = Position.Absolute,
					top = 1,
					width = 3,
					height = 5,
					backgroundColor = Color.white
				}
			};

			if (right)
				button.style.right = 1;
			else
				button.style.left = 1;

			body.Add(button);
			return body;
		}

		private static Label HintLabel(string text, float trailing) =>
			new(text) {style = {color = Color.gray, fontSize = 11, marginRight = trailing}};

		#region Toolbar

		private VisualElement BuildToolbar()
		{
			var toolbar = new VisualElement
			{
				style = {flexDirection = FlexDirection.Row, alignItems = Align.Center, marginBottom = 4}
			};

			// Боковые группы одной ширины: только так плашка режимов встаёт по центру окна,
			// а не по центру остатка после навигации
			var left = SideGroup(Justify.FlexStart);
			left.Add(BuildNav());

			_rangeLabel = new Label
			{
				style = {marginLeft = 6, unityFontStyleAndWeight = FontStyle.Bold}
			};
			left.Add(_rangeLabel);

			toolbar.Add(left);
			toolbar.Add(BuildModeOverlay());

			var right = SideGroup(Justify.FlexEnd);
			var rules = OverlayBar();

			_rulesButton = OverlayButton(string.Empty, ToggleSidebar, 28);
			_rulesButton.tooltip = "Показать/скрыть список правил";
			_rulesButton.style.alignItems = Align.Center;
			_rulesButton.style.justifyContent = Justify.Center;
			_rulesButton.style.paddingLeft = 0;
			_rulesButton.style.paddingRight = 0;

			// Иконка отдельным Image, а не backgroundImage: background-size переезжал между
			// версиями UIToolkit, а Image со ScaleToFit ведёт себя одинаково
			_rulesIcon = new Image
			{
				scaleMode = ScaleMode.ScaleToFit,
				style = {width = 14, height = 14}
			};
			_rulesButton.Add(_rulesIcon);

			rules.Add(_rulesButton);
			right.Add(rules);
			toolbar.Add(right);

			return toolbar;
		}

		private static VisualElement SideGroup(Justify justify) =>
			new()
			{
				style =
				{
					flexDirection = FlexDirection.Row,
					alignItems = Align.Center,
					justifyContent = justify,
					flexGrow = 1,
					flexBasis = 0,
					overflow = Overflow.Hidden
				}
			};

		/// <summary>«◀ Today ▶» — три самостоятельные кнопки-таблетки, как в календаре мака</summary>
		private VisualElement BuildNav()
		{
			var nav = new VisualElement
			{
				style = {flexDirection = FlexDirection.Row, alignItems = Align.Center}
			};

			nav.Add(Pill(Arrow(OverlayButton("◀", () => Shift(-1), 28))));

			// Возвращает показанный период к текущему. Focus не трогает — его ставят ПКМ
			// осознанно, и терять его от навигации обидно
			var today = OverlayButton("Today", () =>
			{
				_anchor = DateTime.UtcNow.Date;
				Refresh();
			});

			today.tooltip = "Показать текущий период (focus не меняется)";
			_todayButton = Pill(today);
			nav.Add(_todayButton);

			nav.Add(Pill(Arrow(OverlayButton("▶", () => Shift(1), 28))));

			return nav;

			// У каждой кнопки свой бокс вместо общей плашки; радиус — как у кнопок режимов,
			// высота фиксированная и общая, чтобы мелкий глиф стрелки не ужимал свою кнопку
			static Button Pill(Button button)
			{
				button.style.backgroundColor = OVERLAY;
				button.style.height = 22;
				button.style.marginLeft = 2;
				button.style.marginRight = 2;
				button.style.paddingTop = 0;
				button.style.paddingBottom = 0;

				return button;
			}

			// Только глиф меньше — сама кнопка остаётся той же высоты
			static Button Arrow(Button button)
			{
				button.style.fontSize = 9;
				button.style.unityTextAlign = TextAnchor.MiddleCenter;

				return button;
			}
		}

		private VisualElement BuildModeOverlay()
		{
			var bar = OverlayBar();

			foreach (var mode in new[] {ViewMode.Day, ViewMode.Week, ViewMode.Month, ViewMode.Year})
			{
				var captured = mode;
				var button = OverlayButton(mode.ToString(), () =>
				{
					_mode = captured;
					Refresh();
				});

				_modeButtons[(int) mode] = button;
				bar.Add(button);
			}

			return bar;
		}

		private void ToggleSidebar()
		{
			_sidebarVisible = !_sidebarVisible;
			SyncSidebar();
		}

		private void SyncSidebar()
		{
			if (_sidebar != null)
				_sidebar.style.display = _sidebarVisible ? DisplayStyle.Flex : DisplayStyle.None;

			if (_rulesButton == null)
				return;

			_rulesButton.style.backgroundColor = _sidebarVisible ? OVERLAY_SELECTED : Color.clear;

			if (_rulesIcon != null)
				_rulesIcon.image = RulesIcon(_sidebarVisible);
		}

		private static Texture2D RulesIcon(bool selected)
		{
			ref var cached = ref selected ? ref _rulesIconSelected : ref _rulesIconNormal;

			if (cached != null)
				return cached;

			var color = selected ? Color.white : new Color(0.78f, 0.78f, 0.78f);
			cached = SdfIcons.CreateTransparentIconTexture(SdfIconType.ListUl, color, 14, 14, 0);
			cached.hideFlags |= HideFlags.DontUnloadUnusedAsset;

			return cached;
		}

		private static Texture2D WindowIcon()
			=> Icon(ref _windowIcon, SdfIconType.CalendarWeek, 24);

		private static Texture2D EditIcon()
			=> Icon(ref _editIcon, SdfIconType.PencilFill, 12);

		private static Texture2D Icon(ref Texture2D cache, SdfIconType icon, int size)
		{
			if (cache != null)
				return cache;

			cache = SdfIcons.CreateTransparentIconTexture(icon, new Color(0.78f, 0.78f, 0.78f), size, size, 0);
			cache.hideFlags |= HideFlags.DontUnloadUnusedAsset;

			return cache;
		}

		private static void ReleaseIcons()
		{
			Release(ref _rulesIconNormal);
			Release(ref _rulesIconSelected);
			Release(ref _windowIcon);
			Release(ref _editIcon);
		}

		private static void Release(ref Texture2D icon)
		{
			if (icon != null)
				DestroyImmediate(icon);

			icon = null;
		}

		/// <summary>
		/// Плашка в духе Scene Overlay: скруглённая подложка, кнопки без своих рамок
		/// </summary>
		private static VisualElement OverlayBar()
		{
			var bar = new VisualElement
			{
				style =
				{
					flexDirection = FlexDirection.Row,
					backgroundColor = OVERLAY,
					paddingLeft = 2,
					paddingRight = 2,
					paddingTop = 2,
					paddingBottom = 2,
					marginLeft = 2,
					marginRight = 2
				}
			};

			SetRadius(bar, 7);
			return bar;
		}

		private static Button OverlayButton(string text, Action action, float width = 0)
		{
			var button = new Button(action) {text = text};

			button.style.marginLeft = 1;
			button.style.marginRight = 1;
			button.style.marginTop = 0;
			button.style.marginBottom = 0;
			button.style.paddingLeft = 8;
			button.style.paddingRight = 8;
			button.style.backgroundColor = Color.clear;
			button.style.borderLeftWidth = button.style.borderRightWidth = 0;
			button.style.borderTopWidth = button.style.borderBottomWidth = 0;
			button.style.color = new Color(0.78f, 0.78f, 0.78f);

			if (width > 0)
				button.style.width = width;

			SetRadius(button, 5);
			return button;
		}

		private static void SetRadius(VisualElement element, float radius)
		{
			element.style.borderTopLeftRadius = radius;
			element.style.borderTopRightRadius = radius;
			element.style.borderBottomLeftRadius = radius;
			element.style.borderBottomRightRadius = radius;
		}

		private void SyncModeButtons()
		{
			foreach (var pair in _modeButtons)
			{
				var selected = pair.Key == (int) _mode;
				pair.Value.style.backgroundColor = selected ? OVERLAY_SELECTED : Color.clear;
				pair.Value.style.color = selected ? Color.white : new Color(0.78f, 0.78f, 0.78f);
				pair.Value.style.unityFontStyleAndWeight = selected ? FontStyle.Bold : FontStyle.Normal;
			}
		}

		#endregion

		#region Sidebar

		private VisualElement BuildSidebar()
		{
			var sidebar = new ScrollView
			{
				style =
				{
					width = SIDEBAR_WIDTH,
					marginLeft = 4,
					paddingLeft = 4,
					paddingRight = 4,
					paddingTop = 4,
					paddingBottom = 4,
					backgroundColor = SIDEBAR
				}
			};

			SetRadius(sidebar, 6);

			_rules = sidebar.contentContainer;
			_sidebar = sidebar;
			SyncSidebar();

			return sidebar;
		}

		private void RefreshRules(SchedulePoint[] points, List<Occurrence> occurrences)
		{
			_rules.Clear();

			var byKind = new Dictionary<SchedulePointKind, List<int>>();
			var invalid = new List<int>();

			for (var i = 0; i < points.Length; i++)
			{
				if (!TryGetKind(points[i], out var kind))
				{
					invalid.Add(i);
					continue;
				}

				if (!byKind.TryGetValue(kind, out var list))
					byKind[kind] = list = new List<int>();

				list.Add(i);
			}

			foreach (var kind in KINDS)
			{
				if (!byKind.TryGetValue(kind, out var indices))
					continue;

				var foldout = BuildGroup($"{kind} ({indices.Count})");

				foreach (var index in indices)
					foldout.Add(BuildRule(index, points[index], kind, occurrences));

				_rules.Add(foldout);
			}

			if (invalid.Count > 0)
			{
				var foldout = BuildGroup($"Invalid ({invalid.Count})");

				foreach (var index in invalid)
					foldout.Add(BuildInvalidRule(index, points[index]));

				_rules.Add(foldout);
			}

			if (points.Length == 0)
				_rules.Add(new Label("No points") {style = {color = Color.gray, fontSize = 11}});
		}

		/// <summary>
		/// Foldout сдвигает содержимое под свою стрелку — в узком сайдбаре этот отступ съедает
		/// строку правила, а вкладывать сюда всё равно нечего
		/// </summary>
		private static Foldout BuildGroup(string text)
		{
			var foldout = new Foldout {text = text, value = true};
			foldout.style.marginBottom = 2;
			foldout.contentContainer.style.marginLeft = 0;

			return foldout;
		}

		/// <summary>
		/// Битую точку надо хотя бы удалить: её код не декодируется, карточку правила по нему
		/// не построить, а инспектор такую строку не рисует вовсе
		/// </summary>
		private VisualElement BuildInvalidRule(int index, SchedulePoint point)
		{
			var row = new VisualElement
			{
				style = {flexDirection = FlexDirection.Row, alignItems = Align.Center, marginBottom = 1}
			};

			row.Add(new Label($"#{index} · code {point.code}")
			{
				style = {flexGrow = 1, color = new Color(0.85f, 0.4f, 0.4f), fontSize = 11}
			});

			var remove = OverlayButton("×", () => RemovePoint(index), 18);
			remove.tooltip = $"Удалить точку #{index}";
			row.Add(remove);

			return row;
		}

		private VisualElement BuildRule(int index, SchedulePoint point, SchedulePointKind kind,
			List<Occurrence> occurrences)
		{
			var row = new VisualElement
			{
				style =
				{
					flexDirection = FlexDirection.Row,
					alignItems = Align.Center,
					marginBottom = 1,
					paddingLeft = 2,
					paddingRight = 2,
					paddingTop = 1,
					paddingBottom = 1,
					backgroundColor = new Color(0.23f, 0.23f, 0.23f)
				}
			};

			SetRadius(row, 4);

			row.Add(new VisualElement
			{
				style = {width = 4, height = 14, marginRight = 4, backgroundColor = GetColor(kind, index)}
			});

			var tag = OrdinalTag(index);

			if (tag != null)
			{
				row.Add(new Label(tag)
				{
					tooltip = $"Точка #{index} в списке схемы",
					style =
					{
						color = Color.gray,
						fontSize = 10,
						marginRight = 3,
						unityFontStyleAndWeight = FontStyle.Bold
					}
				});
			}

			var count = 0;
			for (var i = 0; i < occurrences.Count; i++)
			{
				if (occurrences[i].index == index)
					count++;
			}

			var duration = GetDuration(_durations, index);

			var rule = duration > 0
				? DescribeWindowRule(point, kind, duration)
				: DescribeRule(point, kind);

			row.Add(new Label(rule)
			{
				tooltip = $"code: {point.code}",
				style = {flexGrow = 1, fontSize = 11, whiteSpace = WhiteSpace.Normal}
			});

			row.Add(new Label(count.ToString())
			{
				tooltip = kind == SchedulePointKind.Interval
					? "Interval показывает только ближайшее срабатывание от focus — остальные лишь повторяют его с шагом"
					: "Вхождений за показанный период",
				style = {color = Color.gray, fontSize = 10, marginRight = 4}
			});

			var edit = OverlayButton(string.Empty, () => PromptEditPoint(index), 18);
			edit.tooltip = $"Изменить точку #{index} — редактор как в инспекторе";
			edit.style.alignItems = Align.Center;
			edit.style.justifyContent = Justify.Center;
			edit.style.paddingLeft = 0;
			edit.style.paddingRight = 0;

			edit.Add(new Image
			{
				scaleMode = ScaleMode.ScaleToFit,
				image = EditIcon(),
				style = {width = 10, height = 10}
			});

			row.Add(edit);

			var remove = OverlayButton("×", () => RemovePoint(index), 18);
			remove.tooltip = $"Удалить точку #{index}";
			row.Add(remove);

			row.AddManipulator(new ContextualMenuManipulator(evt =>
			{
				evt.menu.AppendAction($"Edit point #{index}…", _ => PromptEditPoint(index));
				evt.menu.AppendAction($"Remove point #{index}", _ => RemovePoint(index));
			}));

			return row;
		}

		/// <summary>
		/// Человекочитаемое правило: ради него календарь и открывают — из кода long его не видно
		/// </summary>
		private static string DescribeRule(SchedulePoint point, SchedulePointKind kind)
		{
			SchedulePointDecode decode;

			try
			{
				decode = point.code;
			}
			catch
			{
				return $"#{point.code}";
			}

			var time = $"{decode.hr:00}:{decode.min:00}" + (decode.sec > 0 ? $":{decode.sec:00}" : string.Empty);

			switch (kind)
			{
				case SchedulePointKind.Interval:
					// Таймкод «00:18:20» читается как время суток — пишем единицами
					return $"every {CompactSpan(decode.sec)}";

				case SchedulePointKind.Date:
					return $"{decode.day + 1:00}.{decode.mh + 1:00}.{decode.yr}, {time}";

				case SchedulePointKind.Daily:
					return $"every day, {time}";

				case SchedulePointKind.Monthly:
					return decode.sign
						? $"day {decode.day + 1}, {time}"
						: $"day {decode.day + 1} from month end, {time}";

				case SchedulePointKind.Yearly:
					return $"{MonthName(decode.mh)} {decode.day + 1}, {time}";

				case SchedulePointKind.Weekly:
					return $"every {WeekDayName(decode.day)}, {time}";

				case SchedulePointKind.MonthlyOnWeekday:
					return $"{WeekNumber(decode.weekOfMonth, decode.sign)} of month, {WeekDayName(decode.day)}, {time}";

				case SchedulePointKind.YearlyOnWeekday:
					return $"{MonthName(decode.mh)}, {WeekNumber(decode.weekOfMonth, decode.sign)}, " +
						$"{WeekDayName(decode.day)}, {time}";

				default:
					return kind.ToString();
			}
		}

		/// <summary>
		/// Подпись карточки: Interval показывает ближайшее срабатывание, поэтому «через шаг»,
		/// а не правило «каждые»
		/// </summary>
		private static string DescribeOccurrence(SchedulePoint point, SchedulePointKind kind)
		{
			if (kind != SchedulePointKind.Interval)
				return DescribeRule(point, kind);

			try
			{
				SchedulePointDecode decode = point.code;
				return $"in {CompactSpan(decode.sec)}";
			}
			catch
			{
				return DescribeRule(point, kind);
			}
		}

		/// <summary>
		/// Правило с окном — началом и концом, а не «правило + длительность»: границы дизайнер
		/// и задавал, длительность для него производная
		/// </summary>
		private static string DescribeWindowRule(SchedulePoint point, SchedulePointKind kind, long duration)
		{
			SchedulePointDecode decode;

			try
			{
				decode = point.code;
			}
			catch
			{
				return DescribeRule(point, kind);
			}

			var startTime = decode.hr * 3600L + decode.min * 60L + decode.sec;

			switch (kind)
			{
				case SchedulePointKind.Daily:
				{
					var end = startTime + duration;
					var extraDays = end / TimeUtility.SECS_IN_ONE_DAY;
					var suffix = extraDays > 0 ? $" (+{extraDays} d)" : string.Empty;

					return $"start — {DayTime(startTime)}\nend — {DayTime(end % TimeUtility.SECS_IN_ONE_DAY)}{suffix}";
				}

				case SchedulePointKind.Weekly:
				{
					var start = decode.day * TimeUtility.SECS_IN_ONE_DAY + startTime;
					var end = start + duration;
					var endDay = end / TimeUtility.SECS_IN_ONE_DAY % 7;

					return $"start — {WeekDayName(decode.day)}, {DayTime(startTime)}\n" +
						$"end — {WeekDayName(endDay)}, {DayTime(end % TimeUtility.SECS_IN_ONE_DAY)}";
				}

				case SchedulePointKind.Date:
				{
					try
					{
						var start = new DateTime((int) decode.yr, decode.mh + 1, (int) decode.day + 1,
							decode.hr, decode.min, (int) decode.sec, DateTimeKind.Utc);
						var end = start.AddSeconds(duration);

						return $"start — {start:dd.MM.yyyy, HH:mm}\nend — {end:dd.MM.yyyy, HH:mm}";
					}
					catch
					{
						break;
					}
				}
			}

			// У месячных и годовых правил конец не привязать к фиксированному дню —
			// месяцы разной длины, показываем длительность
			return $"{DescribeRule(point, kind)}\nduration — {CompactSpan(duration)}";
		}

		private static string DayTime(long daySeconds)
		{
			var hr = daySeconds / TimeUtility.SECS_IN_ONE_HOUR;
			var min = daySeconds / TimeUtility.SECS_IN_ONE_MINUTE % 60;
			var sec = daySeconds % 60;

			return sec > 0 ? $"{hr:00}:{min:00}:{sec:00}" : $"{hr:00}:{min:00}";
		}

		private static string CompactSpan(long seconds)
			=> ScheduleEditorFormat.CompactSpan(seconds);

		private static string MonthName(int month)
			=> CultureInfo.InvariantCulture.DateTimeFormat.GetMonthName(Mathf.Clamp(month + 1, 1, 12));

		private static string WeekDayName(long day) =>
			day >= 0 && day < WEEK_DAY_NAMES.Length ? WEEK_DAY_NAMES[day] : $"day {day}";

		private static string WeekNumber(int week, bool fromStart) =>
			fromStart ? $"week {week + 1}" : $"week {week + 1} from end";

		#endregion

		#region Range

		/// <summary>Шаг листания — весь показанный период</summary>
		private void Shift(int direction)
		{
			_anchor = _mode switch
			{
				ViewMode.Day => _anchor.AddDays(direction),
				ViewMode.Week => _anchor.AddDays(direction * DAYS_IN_WEEK),
				ViewMode.Month => _anchor.AddMonths(direction),
				_ => _anchor.AddYears(direction)
			};

			Refresh();
		}

		private DateTime RangeStart =>
			_mode switch
			{
				ViewMode.Day => _anchor.Date,
				ViewMode.Week => WeekStart(_anchor),
				ViewMode.Month => new DateTime(_anchor.Year, _anchor.Month, 1, 0, 0, 0, DateTimeKind.Utc),
				_ => new DateTime(_anchor.Year, 1, 1, 0, 0, 0, DateTimeKind.Utc)
			};

		private DateTime RangeEnd =>
			_mode switch
			{
				ViewMode.Day => RangeStart.AddDays(1),
				ViewMode.Week => RangeStart.AddDays(DAYS_IN_WEEK),
				ViewMode.Month => RangeStart.AddMonths(1),
				_ => RangeStart.AddYears(1)
			};

		/// <summary>Неделя начинается с понедельника — как и в кодировке Weekly</summary>
		private static DateTime WeekStart(DateTime date) =>
			date.Date.AddDays(-(((int) date.DayOfWeek + 6) % 7));

		private string RangeTitle()
		{
			var start = RangeStart;

			return _mode switch
			{
				ViewMode.Day => start.ToString("dddd, d MMMM yyyy", CultureInfo.InvariantCulture),
				ViewMode.Week => $"{start.ToString("d MMM", CultureInfo.InvariantCulture)} — " +
					start.AddDays(6).ToString("d MMM yyyy", CultureInfo.InvariantCulture),
				ViewMode.Month => start.ToString("MMMM yyyy", CultureInfo.InvariantCulture),
				_ => start.ToString("yyyy", CultureInfo.InvariantCulture)
			};
		}

		#endregion

		#region Calendar

		private void Refresh()
		{
			if (_content == null)
				return;

			_signature = GetSignature();

			_content.Clear();
			_nearest = null;
			_nowLine = null;
			_nowChip = null;
			_nowHour = DateTime.UtcNow.Hour;
			SyncModeButtons();

			// Когда сегодня и так на экране, кнопке нечего делать — гасим, чтобы не кликалась
			var todayDate = DateTime.UtcNow.Date;
			_todayButton?.SetEnabled(todayDate < RangeStart || todayDate >= RangeEnd);

			if (!TryGetPoints(out var points))
			{
				_rangeLabel.text = string.Empty;
				_rules.Clear();
				_footer.text = "Schedule is unavailable — the inspector was closed, reopen the calendar";
				return;
			}

			var start = RangeStart;
			var end = RangeEnd;

			_rangeLabel.text = RangeTitle();
			_durations = GetDurations();
			(_ordinals, _kindTotals) = BuildOrdinals(points);

			var occurrences = Collect(points, _durations, start, end, _focus);
			_nearest = FindNearest(occurrences, _focus);

			switch (_mode)
			{
				case ViewMode.Day:
					_content.Add(BuildTimeline(start, 1, points, occurrences));
					break;

				case ViewMode.Week:
					_content.Add(BuildTimeline(start, DAYS_IN_WEEK, points, occurrences));
					break;

				case ViewMode.Month:
					_content.Add(BuildMonth(start, occurrences, compact: false));
					break;

				default:
					BuildYear(start, occurrences);
					break;
			}

			RefreshRules(points, occurrences);
			_footer.text = BuildFooter(points, occurrences);
		}

		private void BuildYear(DateTime start, List<Occurrence> occurrences)
		{
			const int columns = 3;
			VisualElement row = null;

			for (var i = 0; i < 12; i++)
			{
				if (i % columns == 0)
				{
					row = new VisualElement
					{
						style = {flexDirection = FlexDirection.Row, flexGrow = 1, flexShrink = 0}
					};

					_content.Add(row);
				}

				row.Add(BuildMonth(start.AddMonths(i), occurrences, compact: true));
			}
		}

		#endregion

		#region Timeline

		/// <summary>
		/// Почасовая лента (Day/Week): в отличие от сетки месяца здесь видно и время, и само
		/// правило — отдельная панель деталей уже не нужна
		/// </summary>
		private VisualElement BuildTimeline(DateTime start, int days, SchedulePoint[] points,
			List<Occurrence> occurrences)
		{
			var timeline = new VisualElement {style = {flexGrow = 1}};

			if (days > 1)
				timeline.Add(BuildTimelineHeader(start, days));

			// Бакеты по ячейкам: сверять каждое вхождение с каждой из 24×days ячеек — квадрат,
			// на больших расписаниях лента строится заметно дольше
			var buckets = BucketOccurrences(start, days, occurrences);

			for (var hour = 0; hour < HOURS_IN_DAY; hour++)
			{
				// flexGrow — чтобы сутки занимали всю высоту окна, flexShrink = 0 — чтобы при
				// нехватке места включался скролл, а не сжатие строк в нечитаемую кашу
				var row = new VisualElement
				{
					style =
					{
						flexDirection = FlexDirection.Row,
						minHeight = HOUR_HEIGHT,
						flexGrow = 1,
						flexShrink = 0
					}
				};

				row.Add(new Label($"{hour:00}:00")
				{
					style =
					{
						width = HOUR_GUTTER,
						color = Color.gray,
						fontSize = 10,
						unityTextAlign = TextAnchor.UpperRight,
						paddingRight = 6
					}
				});

				for (var day = 0; day < days; day++)
					row.Add(BuildHourCell(start.AddDays(day), hour, points, buckets[day * HOURS_IN_DAY + hour]));

				timeline.Add(row);
			}

			return timeline;
		}

		/// <summary>Вхождения по ячейкам ленты: окно попадает во все часы, которые задевает</summary>
		private static List<Occurrence>[] BucketOccurrences(DateTime start, int days, List<Occurrence> occurrences)
		{
			var buckets = new List<Occurrence>[days * HOURS_IN_DAY];
			var end = start.AddDays(days);

			for (var i = 0; i < occurrences.Count; i++)
			{
				var occurrence = occurrences[i];
				var from = occurrence.utc;
				var to = occurrence.HasWindow ? occurrence.End : occurrence.utc.AddTicks(1);

				if (from >= end || to <= start)
					continue;

				var firstHour = Math.Max(0, (int) Math.Floor((from - start).TotalHours));
				var lastHour = Math.Min(buckets.Length, (int) Math.Ceiling((to - start).TotalHours));

				for (var h = firstHour; h < lastHour; h++)
					(buckets[h] ??= new List<Occurrence>()).Add(occurrence);
			}

			return buckets;
		}

		private VisualElement BuildTimelineHeader(DateTime start, int days)
		{
			// Шапка сжимается первой — без запрета от неё остаётся обрезанная полоска
			var header = new VisualElement
			{
				style = {flexDirection = FlexDirection.Row, marginBottom = 2, flexShrink = 0, minHeight = 16}
			};

			header.Add(new VisualElement {style = {width = HOUR_GUTTER}});

			var today = DateTime.UtcNow.Date;

			for (var i = 0; i < days; i++)
			{
				var date = start.AddDays(i);

				header.Add(new Label($"{WEEK_DAYS[i % DAYS_IN_WEEK]} {date.Day}")
				{
					style =
					{
						flexGrow = 1,
						flexBasis = 0,
						unityTextAlign = TextAnchor.MiddleCenter,
						fontSize = 11,
						color = date.Date == today ? NOW : Color.gray,
						unityFontStyleAndWeight = date.Date == _focus.Date ? FontStyle.Bold : FontStyle.Normal
					}
				});
			}

			return header;
		}

		/// <param name="cellOccurrences">Вхождения именно этой ячейки (из бакетов), может быть null</param>
		private VisualElement BuildHourCell(DateTime date, int hour, SchedulePoint[] points,
			List<Occurrence> cellOccurrences)
		{
			var cell = new VisualElement
			{
				style =
				{
					flexGrow = 1,
					flexBasis = 0,
					marginLeft = 1,
					paddingLeft = 2,
					paddingRight = 2,
					backgroundColor = BACKGROUND,
					borderTopWidth = 1,
					borderTopColor = HOUR_LINE,
					borderLeftWidth = 1,
					borderLeftColor = HOUR_LINE,
					position = Position.Relative,
					overflow = Overflow.Hidden
				}
			};

			var hourStart = new DateTime(date.Year, date.Month, date.Day, hour, 0, 0, DateTimeKind.Utc);
			var hourEnd = hourStart.AddHours(1);

			if (cellOccurrences != null)
			{
				for (var i = 0; i < cellOccurrences.Count; i++)
				{
					var occurrence = cellOccurrences[i];

					cell.Add(occurrence.HasWindow
						? BuildWindowBand(points, occurrence, hourStart, hourEnd)
						: BuildTimelineBar(points, occurrence));
				}
			}

			// Focus — своя линия: он может стоять и не на сегодня
			if (_focus.Date == date.Date && _focus.Hour == hour)
				cell.Add(TimeLine(_focus.Minute, FOCUS, $"Focus: {Humanize(_focus)}"));

			// «Сейчас» — последним: focus по умолчанию встаёт ровно в текущее время, и
			// добавленный раньше красный оказался бы ровно под зелёным
			var now = DateTime.UtcNow;

			if (now.Date == date.Date && now.Hour == hour)
			{
				_nowLine = TimeLine(now.Minute, NOW, NowTooltip(now));
				cell.Add(_nowLine);
			}

			var clicked = new DateTime(date.Year, date.Month, date.Day, hour, 0, 0, DateTimeKind.Utc);
			cell.AddManipulator(new ContextualMenuManipulator(evt => FillDayMenu(evt.menu, clicked)));

			return cell;
		}

		/// <summary>Линия внутри часа, сдвинутая по минутам</summary>
		/// <remarks>
		/// Сдвиг в процентах, а не в пикселях: строка часа растягивается по высоте окна,
		/// и от HOUR_HEIGHT линия уезжала бы
		/// </remarks>
		private static VisualElement TimeLine(int minute, Color color, string tooltip) =>
			new()
			{
				tooltip = tooltip,
				pickingMode = PickingMode.Ignore,
				style =
				{
					position = Position.Absolute,
					left = 0,
					right = 0,
					top = Length.Percent(minute / 60f * 100f),
					height = 2,
					backgroundColor = color
				}
			};

		/// <summary>
		/// Окно в ленте — полоса реальной длины: в каждой задетой ячейке рисуется её кусок,
		/// подпись только в стартовой, иначе она повторится в каждом часе
		/// </summary>
		private VisualElement BuildWindowBand(SchedulePoint[] points, Occurrence occurrence,
			DateTime hourStart, DateTime hourEnd)
		{
			var isNearest = _nearest.HasValue && _nearest.Value == occurrence.utc;
			var color = GetColor(occurrence.kind, occurrence.index);

			var from = occurrence.utc > hourStart ? occurrence.utc : hourStart;
			var to = occurrence.End < hourEnd ? occurrence.End : hourEnd;

			var band = new VisualElement
			{
				tooltip = Describe(occurrence),
				style =
				{
					position = Position.Absolute,
					left = 0,
					right = 0,
					top = Length.Percent((float) (from - hourStart).TotalMinutes / 60f * 100f),
					height = Length.Percent((float) (to - from).TotalMinutes / 60f * 100f),
					backgroundColor = color * new Color(1f, 1f, 1f, 0.45f),
					borderLeftWidth = 3,
					borderLeftColor = isNearest ? NEAREST : color,
					justifyContent = Justify.Center,
					overflow = Overflow.Hidden
				}
			};

			if (occurrence.utc >= hourStart)
			{
				var valid = occurrence.index >= 0 && occurrence.index < points.Length;

				var rule = valid
					? DescribeRule(points[occurrence.index], occurrence.kind)
					: occurrence.kind.ToString();

				band.Add(new Label(WithTag($"{occurrence.utc:HH:mm} → {occurrence.End:HH:mm}   {rule}", occurrence.index))
				{
					enableRichText = true,
					style =
					{
						fontSize = 10,
						marginLeft = 4,
						color = isNearest ? NEAREST : Color.white,
						unityFontStyleAndWeight = isNearest ? FontStyle.Bold : FontStyle.Normal
					}
				});
			}

			var index = occurrence.index;
			band.AddManipulator(new ContextualMenuManipulator(evt =>
			{
				evt.menu.AppendAction("Focus here", _ => SetFocus(occurrence.utc));
				evt.menu.AppendAction($"Edit point #{index}…", _ => PromptEditPoint(index));
				evt.menu.AppendAction($"Remove point #{index}", _ => RemovePoint(index));
			}));

			return band;
		}

		private VisualElement BuildTimelineBar(SchedulePoint[] points, Occurrence occurrence)
		{
			var isNearest = _nearest.HasValue && _nearest.Value == occurrence.utc;
			var color = GetColor(occurrence.kind, occurrence.index);

			var bar = new VisualElement
			{
				tooltip = Describe(occurrence),
				style =
				{
					flexDirection = FlexDirection.Row,
					alignItems = Align.Center,
					marginTop = 1,
					marginBottom = 1,
					paddingLeft = 4,
					paddingRight = 4,
					backgroundColor = color * new Color(1f, 1f, 1f, 0.55f),
					borderLeftWidth = 3,
					borderLeftColor = color
				}
			};

			SetRadius(bar, 3);

			// Ближайшую метим торцом и временем, а не рамкой вокруг: рамка на всю ширину
			// ленты читается как ещё одна линия времени, а не как выделение
			if (isNearest)
				bar.style.borderLeftColor = NEAREST;

			bar.Add(new Label(WithTag($"{occurrence.utc:HH:mm}", occurrence.index))
			{
				enableRichText = true,
				style =
				{
					fontSize = 10,
					color = isNearest ? NEAREST : Color.white,
					marginRight = 5,
					unityFontStyleAndWeight = isNearest ? FontStyle.Bold : FontStyle.Normal
				}
			});

			var valid = occurrence.index >= 0 && occurrence.index < points.Length;

			bar.Add(new Label(valid ? DescribeOccurrence(points[occurrence.index], occurrence.kind) : occurrence.kind.ToString())
			{
				style = {flexGrow = 1, fontSize = 10, color = new Color(0.92f, 0.92f, 0.92f)}
			});

			var index = occurrence.index;
			bar.AddManipulator(new ContextualMenuManipulator(evt =>
			{
				evt.menu.AppendAction("Focus here", _ => SetFocus(occurrence.utc));
				evt.menu.AppendAction($"Edit point #{index}…", _ => PromptEditPoint(index));
				evt.menu.AppendAction($"Remove point #{index}", _ => RemovePoint(index));
			}));

			return bar;
		}

		#endregion

		#region Month grid

		private VisualElement BuildMonth(DateTime month, List<Occurrence> occurrences, bool compact)
		{
			var block = new VisualElement
			{
				style = {marginRight = 6, marginBottom = 6, flexGrow = 1, flexShrink = 0}
			};

			// flexBasis считается по главной оси: в Year блоки лежат в горизонтальном ряду
			// (это ширина), а одиночный месяц — прямо в вертикальном скролле, где нулевой
			// базис схлопнул бы его по высоте
			if (compact)
				block.style.flexBasis = 0;

			block.Add(new Label(compact ? month.ToString("MMMM yyyy", CultureInfo.InvariantCulture) : string.Empty)
			{
				style =
				{
					display = compact ? DisplayStyle.Flex : DisplayStyle.None,
					unityTextAlign = TextAnchor.MiddleCenter,
					unityFontStyleAndWeight = FontStyle.Bold,
					fontSize = 11,
					marginBottom = 2
				}
			});

			var weekDays = new VisualElement {style = {flexDirection = FlexDirection.Row, flexShrink = 0}};

			foreach (var day in WEEK_DAYS)
			{
				weekDays.Add(new Label(compact ? day[..1] : day)
				{
					style =
					{
						flexGrow = 1,
						flexBasis = 0,
						unityTextAlign = TextAnchor.MiddleCenter,
						color = Color.gray,
						fontSize = compact ? 9 : 11
					}
				});
			}

			block.Add(weekDays);

			var first = WeekStart(month);
			var today = DateTime.UtcNow.Date;

			for (var week = 0; week < WEEKS; week++)
			{
				// flexBasis = 0 — иначе высота недели идёт от её содержимого, и неделя без
				// вхождений выходит вдвое ниже соседней. minHeight держит пол в низком
				// окне — там уже включается скролл
				var row = new VisualElement
				{
					style =
					{
						flexDirection = FlexDirection.Row,
						flexGrow = 1,
						flexShrink = 0,
						flexBasis = 0,
						minHeight = compact ? COMPACT_DAY_HEIGHT + 2 : DAY_HEIGHT + 2
					}
				};

				for (var i = 0; i < DAYS_IN_WEEK; i++)
					row.Add(BuildDay(first.AddDays(week * DAYS_IN_WEEK + i), month, occurrences, today, compact));

				block.Add(row);
			}

			return block;
		}

		private VisualElement BuildDay(DateTime date, DateTime month, List<Occurrence> occurrences,
			DateTime today, bool compact)
		{
			var inMonth = date.Month == month.Month && date.Year == month.Year;

			// Чужой день остаётся просто числом: в году он попадает сразу в два мини-месяца
			// (метки задвоились бы), а в месяце вхождения за границами периода вообще не собраны —
			// «пустой» рабочий день читался бы как «правило тут не срабатывает»
			var muted = !inMonth;

			var isFocus = !muted && date.Date == _focus.Date;
			var isToday = !muted && date.Date == today;

			var cell = new VisualElement
			{
				style =
				{
					flexGrow = 1,
					flexBasis = 0,
					minHeight = compact ? COMPACT_DAY_HEIGHT : DAY_HEIGHT,
					marginRight = 2,
					marginBottom = 2,
					paddingLeft = 2,
					paddingRight = 2,
					paddingTop = 1,
					backgroundColor = inMonth ? BACKGROUND : OUT_OF_MONTH,
					overflow = Overflow.Hidden
				}
			};

			var hasNearest = !muted && _nearest.HasValue && _nearest.Value.Date == date.Date;
			var accent = hasNearest || isFocus;

			cell.style.borderLeftWidth = cell.style.borderRightWidth = accent ? 2 : 1;
			cell.style.borderTopWidth = cell.style.borderBottomWidth = accent ? 2 : 1;

			var borderColor = hasNearest ? NEAREST : isFocus ? FOCUS : BORDER;
			cell.style.borderLeftColor = cell.style.borderRightColor = borderColor;
			cell.style.borderTopColor = cell.style.borderBottomColor = borderColor;

			var number = new Label(date.Day.ToString())
			{
				style =
				{
					color = inMonth ? Color.white : Color.gray,
					fontSize = compact ? 9 : 11,
					unityFontStyleAndWeight = inMonth ? FontStyle.Bold : FontStyle.Normal
				}
			};

			// В компакте чипы не влезают, поэтому «сейчас» там — плашка на самом числе
			if (isToday && compact)
			{
				number.tooltip = NowTooltip(DateTime.UtcNow);
				number.style.color = Color.white;
				number.style.backgroundColor = NOW;
				number.style.alignSelf = Align.FlexStart;
				number.style.paddingLeft = number.style.paddingRight = 3;
				number.style.unityTextAlign = TextAnchor.MiddleCenter;
				SetRadius(number, 8);
			}

			cell.Add(number);

			// Красная плашка со временем, а не просто красное число: в месяце иначе не видно,
			// который сейчас час — линии, как в ленте, тут нет
			if (isToday && !compact)
			{
				var now = DateTime.UtcNow;

				_nowChip = TimeChip(now.ToString(TIME_FORMAT, CultureInfo.InvariantCulture), NOW,
					NowTooltip(now), bold: true);

				cell.Add(_nowChip);
			}

			if (isFocus && !compact)
			{
				cell.Add(TimeChip(_focus.ToString(TIME_FORMAT, CultureInfo.InvariantCulture), FOCUS,
					$"Focus: {Humanize(_focus, seconds: true)}", bold: true));
			}

			if (!muted)
			{
				if (compact)
					AddDots(cell, occurrences, date);
				else
					AddChips(cell, occurrences, date);
			}

			var clicked = date;
			cell.AddManipulator(new ContextualMenuManipulator(evt => FillDayMenu(evt.menu, clicked)));

			// Двойной клик — как в обычном календаре: провалиться в день
			cell.RegisterCallback<MouseDownEvent>(evt =>
			{
				if (evt.button != 0 || evt.clickCount != 2)
					return;

				evt.StopPropagation();
				_mode = ViewMode.Day;
				_anchor = clicked;
				Refresh();
			});

			return cell;
		}

		private void AddChips(VisualElement cell, List<Occurrence> occurrences, DateTime date)
		{
			for (var i = 0; i < occurrences.Count; i++)
			{
				if (!occurrences[i].Falls(date))
					continue;

				cell.Add(occurrences[i].HasWindow
					? BuildWindowChip(occurrences[i], date)
					: BuildChip(occurrences[i]));
			}
		}

		/// <summary>
		/// Окно в сетке месяца: в одной ячейке весь отрезок не покажешь, поэтому у каждого
		/// дня свой кусок — начало, продолжение или конец
		/// </summary>
		private VisualElement BuildWindowChip(Occurrence occurrence, DateTime date)
		{
			var startsHere = occurrence.utc.Date == date.Date;
			var endsHere = occurrence.LastDate == date.Date;

			var text = startsHere
				? endsHere
					? $"{occurrence.utc:HH:mm} → {occurrence.End:HH:mm}"
					: $"{occurrence.utc:HH:mm} →"
				: endsHere
					? $"→ {occurrence.End:HH:mm}"
					: "all day";

			var isNearest = _nearest.HasValue && _nearest.Value == occurrence.utc && startsHere;

			var chip = TimeChip(WithTag(text, occurrence.index), GetColor(occurrence.kind, occurrence.index),
				Describe(occurrence), isNearest);

			// Продолжение — бледнее начала: иначе многодневное окно читается как несколько разных
			if (!startsHere)
				chip.style.opacity = 0.65f;

			if (isNearest)
			{
				chip.style.borderLeftWidth = chip.style.borderRightWidth = 1;
				chip.style.borderTopWidth = chip.style.borderBottomWidth = 1;
				chip.style.borderLeftColor = chip.style.borderRightColor = NEAREST;
				chip.style.borderTopColor = chip.style.borderBottomColor = NEAREST;
			}

			var index = occurrence.index;
			chip.AddManipulator(new ContextualMenuManipulator(evt =>
			{
				evt.menu.AppendAction("Focus here", _ => SetFocus(occurrence.utc));
				evt.menu.AppendAction($"Edit point #{index}…", _ => PromptEditPoint(index));
				evt.menu.AppendAction($"Remove point #{index}", _ => RemovePoint(index));
			}));

			return chip;
		}

		private void AddDots(VisualElement cell, List<Occurrence> occurrences, DateTime date)
		{
			var row = new VisualElement {style = {flexDirection = FlexDirection.Row, flexWrap = Wrap.Wrap}};
			var shown = 0;
			var total = 0;

			for (var i = 0; i < occurrences.Count; i++)
			{
				if (!occurrences[i].Falls(date))
					continue;

				total++;

				if (shown >= COMPACT_DOTS_LIMIT)
					continue;

				shown++;
				row.Add(new VisualElement
				{
					tooltip = Describe(occurrences[i]),
					style =
					{
						width = 5,
						height = 5,
						marginRight = 1,
						marginTop = 1,
						backgroundColor = GetColor(occurrences[i].kind, occurrences[i].index)
					}
				});
			}

			if (total > shown)
			{
				row.Add(new Label($"+{total - shown}")
				{
					style = {fontSize = 8, color = Color.gray, marginLeft = 1}
				});
			}

			cell.Add(row);
		}

		/// <summary>Единый вид у всех времён в ячейке — и у вхождений, и у focus</summary>
		private static Label TimeChip(string text, Color color, string tooltip, bool bold)
		{
			var chip = new Label(text)
			{
				tooltip = tooltip,
				enableRichText = true,
				style =
				{
					fontSize = 10,
					marginTop = 1,
					paddingLeft = 3,
					paddingRight = 3,
					color = Color.white,
					backgroundColor = color,
					unityTextAlign = TextAnchor.MiddleLeft,
					unityFontStyleAndWeight = bold ? FontStyle.Bold : FontStyle.Normal
				}
			};

			SetRadius(chip, 3);
			return chip;
		}

		private VisualElement BuildChip(Occurrence occurrence)
		{
			var isNearest = _nearest.HasValue && _nearest.Value == occurrence.utc;

			var chip = TimeChip(WithTag($"{occurrence.utc:HH:mm}", occurrence.index),
				GetColor(occurrence.kind, occurrence.index), Describe(occurrence), isNearest);

			if (isNearest)
			{
				chip.style.borderLeftWidth = chip.style.borderRightWidth = 1;
				chip.style.borderTopWidth = chip.style.borderBottomWidth = 1;
				chip.style.borderLeftColor = chip.style.borderRightColor = NEAREST;
				chip.style.borderTopColor = chip.style.borderBottomColor = NEAREST;
			}

			var index = occurrence.index;
			chip.AddManipulator(new ContextualMenuManipulator(evt =>
			{
				evt.menu.AppendAction("Focus here", _ => SetFocus(occurrence.utc));
				evt.menu.AppendAction($"Edit point #{index}…", _ => PromptEditPoint(index));
				evt.menu.AppendAction($"Remove point #{index}", _ => RemovePoint(index));
			}));

			return chip;
		}

		private static string Describe(Occurrence occurrence)
		{
			var text = $"{occurrence.kind} · point #{occurrence.index}\n{Humanize(occurrence.utc, seconds: true)}";

			if (!occurrence.HasWindow)
				return text;

			return text + $"\n→ {Humanize(occurrence.End, seconds: true)}" +
				$"\nduration: {CompactSpan(occurrence.duration)}";
		}

		private string BuildFooter(SchedulePoint[] points, List<Occurrence> occurrences)
		{
			// Каждая дата — своей строкой: в одну они склеиваются в нечитаемую простыню
			var text = $"Points: {points.Length}   ·   occurrences in range: {occurrences.Count}";

			text += $"\n{Colored("Focus", FOCUS)} — calendar reference point, set via right click:" +
				$" {Humanize(_focus)}";

			text += _nearest.HasValue
				? $"\n{Colored("Nearest", NEAREST)} — first occurrence strictly after focus:" +
				$" {Humanize(_nearest.Value, seconds: true)}"
				: $"\n{Colored("Nearest", NEAREST)} — no occurrences after focus in the shown range";

			return text;
		}

		/// <summary>
		/// Дата словами, технический вид — в скобках: читать удобнее словами, а сверять
		/// с кодом и логами — только по ISO
		/// </summary>
		private static string Humanize(DateTime utc, bool seconds = false)
		{
			var time = seconds ? "HH:mm:ss" : "HH:mm";

			return $"{utc.ToString($"dddd, d MMMM yyyy, {time}", CultureInfo.InvariantCulture)}" +
				$" ({utc.ToString($"yyyy-MM-dd {time}", CultureInfo.InvariantCulture)} UTC)";
		}

		private static string Colored(string text, Color color) =>
			$"<color=#{ColorUtility.ToHtmlStringRGB(color)}>{text}</color>";

		#endregion

		#region Menu

		private void FillDayMenu(DropdownMenu menu, DateTime date)
		{
			var captureDate = date;

			menu.AppendAction($"Focus/{date.ToString("d MMMM yyyy, HH:mm", CultureInfo.InvariantCulture)}",
				_ => SetFocus(captureDate));
			menu.AppendAction("Focus/Pick time…", _ => PromptFocus(captureDate));
			menu.AppendSeparator();

			foreach (var (label, kind) in AddOptions(date))
			{
				var captured = kind;
				menu.AppendAction(label, _ => PromptAddPoint(captured, captureDate));
			}
		}

		private static IEnumerable<(string label, SchedulePointKind kind)> AddOptions(DateTime date)
		{
			yield return ($"Add/Date — {date.ToString("d MMMM yyyy", CultureInfo.InvariantCulture)}",
				SchedulePointKind.Date);
			yield return ("Add/Daily — every day", SchedulePointKind.Daily);
			yield return ($"Add/Weekly — every {date.ToString("dddd", CultureInfo.InvariantCulture)}",
				SchedulePointKind.Weekly);
			yield return ($"Add/Monthly — day {date.Day}", SchedulePointKind.Monthly);
			yield return ($"Add/Yearly — {date.ToString("d MMMM", CultureInfo.InvariantCulture)}",
				SchedulePointKind.Yearly);
		}

		private void PromptFocus(DateTime date)
		{
			EditorTimeDialog.Show("Set Focus",
				_focus.TimeOfDay,
				(hr, min, sec) =>
					SetFocus(new DateTime(date.Year, date.Month, date.Day, hr, min, sec, DateTimeKind.Utc)),
				$"{date.ToString("d MMMM yyyy", CultureInfo.InvariantCulture)} · UTC");
		}

		private void SetFocus(DateTime utc)
		{
			_focus = DateTime.SpecifyKind(utc, DateTimeKind.Utc);
			Refresh();
		}

		private void PromptAddPoint(SchedulePointKind kind, DateTime date)
		{
			var suggested = SchedulePointDecode.GetDefault(kind);

			// В ленте час уже выбран кликом — предлагаем его, а не дефолт правила
			var initial = date.TimeOfDay > TimeSpan.Zero
				? date.TimeOfDay
				: new TimeSpan(suggested.hr, suggested.min, 0);

			EditorTimeDialog.Show("Add Schedule Point",
				initial,
				(hr, min, sec) => AddPoint(kind, date, hr, min, sec),
				$"{kind} · {date.ToString("d MMMM yyyy", CultureInfo.InvariantCulture)} · UTC",
				"Add");
		}

		private void AddPoint(SchedulePointKind kind, DateTime date, byte hr, byte min, byte sec)
		{
			var decode = SchedulePointDecode.GetDefault(kind);
			decode.hr = hr;
			decode.min = min;
			decode.sec = sec;

			switch (kind)
			{
				case SchedulePointKind.Date:
					decode.yr = date.Year;
					decode.mh = (byte) (date.Month - 1);
					decode.day = date.Day - 1;
					break;

				case SchedulePointKind.Weekly:
					// Неделя в кодировке начинается с понедельника
					decode.day = ((int) date.DayOfWeek + 6) % 7;
					break;

				case SchedulePointKind.Monthly:
					decode.day = date.Day - 1;
					break;

				case SchedulePointKind.Yearly:
					decode.mh = (byte) (date.Month - 1);
					decode.day = date.Day - 1;
					break;
			}

			long code = SchedulePointDecode.Encode(in decode);

			Mutate(points =>
			{
				var updated = new SchedulePoint[points.Length + 1];
				Array.Copy(points, updated, points.Length);
				updated[^1] = new SchedulePoint {code = code};
				return updated;
			});
		}

		/// <summary>
		/// Правка — тем же дровером, что и в инспекторе (код точки + duration окна):
		/// собственный диалог разошёлся бы с инспектором при первом же изменении схемы
		/// </summary>
		private void PromptEditPoint(int index)
		{
			if (_property == null)
				return;

			PointEditWindow.Open(_property, index, SyncExternalChange);
		}

		/// <summary>
		/// Инспекторный дровер точки в отдельном окне. Дерево своё, а не календарное:
		/// одно PropertyTree нельзя рисовать из двух IMGUI-контекстов
		/// </summary>
		private class PointEditWindow : EditorWindow
		{
			private PropertyTree _tree;
			private string _path;
			private GUIContent _label;

			/// <summary>Сколько точек было на момент открытия — путь окна привязан к индексу</summary>
			private int _count;

			public static void Open(InspectorProperty pointsProperty, int index, Action onEdited)
			{
				InspectorProperty child;

				try
				{
					child = pointsProperty.Children[index];
				}
				catch
				{
					return;
				}

				if (child == null)
					return;

				var window = CreateInstance<PointEditWindow>();
				window.titleContent = new GUIContent($"Edit Point #{index}", WindowIcon());
				window._path = child.Path;
				window._count = pointsProperty.Children.Count;
				window._label = new GUIContent($"#{index}");
				window._tree = PropertyTree.Create(new List<object>(pointsProperty.Tree.WeakTargets));
				window._tree.OnPropertyValueChanged += (_, _) => onEdited?.Invoke();

				var main = EditorGUIUtility.GetMainWindowPosition();
				window.position = new Rect(main.center.x - 210f, main.center.y - 90f, 420f, 180f);
				window.ShowUtility();
			}

			private void OnGUI()
			{
				var property = _tree?.GetPropertyAtPath(_path);

				// Точку могли удалить, ассет — выгрузить: без цели окну нечего редактировать
				if (property == null)
				{
					Close();
					return;
				}

				// Путь элемента коллекции — это индекс: стоит удалить точку, и под ним окажется
				// другая. Состав массива изменился — закрываемся, а не правим чужое правило
				if (property.Parent != null && property.Parent.Children.Count != _count)
				{
					Close();
					return;
				}

				GUILayout.Space(6);
				InspectorUtilities.BeginDrawPropertyTree(_tree, true);
				property.Draw(_label);
				InspectorUtilities.EndDrawPropertyTree(_tree);
			}

			private void OnDestroy()
			{
				_tree?.Dispose();
				_tree = null;
			}
		}

		private void RemovePoint(int index)
		{
			if (!TryGetPoints(out var current) || index < 0 || index >= current.Length)
				return;

			var rule = TryGetKind(current[index], out var kind)
				? DescribeRule(current[index], kind)
				: $"code {current[index].code}";

			// Удаление точки правится только через undo, а промахнуться по «×» легко
			if (!EditorUtility.DisplayDialog("Delete schedule point?",
					$"#{index} · {kind}\n{rule}", "Delete", "Cancel"))
				return;

			Mutate(points =>
				{
					if (index < 0 || index >= points.Length)
						return points;

					var updated = new SchedulePoint[points.Length - 1];
					Array.Copy(points, updated, index);
					Array.Copy(points, index + 1, updated, index, points.Length - index - 1);
					return updated;
				},
				durations =>
				{
					if (durations == null || index < 0 || index >= durations.Length)
						return durations;

					var updated = new long[durations.Length - 1];
					Array.Copy(durations, updated, index);
					Array.Copy(durations, index + 1, updated, index, durations.Length - index - 1);

					for (var i = 0; i < updated.Length; i++)
					{
						if (updated[i] > 0)
							return updated;
					}

					return null;
				});
		}

		#endregion

		#region Data

		/// <summary>Тик сверки: перечитывается только то, что календарь и рисует</summary>
		private void SyncData() => Sync(reload: false);

		/// <summary>
		/// Полная сверка — после undo и правок из чужих окон: там дерево могло перестроить
		/// сами свойства, а не только их значения
		/// </summary>
		private void ReloadData() => Sync(reload: true);

		/// <summary>
		/// Сверяет схему с показанным снимком и перестраивает окно, когда они разошлись
		/// </summary>
		/// <remarks>
		/// Правка точки приходит не только из календаря: инспектор, окно точки, undo, скрипт —
		/// уведомления есть не от каждого источника, а от окна точки приходят через дерево,
		/// которое календарю не принадлежит. Дешевле сверять содержимое, чем ловить все события
		/// </remarks>
		/// <param name="reload">
		/// Перечитать дерево целиком. На тике это лишнее: UpdateTree обходит все живые свойства
		/// таргета, пересчитывает их состояния и попутно применяет чужие отложенные правки —
		/// календарю же нужны ровно points и durations
		/// </param>
		private void Sync(bool reload)
		{
			if (_content == null)
				return;

			try
			{
				if (reload)
					_property?.Tree?.UpdateTree();
				else
					UpdateValues();
			}
			catch
			{
				// Дерево могло умереть вместе с инспектором — работаем по прошлому снимку,
				// а недоступность данных покажет сам Refresh
			}

			if (GetSignature() == _signature)
				return;

			Refresh();
		}

		/// <summary>Перечитывает значения от корня дерева до массива точек</summary>
		/// <remarks>
		/// Правку внутри точки видно и без этого — массив общий с деревом инспектора. Ветка нужна
		/// на замену самого массива: add/remove, undo, пересоздание при десериализации. Сверху
		/// вниз, потому что значение ребёнка читается из значения родителя
		/// </remarks>
		private void UpdateValues()
		{
			if (_property == null)
				return;

			UpdateValueChain(_property.ParentValueProperty);

			// Самому массиву — полный Update: он обновляет ещё и детей. Без этого добавленная
			// мимо календаря точка есть в данных, а свойства для неё в дереве ещё нет,
			// и «Edit point» по ней молча ничего не делает
			_property.Update(true);
		}

		private static void UpdateValueChain(InspectorProperty property)
		{
			if (property == null)
				return;

			UpdateValueChain(property.ParentValueProperty);
			property.ValueEntry?.Update();
		}

		/// <summary>
		/// Отпечаток схемы — точки и длительности окон, всё, что рисует календарь
		/// </summary>
		/// <remarks>
		/// FNV-1a вместо копии массивов: цена коллизии — одна пропущенная перерисовка
		/// до следующей правки. Ноль отдаётся, когда схемы нет вовсе — это тоже состояние
		/// </remarks>
		private ulong GetSignature()
		{
			if (!TryGetPoints(out var points))
				return 0;

			var durations = GetDurations();
			var hash = Mix(FNV_BASIS, points.Length);

			for (var i = 0; i < points.Length; i++)
				hash = Mix(hash, points[i].code);

			hash = Mix(hash, durations?.Length ?? 0);

			if (durations == null)
				return hash;

			for (var i = 0; i < durations.Length; i++)
				hash = Mix(hash, durations[i]);

			return hash;

			// Побайтово: FNV смешивает по байту, восьмёркой сразу соседние коды дали бы
			// соседние же хеши — а различать надо именно их
			static ulong Mix(ulong hash, long value)
			{
				for (var i = 0; i < sizeof(long); i++)
				{
					hash ^= (byte) (value >> (i * 8));
					hash *= FNV_PRIME;
				}

				return hash;
			}
		}

		private void Mutate(Func<SchedulePoint[], SchedulePoint[]> mutate,
			Func<long[], long[]> mutateDurations = null)
		{
			if (!TryGetPoints(out var points))
				return;

			var updated = mutate(points);

			try
			{
				// RecordForUndo, а не Undo.RecordObject: точки могут лежать в Odin-сериализации,
				// и сырой Unity-undo вернул бы только unity-часть — потому откат и «ломался»
				_property.RecordForUndo("Edit Schedule");

				_property.ValueEntry.WeakSmartValue = updated;
				_property.ValueEntry.ApplyChanges();

				// durations параллелен точкам — правки, меняющие индексы, обязаны двигать и его
				if (mutateDurations != null &&
					_property.Parent?.ValueEntry?.WeakSmartValue is ScheduleScheme scheme)
				{
					var durations = mutateDurations(scheme.durations);
					var durationsProperty = _property.Parent.Children[nameof(ScheduleScheme.durations)];

					if (durationsProperty?.ValueEntry != null)
					{
						durationsProperty.ValueEntry.WeakSmartValue = durations;
						durationsProperty.ValueEntry.ApplyChanges();
					}
					else
					{
						// ApplyChanges обязателен: durations скрыт HideInInspector, своего свойства
						// у него нет, и без применения запись затрёт ближайший Update дерева
						scheme.durations = durations;
						_property.Parent.ValueEntry.WeakSmartValue = scheme;
						_property.Parent.ValueEntry.ApplyChanges();
					}
				}

				_property.MarkSerializationRootDirty();

				foreach (var target in _property.Tree.WeakTargets)
				{
					if (target is UnityEngine.Object unityObject)
						EditorUtility.SetDirty(unityObject);
				}

				// Инспектор сам не перерисуется: правка пришла из чужого окна, и до наведения
				// мыши список точек показывал бы прежнее расписание
				InternalEditorUtility.RepaintAllViews();
			}
			catch (Exception exception)
			{
				Debug.LogError($"Не удалось изменить расписание: {exception.Message}");
				_property = null;
			}

			Refresh();
		}

		private bool TryGetPoints(out SchedulePoint[] points)
		{
			points = Array.Empty<SchedulePoint>();

			try
			{
				if (_property?.ValueEntry == null || _property.Tree?.WeakTargets == null)
					return false;

				points = _property.ValueEntry.WeakSmartValue as SchedulePoint[] ?? Array.Empty<SchedulePoint>();
				return true;
			}
			catch
			{
				return false;
			}
		}

		/// <summary>
		/// Строго позже focus: Interval раскладывается от него же и всегда даёт вхождение ровно
		/// в focus — с нестрогим сравнением «ближайшей» вечно оказывался бы сам focus
		/// </summary>
		private static DateTime? FindNearest(List<Occurrence> occurrences, DateTime focus)
		{
			for (var i = 0; i < occurrences.Count; i++)
			{
				if (occurrences[i].utc > focus)
					return occurrences[i].utc;
			}

			return null;
		}

		private static List<Occurrence> Collect(SchedulePoint[] points, long[] durations,
			DateTime from, DateTime to, DateTime focus)
		{
			var result = new List<Occurrence>();

			for (var i = 0; i < points.Length; i++)
			{
				if (!TryGetKind(points[i], out var kind))
					continue;

				if (kind == SchedulePointKind.Interval)
				{
					CollectInterval(points[i], i, from, to, focus, result);
					continue;
				}

				var duration = GetDuration(durations, i);

				// Окно могло начаться до периода и дотянуться в него — отступаем на его длину.
				// Секунда назад — только если есть куда: на нижней границе DateTime бросает
				var cursor = SubtractSeconds(from, duration);

				if (cursor > DateTime.MinValue)
					cursor = cursor.AddSeconds(-1);

				for (var guard = 0; guard < OCCURRENCES_LIMIT; guard++)
				{
					DateTime next;

					try
					{
						next = ScheduleUtility.ToDateTime(ref points[i], cursor);
					}
					catch
					{
						break;
					}

					// Date возвращает одну и ту же дату при любом курсоре — на ней и останавливаемся
					if (next <= cursor || next >= to)
						break;

					cursor = next;

					// Отступ назад мог захватить уже закончившиеся окна
					if (ScheduleEditorFormat.AddSeconds(next, duration) <= from && next < from)
						continue;

					result.Add(new Occurrence(i, kind, next, duration));
				}
			}

			result.Sort((a, b) => a.utc.CompareTo(b.utc));
			return result;
		}

		private static DateTime SubtractSeconds(DateTime utc, long seconds)
		{
			if (seconds <= 0)
				return utc;

			var limit = (utc - DateTime.MinValue).TotalSeconds;

			return seconds >= limit ? DateTime.MinValue : utc.AddSeconds(-seconds);
		}

		private static long GetDuration(long[] durations, int index)
			=> durations != null && index >= 0 && index < durations.Length
				? Math.Max(0, durations[index])
				: 0;

		/// <summary>Длительности лежат не в точках, а рядом — в durations схемы</summary>
		private long[] GetDurations()
		{
			try
			{
				return _property?.Parent?.ValueEntry?.WeakSmartValue is ScheduleScheme scheme
					? scheme.durations
					: null;
			}
			catch
			{
				return null;
			}
		}

		/// <summary>
		/// Interval считается от запуска, календарной привязки у него нет — раскладываем его
		/// от focus, поэтому сдвиг focus двигает и всю сетку интервала
		/// </summary>
		private static void CollectInterval(SchedulePoint point, int index, DateTime from, DateTime to,
			DateTime focus, List<Occurrence> result)
		{
			SchedulePointDecode decode;

			try
			{
				decode = point.code;
			}
			catch
			{
				return;
			}

			if (decode.sec <= 0)
				return;

			// Interval монотонен относительно focus: все вхождения — «первое + k*шаг»,
			// и пачка карточек на весь период не добавляет информации. Показываем только
			// ближайшее срабатывание — первый шаг ПОСЛЕ focus, а не в него, иначе чип
			// дублировал бы сам focus
			var current = ScheduleEditorFormat.AddSeconds(focus, decode.sec);

			if (current >= from && current < to)
				result.Add(new Occurrence(index, SchedulePointKind.Interval, current, 0));
		}

		private static bool TryGetKind(SchedulePoint point, out SchedulePointKind kind)
		{
			try
			{
				kind = point.GetKind();
				return true;
			}
			catch
			{
				kind = default;
				return false;
			}
		}

		/// <summary>
		/// Оттенок конкретной точки: несколько правил одного типа иначе неразличимы в календаре.
		/// Меняются только яркость и насыщенность — тон общий, тип остаётся узнаваемым
		/// </summary>
		private Color GetColor(SchedulePointKind kind, int index)
		{
			var color = GetColor(kind);

			var ordinal = _ordinals != null && index >= 0 && index < _ordinals.Length
				? _ordinals[index] % 4
				: 0;

			if (ordinal == 0)
				return color;

			Color.RGBToHSV(color, out var h, out var s, out var v);

			// Светлый вариант с потолком: на слишком ярком фоне белый текст чипа перестаёт читаться
			return ordinal switch
			{
				1 => Color.HSVToRGB(h, s, Mathf.Min(v * 1.3f, 0.85f)),
				2 => Color.HSVToRGB(h, s, v * 0.7f),
				_ => Color.HSVToRGB(h, s * 0.55f, Mathf.Min(v * 1.15f, 0.8f))
			};
		}

		private static (int[] ordinals, int[] totals) BuildOrdinals(SchedulePoint[] points)
		{
			var ordinals = new int[points.Length];
			var totals = new int[points.Length];
			var kinds = new SchedulePointKind?[points.Length];
			var counts = new Dictionary<SchedulePointKind, int>();

			for (var i = 0; i < points.Length; i++)
			{
				if (!TryGetKind(points[i], out var kind))
					continue;

				kinds[i] = kind;
				counts.TryGetValue(kind, out var count);
				ordinals[i] = count;
				counts[kind] = count + 1;
			}

			for (var i = 0; i < points.Length; i++)
			{
				if (kinds[i].HasValue)
					totals[i] = counts[kinds[i].Value];
			}

			return (ordinals, totals);
		}

		/// <summary>Метка «#N» внутри типа — привязка чипа в календаре к правилу в сайдбаре</summary>
		private string OrdinalTag(int index)
		{
			if (_ordinals == null || index < 0 || index >= _ordinals.Length || _kindTotals[index] < 2)
				return null;

			return $"#{_ordinals[index] + 1}";
		}

		/// <summary>Метка полупрозрачным хвостом: видна, но со временем не спорит</summary>
		private string WithTag(string text, int index)
		{
			var tag = OrdinalTag(index);
			return tag == null ? text : $"{text} <color=#FFFFFF99>{tag}</color>";
		}

		/// <summary>
		/// Тона типов не повторяют системные метки: красный занят «сейчас» (NOW), зелёный — focus,
		/// янтарный — ближайшей точкой — чип типа в том же цвете читался бы как метка
		/// </summary>
		private static Color GetColor(SchedulePointKind kind) =>
			kind switch
			{
				SchedulePointKind.Date => new Color(0.56f, 0.36f, 0.66f),
				SchedulePointKind.Daily => new Color(0.24f, 0.50f, 0.45f),
				SchedulePointKind.Monthly => new Color(0.32f, 0.42f, 0.60f),
				SchedulePointKind.Yearly => new Color(0.60f, 0.34f, 0.54f),
				SchedulePointKind.Weekly => new Color(0.55f, 0.48f, 0.28f),
				SchedulePointKind.MonthlyOnWeekday => new Color(0.30f, 0.48f, 0.55f),
				SchedulePointKind.YearlyOnWeekday => new Color(0.48f, 0.30f, 0.44f),
				_ => new Color(0.4f, 0.4f, 0.4f)
			};

		#endregion

		private enum ViewMode
		{
			Day,
			Week,
			Month,
			Year
		}

		private readonly struct Occurrence
		{
			public readonly int index;
			public readonly SchedulePointKind kind;
			public readonly DateTime utc;

			/// <inheritdoc cref="ISchedulePoint.Duration"/>
			public readonly long duration;

			public Occurrence(int index, SchedulePointKind kind, DateTime utc, long duration)
			{
				this.index = index;
				this.kind = kind;
				this.utc = utc;
				this.duration = duration;
			}

			public bool HasWindow => duration > 0;

			public DateTime End => ScheduleEditorFormat.AddSeconds(utc, duration);

			/// <summary>Последний день окна: конец исключающий, полночь принадлежит прошлым суткам</summary>
			public DateTime LastDate => End.AddTicks(-1).Date;

			/// <summary>Попадает ли вхождение на день — окно ещё и всеми промежуточными сутками</summary>
			public bool Falls(DateTime date)
				=> HasWindow
					? utc < date.Date.AddDays(1) && End > date.Date
					: utc.Date == date.Date;
		}
	}
}
