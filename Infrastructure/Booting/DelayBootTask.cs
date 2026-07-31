using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Fusumity.Attributes;
using Sapientia;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Booting
{
	/// <summary>
	/// Дле тестовых целей и отладки
	/// </summary>
	[TypeRegistryItem(
		"\u2009Delay", //В начале делаем отступ из-за отрисовки...
		"",
		SdfIconType.Clock)]
	[Serializable]
	public class DelayBootTask : BaseBootTask, IWeightedProgress
	{
		[SerializeField]
		[UnitParent(Units.Second)]
		private Toggle<float> delay;

		public float Weight => delay;

		protected override async UniTask RunTaskAsync(Blackboard _, IProgress<BootProgressInfo> progress = null, CancellationToken token = default)
		{
			var time = 0f;
			while (time < delay)
			{
				progress?.Report(new BootProgressInfo(time / delay));
				await UniTask.NextFrame(token);
				time += Time.deltaTime;
				token.ThrowIfCancellationRequested();
			}
		}
	}
}
