using System;
using UnityEngine;

namespace AssetManagement
{
	/// <summary>
	/// Опрашиваемый приёмник прогресса загрузки ассета. <br/>
	/// Передаётся в загрузку как <see cref="IProgress{T}"/>, значение (нормализованное [0..1])
	/// опрашивается через <see cref="value"/> (например из Update)
	/// </summary>
	public sealed class AsyncOperationProgress : IProgress<float>
	{
		/// <summary>
		/// Нормализованный прогресс загрузки [0..1]
		/// </summary>
		public float value { get; private set; }

		/// <summary>
		/// Загрузка завершена
		/// </summary>
		public bool IsDone { get => value >= 1f; }

		//UniTask репортит handle.PercentComplete каждый кадр
		void IProgress<float>.Report(float progress)
			=> value = Mathf.Clamp01(progress);
	}
}
