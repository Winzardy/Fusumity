using Sapientia.Comparison;
using UnityEngine;

namespace UI
{
	public abstract class IntComparisonStateSwitcher : StateSwitcher<int>
	{
		[Space]
		public ComparisonOperator @operator;
		public int value;

		protected sealed override void OnStateSwitched(int a)
		{
			var result = a.Compare(@operator, value);
			OnStateSwitched(result);
		}

		protected abstract void OnStateSwitched(bool value);
	}
}
