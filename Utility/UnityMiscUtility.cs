using System.Collections.Generic;
using Sapientia.Collections;
using UnityEngine;

namespace Fusumity.Utility
{
	/// <summary>
	/// https://www.notion.so/Extension-b985410501c742dabb3a08ca171a319c?pvs=4#daebb5eacd514cf3ae584dec4b7dbe29
	/// </summary>
	public static class UnityMiscUtility
	{
		private static readonly SimpleList<ParticleSystem.Particle> _particleBuffer = new();

		public static SimpleList<ParticleSystem.Particle> GetParticles(this ParticleSystem particleSystem)
		{
			var particleCount = particleSystem.particleCount;
			_particleBuffer.ClearFast();
			_particleBuffer.Expand(particleCount);
			var count = particleSystem.GetParticles(_particleBuffer.GetInnerArray(), particleCount);
			_particleBuffer.SetCount(count);

			return _particleBuffer;
		}

		public static void SetParticles(this ParticleSystem particleSystem, SimpleList<ParticleSystem.Particle> particles)
		{
			particleSystem.SetParticles(particles.GetInnerArray(), particles.Count);
		}

		public static float EvaluateFromProgress(this AnimationCurve function, float progress)
		{
			var functionTime = function.keys[^1].time * progress;
			return function.Evaluate(functionTime);
		}

		public static void SetLocalScale(this IEnumerable<Transform> components, Vector3 localScale)
		{
			foreach (var component in components)
			{
				component.localScale = localScale;
			}
		}

		public static void Destroy(this Object target)
		{
			if (Application.isPlaying)
				Object.Destroy(target);
			else
				Object.DestroyImmediate(target, true);
		}

		public static void SetLossyScale(this Transform target, Vector3 scale)
		{
			var lossyScale = target.lossyScale;
			var localScale = target.localScale;
			target.localScale = new Vector3(scale.x * localScale.x / lossyScale.x, scale.y * localScale.y / lossyScale.y,
				scale.z * localScale.z / lossyScale.z);
		}
	}
}
