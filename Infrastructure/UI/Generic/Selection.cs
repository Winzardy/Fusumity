using System;
using System.Collections.Generic;
using Sapientia.Collections;
using Sapientia.Pooling;

namespace UI
{
	public struct SelectionParameters
	{
		/// <summary>
		/// Сколько минимум выделенных элементов
		/// </summary>
		public int min;

		/// <summary>
		/// Сколько максимум выделенных элементов
		/// </summary>
		public int max;

		/// <summary>
		/// Автоматически снимать выделение на предыдущих элементах,
		/// если пытаемся выбрать при максимальном выделении
		/// </summary>
		public bool autoDeselectPrevOnMaxSelection;

		/// <summary>
		/// Минимум выбран один элемент и выбрать можно максимум один элемент,
		/// при выделении нового элемента, старый деселектится
		/// </summary>
		public static readonly SelectionParameters Single = new(min: 1, max: 1, autoDeselectPrevOnMaxSelection: true);

		/// <summary>
		/// Максимум выбран один элемент или ничего,
		/// при выделении нового элемента, старый деселектится
		/// </summary>
		public static readonly SelectionParameters SingleOrEmpty =
			new(min: 0, max: 1, autoDeselectPrevOnMaxSelection: true);

		/// <summary>
		/// Можно выбрать все и все деселектить
		/// </summary>
		public static readonly SelectionParameters Multiselect = new(min: 0, max: int.MaxValue);

		public SelectionParameters(int min, int max) : this(min, max, false)
		{
		}

		public SelectionParameters(int min, int max, bool autoDeselectPrevOnMaxSelection)
		{
			this.min = min;
			this.max = max;
			this.autoDeselectPrevOnMaxSelection = autoDeselectPrevOnMaxSelection;
		}
	}

	/// <summary>
	/// Посредник между выделением элементов, тут задаются режимы выделения и хранятся выделенные
	///
	/// Может быть использован не только в UI
	/// </summary>
	public sealed class Selection<T> : IDisposable
	{
		private T[] _elements;

		private Action<int, bool, bool> _onSelectionChanged;
		private Action<bool> _onMaxSelectionChanged;

		private SelectionParameters _parameters;

		private int[] _priorityIndexesOnAutoMinSelection;
		private bool? _cachePrevMaxSelection;

		//LinkedList потому что важен еще порядок
		private LinkedList<int> _selectedElements;

		public LinkedList<int> SelectedElements => _selectedElements;

		public ref readonly SelectionParameters Parameters => ref _parameters;

		public Selection(Action<int, bool, bool> onSelectionChanged,
			SelectionParameters? parameters = null) : this(onSelectionChanged, null, parameters)
		{
		}

		/// <param name="onSelectionChanged">Index, value, immediate</param>
		/// <param name="onMaxSelectionChanged">Вызывается при изменения состояния "макс элементов выделено"</param>
		public Selection(
			Action<int, bool, bool> onSelectionChanged,
			Action<bool> onMaxSelectionChanged,
			SelectionParameters? parameters)
		{
			_selectedElements = LinkedListPool<int>.Get();

			_onSelectionChanged = onSelectionChanged;
			_onMaxSelectionChanged = onMaxSelectionChanged;

			if (parameters.HasValue)
				SetParameters(parameters.Value, false);
		}

		public void Dispose()
		{
			_selectedElements?.ReleaseToStaticPool();
			_selectedElements = null;

			_elements = null;
		}

		public bool IsSelected(int index) => SelectedElements.Contains(index);

		public void SetParameters(SelectionParameters parameters, bool updated = true)
		{
			if (_parameters.min > _parameters.max)
				throw new Exception("SelectionParameters: Min can't be more than max");

			var needUpdateMax = updated && _parameters.max != parameters.max;
			var needUpdateMin = updated && _parameters.min != parameters.min;

			_parameters = parameters;

			if (needUpdateMax || needUpdateMin)
				TryAutoMinMaxSelection();
		}

