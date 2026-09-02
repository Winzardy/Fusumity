using DG.Tweening;
using UnityEngine;

namespace ZenoTween.Utility
{
	public static class TweenUtility
	{
		public static void KillWithCallbacks(this Tween tween)
		{
			if (!tween.IsActive())
				return;
			tween.Complete(true);
			tween.Kill();
		}

		public static void KillSafe(this Tween tween, bool complete = false)
		{
			if (!tween.IsActive())
				return;

			tween.Kill(complete);
		}

		public static bool IsPlayingSafe(this Tween tween)
		{
			if (!tween.IsActive())
				return false;

			return tween.IsPlaying();
		}

		/// <summary>
		/// Привязывает время жизни твина к объекту: при уничтожении объекта твин будет убит
		/// </summary>
		/// <remarks>
		/// Ставить только на корневые твины и Sequence. Убийство твина, вложенного
		/// в Sequence, ломает саму Sequence — вложенные живут временем жизни родителя
		/// </remarks>
		public static T LinkTo<T>(this T tween, object owner, LinkBehaviour behaviour = LinkBehaviour.KillOnDestroy)
			where T : Tween
		{
			if (!tween.IsActive())
				return tween;

			var link = owner switch
			{
				GameObject gameObject => gameObject,
				Component component => component.gameObject,
				_ => null
			};

			if (!link)
				return tween;

			return tween.SetLink(link, behaviour);
		}
	}
}
