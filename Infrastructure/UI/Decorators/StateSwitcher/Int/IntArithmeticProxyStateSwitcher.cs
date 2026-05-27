using Sapientia;
using UnityEngine;

namespace UI
{
	public class IntArithmeticProxyStateSwitcher : StateSwitcher<int>
	{
		[Space]
		public ArithmeticOperator @operator;
		public int value;

		[Space]
		public StateSwitcher<int> switcher;

		protected sealed override void OnStateSwitched(int a)
		{
			var result = a.Operate(@operator, value);
			switcher.Switch(result);
		}
	}
}