		public void TrySelect(int[] indexes, bool immediate = false)
		{
			for (int i = 0; i < indexes.Length; i++)
			{
				TrySelect(indexes[i], immediate);
			}
		}

		public bool TrySelect(int index, bool immediate = false)
		{
			if (SelectedElements.Count >= _parameters.max)
			{
				if (SelectedElements.Contains(index))
					return false;

				if (!_parameters.autoDeselectPrevOnMaxSelection)
					return false;

				if (!SelectedElements.IsNullOrEmpty())
					TryDeselectInternal(SelectedElements.First.Value, immediate);
			}

			return TrySelectInternal(index, immediate);
		}

		private bool TrySelectInternal(int index, bool immediate = false)
		{
			if (SelectedElements.Contains(index))
				return false;

			SelectedElements.AddLast(index);
			SelectionChanged(index, true, immediate);

			TryUpdateMaxSelection();

			return true;
		}

		public void TryDeselect(int[] indexes, bool immediate = false)
		{
			for (int i = 0; i < indexes.Length; i++)
			{
				TryDeselect(indexes[i], immediate);
			}
		}

		public bool TryDeselect(int index, bool immediate = false)
		{
			if (SelectedElements.Count == _parameters.min)
				return false;

			return TryDeselectInternal(index, immediate);
		}

		private bool TryDeselectInternal(int index, bool immediate = false)
		{
			if (SelectedElements.Remove(index))
			{
				SelectionChanged(index, false, immediate);
				TryUpdateMaxSelection();

				return true;
			}

			return false;
		}

		private void TryUpdateMaxSelection()
		{
			var check = SelectedElements.Count >= _parameters.max;
			if (_cachePrevMaxSelection.HasValue)
			{
				if (_cachePrevMaxSelection == check)
					return;
			}

			_cachePrevMaxSelection = check;
			_onMaxSelectionChanged?.Invoke(check);
		}

		/// <param name="elements">Массив элементов с котором работает Selection</param>
		/// <param name="priorityIndexesOnAutoMinSelection">Индексы элементов, которые приоритетнее перед выделением при минимуме</param>
		public void Bind(T[] elements, int[] priorityIndexesOnAutoMinSelection = null)
		{
			_priorityIndexesOnAutoMinSelection = priorityIndexesOnAutoMinSelection;

			if(_elements == elements)
				return;

			SelectedElements.Clear();
			_elements = elements;
			TryAutoMinMaxSelection();
		}

		private void TryAutoMinMaxSelection()
		{
			if (_elements.IsNullOrEmpty())
				return;

			var count = SelectedElements.Count - _parameters.min;

			if (count < 0)
			{
				count = Math.Abs(count);

				if (!_priorityIndexesOnAutoMinSelection.IsNullOrEmpty())
				{
					for (int i = 0; i < _priorityIndexesOnAutoMinSelection.Length; i++)
					{
						if (count <= 0)
							return;

						var index = _priorityIndexesOnAutoMinSelection[i];
						if (TrySelectInternal(index, true))
							count--;
					}
				}

				//Выделяет первые попавшиеся элементы если недостаточно
				count = _parameters.min - SelectedElements.Count;
				for (int i = 0; i < _elements.Length; i++)
				{
					if (count <= 0)
						return;

					if (TrySelectInternal(i, true))
						count--;
				}
			}
			else if (count > 0 && count >= _parameters.max)
			{
				count = _parameters.max - count;

				for (int i = 0; i < count; i++)
					TryDeselectInternal(SelectedElements.First.Value);
			}
		}

		public void TryDeselectAll()
		{
			TryDeselectAllInternal();

			if (_parameters.min <= 0)
				return;

			for (int j = 0; j < _parameters.min; j++)
				TrySelect(j, true);
		}

		private void TryDeselectAllInternal()
		{
			foreach (var element in SelectedElements)
				SelectionChanged(element, false);

			SelectedElements.Clear();
		}

		private void SelectionChanged(int index, bool value, bool immediate = false) =>
			_onSelectionChanged?.Invoke(index, value, immediate);
	}
}
