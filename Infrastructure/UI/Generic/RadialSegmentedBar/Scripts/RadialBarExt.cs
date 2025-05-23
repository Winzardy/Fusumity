using System;
using System.Reflection;
using UnityEngine;
using Random = UnityEngine.Random;

#if UNITY_EDITOR
#endif

namespace UI.RadialBar.Extensions
{
	public static class RadialBarExt
	{
		public static Texture2D ToTexture2D(this AnimationCurve curve, int width = 500, int height = 1)
		{
			if (curve == null) return new Texture2D(1, 1);
			Texture2D texture = new Texture2D(width, height, TextureFormat.RGBAFloat, false);
			texture.wrapMode = TextureWrapMode.Clamp;
			for (int i = 0; i < width; i++)
			{
				float v = curve.Evaluate((float) i / width);
				Color c = new Color(v, v, v, v);
				for (int j = 0; j < height; j++)
				{
					texture.SetPixel(i, j, c);
				}
			}

			texture.Apply();
			return texture;
		}

		public static Texture2D ToTexture2D(this Gradient gradient, int width = 250, int height = 1)
		{
			if (gradient == null) return new Texture2D(1, 1);
			Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
			texture.wrapMode = TextureWrapMode.Clamp;
			if (gradient.mode == GradientMode.Fixed)
			{
				texture.filterMode = FilterMode.Point;
			}

			for (int i = 0; i < width; i++)
			{
				Color c = gradient.Evaluate((float) i / width);
				for (int j = 0; j < height; j++)
				{
					texture.SetPixel(i, j, c);
				}
			}

			texture.Apply();
			return texture;
		}

		public static bool EqualTo(this Gradient a, Gradient b, int samples = 5)
		{
			if (a.mode != b.mode || a.colorKeys.Length != b.colorKeys.Length || a.alphaKeys.Length != b.alphaKeys.Length) return false;

			for (int i = 0; i < a.colorKeys.Length; i++)
			{
				if (!a.colorKeys[i].Equals(b.colorKeys[i]))
					return false;
			}

			for (int i = 0; i < a.alphaKeys.Length; i++)
			{
				if (!a.alphaKeys[i].Equals(b.alphaKeys[i]))
					return false;
			}

			// if (a.mode == GradientMode.Fixed) {
			// 	samples *= 2;
			// }
			// for (int i = 0; i < samples; i++) {
			// 	float time = Random.value;
			// 	if (a.Evaluate(time) != b.Evaluate(time)) {
			// 		return false;
			// 	}
			// }
			return true;
		}

		public static Gradient Clone(this Gradient gradient)
		{
			Gradient clone = new Gradient();
			clone.mode = gradient.mode;
			clone.alphaKeys = (GradientAlphaKey[]) gradient.alphaKeys.Clone();
			clone.colorKeys = (GradientColorKey[]) gradient.colorKeys.Clone();
			return clone;
		}

		public static bool EqualTo(this AnimationCurve a, AnimationCurve b)
		{
			if (a.length != b.length) return false;

			for (int i = 0; i < a.keys.Length; i++)
			{
				var key1 = a.keys[i];
				var key2 = b.keys[i];
				if (key1.time != key2.time || key1.value != key2.value)
					return false;
			}

			return true;
		}

		public static AnimationCurve Clone(this AnimationCurve curve)
		{
			AnimationCurve clone = new AnimationCurve();
			clone.keys = (Keyframe[]) curve.keys.Clone();
			return clone;
		}

		public static Color RandomColor(this Color color, bool includeAlpha = false, float alphaCutoff = 0.5f)
		{
			float alpha = Convert.ToInt32(includeAlpha) * Random.value;
			return new Color(Random.value, Random.value, Random.value,
				(alpha <= alphaCutoff ? 0 : alpha) + Convert.ToInt32(!includeAlpha));
		}

#if UNITY_EDITOR
		static MethodInfo setIconEnabled;

		static MethodInfo SetIconEnabled => setIconEnabled = setIconEnabled ??
			Assembly.GetAssembly(typeof(UnityEditor.Editor))?.GetType("UnityEditor.AnnotationUtility")
			  ?.GetMethod("SetIconEnabled", BindingFlags.Static | BindingFlags.NonPublic);

		public static void SetGizmoIconEnabled(Type type, bool on)
		{
			if (SetIconEnabled == null) return;
			const int MONO_BEHAVIOR_CLASS_ID = 114; // https://docs.unity3d.com/Manual/ClassIDReference.html
			SetIconEnabled.Invoke(null, new object[] {MONO_BEHAVIOR_CLASS_ID, type.Name, on ? 1 : 0});
		}
#endif
	}
}
