using Sapientia.Collections;
using Sapientia.Extensions;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Fusumity.MVVM
{
	/// <summary>
	/// View model that has binary state (either active or inactive).
	/// </summary>
	public interface IStatefulViewModel
	{
		[MaybeNull]
		string Id { get; }
		bool IsActive { get; }

		event Action<bool> ActiveStateChanged;
	}

	/// <summary>
	/// Provides an ability to change its state externally.
	/// </summary>
	public interface IAnemicStatefulViewModel : IStatefulViewModel
	{
		void SetActive(bool active);
	}

	public class AnemicStatefulViewModel : IAnemicStatefulViewModel
	{
		public string Id { get; set; }
		public bool IsActive { get; private set; }

		public event Action<bool> ActiveStateChanged;

		public AnemicStatefulViewModel(string id = null)
		{
			Id = id;
		}

		public void SetActive(bool isActive)
		{
			if (IsActive == isActive)
				return;

			IsActive = isActive;
			ActiveStateChanged?.Invoke(isActive);
		}

		public override string ToString()
		{
			return $"Id: [ {Id} ] Is Active: [ {IsActive} ]";
		}
	}

	/// <summary>
	/// Children state affect the entire branch state.
	/// </summary>
	public class NodeStatefulViewModel : IAnemicStatefulViewModel, IDisposable
	{
		private int _activeChildren;
		private bool _explicitlyDeactivated;

		public string Id { get; }
		public IStatefulViewModel Parent { get; private set; }
		public List<IStatefulViewModel> Children { get; private set; }

		/// <summary>
		/// False by default.
		/// Is not affected by children state.
		/// Changed via SetActive method.
		/// </summary>
		public bool IsActiveSelf { get; private set; }

		/// <summary>
		/// Will be active if either self is set to active,
		/// or any children are active.
		/// Can be overridden with OverrideState method.
		/// </summary>
		public bool IsActive { get { return !_explicitlyDeactivated && (IsActiveSelf || _activeChildren > 0); } }

		public event Action<bool> ActiveStateChanged;

		public NodeStatefulViewModel(string id, IStatefulViewModel parent = null) : this(parent)
		{
			Id = id;
		}

		public NodeStatefulViewModel(IStatefulViewModel parent = null)
		{
			Parent = parent;
		}

		public void Dispose()
		{
			if (!Children.IsNullOrEmpty())
			{
				OnDispose();

				for (int i = 0; i < Children.Count; i++)
				{
					var child = Children[i];
					child.ActiveStateChanged -= HandleChildStateChanged;

					if (child is IDisposable disposable)
					{
						disposable.Dispose();
					}
				}

				Children.Clear();
			}
		}

		/// <summary>
		/// Change self state.
		/// Will not affect children in any way.
		/// </summary>
		public void SetActive(bool active)
		{
			if (IsActiveSelf == active)
				return;

			var wasActive = IsActive;

			IsActiveSelf = active;

			if (wasActive != IsActive)
			{
				OnActiveStateChanged(IsActive);
				ActiveStateChanged?.Invoke(IsActive);
			}
		}

		/// <summary>
		/// Overrides current state.
		/// Self state and children state will be ignored.
		/// </summary>
		public void OverrideState(bool active)
		{
			var wasActive = IsActive;
			_explicitlyDeactivated = !active;

			if (wasActive != IsActive)
			{
				OnActiveStateChanged(IsActive);
				ActiveStateChanged?.Invoke(IsActive);
			}
		}

		public void AddChildren(params IStatefulViewModel[] children)
		{
			for (int i = 0; i < children.Length; i++)
			{
				AddChild(children[i]);
			}
		}

		public void AddChildren(IList<IStatefulViewModel> children)
		{
			for (int i = 0; i < children.Count; i++)
			{
				AddChild(children[i]);
			}
		}

		public void AddChild(IStatefulViewModel child)
		{
			if (child == null)
				return;

			if (Children == null)
			{
				Children = new List<IStatefulViewModel>();
			}

			Children.Add(child);
			if (child is NodeStatefulViewModel node)
			{
				node.Parent = this;
			}

			if (child.IsActive)
			{
				var wasActive = IsActive;

				_activeChildren++;

				if (wasActive != IsActive)
				{
					ActiveStateChanged?.Invoke(IsActive);
				}
			}

			child.ActiveStateChanged += HandleChildStateChanged;
			OnChildAdded(child);
		}

		public void RemoveChild(IStatefulViewModel child)
		{
			if (child == null || Children.IsNullOrEmpty())
				return;

			if (Children.Remove(child))
			{
				child.ActiveStateChanged -= HandleChildStateChanged;

				if (child.IsActive)
				{
					var wasActive = IsActive;
					_activeChildren--;

					if (wasActive != IsActive)
					{
						ActiveStateChanged?.Invoke(IsActive);
					}
				}
			}
		}

		public IStatefulViewModel GetNode(string id, Func<IStatefulViewModel, bool> predicate = null, bool includeSelf = false)
		{
			if (includeSelf && Id == id &&
				(predicate == null || predicate.Invoke(this)))
				return this;

			if (Children.IsNullOrEmpty())
				return default;

			for (int i = 0; i < Children.Count; i++)
			{
				var child = Children[i];
				if (child.Id == id &&
				   (predicate == null || predicate.Invoke(child)))
				{
					return child;
				}

				if (child is NodeStatefulViewModel node)
				{
					var foundNode = node.GetNode(id, predicate);
					if (foundNode != null)
					{
						return foundNode;
					}
				}
			}

			return default;
		}

		/// <summary>
		/// Will return first found node of type T.
		/// Use with caution.
		/// </summary>
		public T GetNode<T>(bool includeSelf = false) where T : IStatefulViewModel
			=> GetNode<T>(null, includeSelf);

		public T GetNode<T>(string id, bool includeSelf = false) where T : IStatefulViewModel
		{
			if (includeSelf && (id.IsNullOrEmpty() || Id == id) && this is T cast)
				return cast;

			if (Children.IsNullOrEmpty())
				return default;

			for (int i = 0; i < Children.Count; i++)
			{
				var child = Children[i];
				if ((id.IsNullOrEmpty() || child.Id == id) && child is T required)
				{
					return required;
				}

				if (child is NodeStatefulViewModel node)
				{
					var foundNode = node.GetNode<T>(id);
					if (foundNode != null)
					{
						return foundNode;
					}
				}
			}

			return default;
		}

		private void HandleChildStateChanged(bool active)
		{
			var wasActive = IsActive;

			if (active)
			{
				_activeChildren++;
			}
			else
			{
				_activeChildren--;
			}

			if (wasActive != IsActive)
			{
				OnActiveStateChanged(IsActive);
				ActiveStateChanged?.Invoke(IsActive);
			}
		}

		protected virtual void OnDispose()
		{
		}

		protected virtual void OnActiveStateChanged(bool isActive)
		{
		}

		protected virtual void OnChildAdded(IStatefulViewModel child)
		{
		}

		public override string ToString()
		{
			return
				$"Node Id: [ {Id} ] " +
				$"Is Active: [ {IsActive} ] " +
				$"Children - {Children.GetCompositeString(x => x.Id)}";
		}
	}
}
