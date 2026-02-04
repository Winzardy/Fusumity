using Fusumity.Utility;
using System;

namespace Fusumity.MVVM
{
	public class BindingSubscription<T> : ActionSubscription<T>
	{
		private IBinding<T> _binding;

		public BindingSubscription(IBinding<T> binding, Action<T> handler) : base(handler)
		{
			_binding = binding;
			_binding.Bind(Invoke);
		}

		protected override void OnDispose()
		{
			_binding.Release();
		}
	}

	public interface IBinding<T>
	{
		void Bind(Action<T> action, bool invokeOnBind = true);
		void Release();
	}
}
