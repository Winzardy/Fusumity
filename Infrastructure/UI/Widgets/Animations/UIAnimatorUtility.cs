namespace UI
{
	public static class UIAnimatorUtility
	{
		public static bool IsVisibleKey(this string key)
			=> key is AnimationType.OPENING or AnimationType.CLOSING;

		/// <summary>
		/// Основная идея чтобы с помощью ключа (пример: "state/clipName") получить слой в котором проигрывается анимация и название анимации (клипа)
		/// Нужно чтобы при вызове другой анимации на этом же слое, он завершил текущую анимацию на этом слое.
		/// Если у ключа нет сепаратора ("/") то анимация проигрывается на 'нулевом' слое.
		/// </summary>
		/// <returns>Имя слоя и имя клипа</returns>
		public static (string layer, string clip) Split(this string key)
		{
			var split = key.Split(AnimationLayer.SEPARATOR);

			return split.Length == 1 ? (string.Empty, split[0]) : (split[0], split[1]);
		}
	}
}
