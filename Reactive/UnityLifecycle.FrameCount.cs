using UnityEngine;

namespace Fusumity.Reactive
{
	public partial class UnityLifecycle
	{
		/// <summary>
		/// Счётчик кадров, инкрементится в конце кадра (LateUpdate). В отличие от
		/// <see cref="Time.frameCount"/> читается с любого потока — использовать для
		/// покадровых кэшей вне мейн-треда. Точная фаза кэшам не важна, важно «раз за кадр».
		/// Пауза и анфокус тоже двигают счётчик: кадры стоят, а время идёт, и покадровый кэш
		/// на возврате отдал бы протухшее значение
		/// </summary>
		public static int FrameCount { get; private set; }

		private static void IncrementFrameCount() => FrameCount++;
	}
}
