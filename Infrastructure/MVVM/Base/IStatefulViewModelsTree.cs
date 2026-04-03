using System;
using System.Linq;

namespace Fusumity.MVVM
{
	public interface IStatefulViewModelsTree : IDisposable
	{
		void AddBranch(NodeStatefulViewModel branch);
		void RemoveBranch(NodeStatefulViewModel branch);

		T GetNode<T>(string id, bool includeSelf = false) where T : IStatefulViewModel;
		IStatefulViewModel GetNode(string id, Func<IStatefulViewModel, bool> predicate = null, bool includeSelf = false);
	}

	public class StatefulViewModelsTree : IStatefulViewModelsTree
	{
		private NodeStatefulViewModel _root;
		private bool _disposed;

		public StatefulViewModelsTree(string rootName = "root")
		{
			_root = new NodeStatefulViewModel(rootName);
		}

		public void Dispose()
		{
			if (!_disposed)
			{
				_root.Dispose();
				_disposed = true;
			}
		}

		/// <summary>
		/// Adds branch to the root node.
		/// </summary>
		public void AddBranch(NodeStatefulViewModel branch)
		{
			if (_disposed)
				return;

			_root.AddChild(branch);
		}

		/// <summary>
		/// Removes branch from the root node.
		/// </summary>
		public void RemoveBranch(NodeStatefulViewModel branch)
		{
			if (_disposed)
				return;

			_root.RemoveChild(branch);
		}

		public void RemoveBranch<T>(Func<T, bool> predicate = null) where T : NodeStatefulViewModel
		{
			if (_disposed)
				return;

			var child = GetBranch<T>(predicate);
			if (child != null)
			{
				RemoveBranch(child);
			}
		}

		public T GetBranch<T>(Func<T, bool> predicate = null) where T : NodeStatefulViewModel
		{
			var query = _root
				.Children?
				.OfType<T>();

			return predicate != null ?
				query?.FirstOrDefault(predicate) :
				query?.FirstOrDefault();
		}

		public T GetBranch<T>(string Id) where T : NodeStatefulViewModel
		{
			return GetBranch<T>(x => x.Id == Id);
		}

		public T GetOrCreateBranch<T>(Func<T> factoryMethod, Func<T, bool> predicate = null) where T : NodeStatefulViewModel
		{
			if (_disposed)
				return default;

			var branch = GetBranch<T>();

			if (branch == null)
			{
				branch = factoryMethod.Invoke();
				AddBranch(branch);
			}

			return branch;
		}

		public T GetOrCreateBranch<T>(Func<T, bool> predicate = null) where T : NodeStatefulViewModel, new()
		{
			return GetOrCreateBranch<T>(static () => new T(), predicate);
		}

		public NodeStatefulViewModel GetOrCreateBranch(string id)
		{
			if (_disposed)
				return default;

			var child = _root.Children?.FirstOrDefault(x => x.Id == id);

			if (child == null ||
				child is not NodeStatefulViewModel node)
			{
				node = new NodeStatefulViewModel(id);
				AddBranch(node);
			}

			return node;
		}

		public bool TryGetNode<T>(string id, out T node) where T : IStatefulViewModel
		{
			node = GetNode<T>(id);
			return node != null;
		}

		public T GetNode<T>(string id, bool includeSelf = false) where T : IStatefulViewModel
		{
			return _root.GetNode<T>(id, includeSelf);
		}

		public IStatefulViewModel GetNode(string id, Func<IStatefulViewModel, bool> predicate = null, bool includeSelf = false)
		{
			return _root.GetNode(id, predicate, includeSelf);
		}
	}
}
