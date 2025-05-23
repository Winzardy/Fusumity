using System;

namespace Fusumity.Utility
{
	public class Incrementer
	{
		private int _initialValue;
		private int _value;
		private Func<int, int> _incrementFunc;

		public int Current => _value;

		public Incrementer(int initialValue = 0)
		{
			_initialValue = initialValue;

			Reset();
		}

		public Incrementer(Func<int, int> function, int initialValue = 0)
		{
			_incrementFunc = function;
			_initialValue = initialValue;
			Reset();
		}

		public void Set(int value)
		{
			_value = value;
		}

		public int Get()
		{
			var currentValue = _value;

			_value = _incrementFunc?.Invoke(_value) ?? _value + 1;

			return currentValue;
		}

		public void Reset()
		{
			_value = _initialValue;
		}

		public override string ToString() => Get().ToString();
	}
}
