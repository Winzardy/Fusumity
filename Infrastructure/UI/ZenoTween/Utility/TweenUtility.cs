using DG.Tweening;

namespace ZenoTween.Utility
{
	public static class TweenUtility
	{
		public static void KillSafe(this Tween tween, bool complete = false)
		{
			if (!tween.IsActive())
				return;

			tween.Kill(complete);
		}
	}
}
