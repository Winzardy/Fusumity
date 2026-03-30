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
