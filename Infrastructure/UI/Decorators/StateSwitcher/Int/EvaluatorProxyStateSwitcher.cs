using Fusumity.Utility;
using Sapientia;
using Sapientia.Deterministic;
using Sapientia.Evaluators;
using Sapientia.Pooling;
using UnityEngine;

namespace UI
{
	public class EvaluatorProxyStateSwitcher : StateSwitcher<int>
	{
		[Space]
		public EvaluatedValue<Blackboard, int> evaluator;

		[Space]
		public StateSwitcher<int> switcher;

		protected sealed override void OnStateSwitched(int a)
		{
			using (Pool<Blackboard>.Get(out var blackboard))
			{
				blackboard.Register(UnityRandomizer<int>.Default);
				blackboard.Register(UnityRandomizer<Fix64>.Default);
				blackboard.Register(a, "value");
				var value = evaluator.Evaluate(blackboard);
				switcher.Switch(value);
			}
		}
	}
}
