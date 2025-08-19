using System;
using System.Collections.Generic;
using UnityEngine;

namespace Fusumity.Utility
{
	public static class TransformUtility
	{
		public static void SetParent(this IEnumerable<Transform> components, Transform parent)
		{
			foreach (var component in components)
			{
				component.SetParent(parent);
			}
		}

		public static T CreateChild<T>(this T parent, string name)
			where T : Transform
		{
			var obj = new GameObject(name, typeof(T));
			obj.TryGetComponent(out T transform);
			transform.SetParent(parent);
			transform.ResetTransform();
			return transform;
		}

		public static void ResetTransformSafe<T>(this T component)
			where T : Component
		{
			if (!component)
				return;

			Reset(component.transform);
		}

		public static void ResetTransform<T>(this T component) where T : Component
			=> Reset(component.transform);

		public static void ResetSafe(this Transform transform)
		{
			if (!transform)
				return;

			transform.localPosition = Vector3.zero;
			transform.localRotation = Quaternion.identity;
			transform.localScale = Vector3.one;
		}

		public static void Reset(this Transform transform)
		{
			transform.localPosition = Vector3.zero;
			transform.localRotation = Quaternion.identity;
			transform.localScale = Vector3.one;
		}

		public static void SetLayerRecursive(this Transform transform, int layer)
		{
			transform.gameObject.layer = layer;

			var childCount = transform.childCount;

			for (int i = 0; i < childCount; i++)
				SetLayerRecursive(transform.GetChild(i), layer);
		}

		public static TransformSnapshot Snapshot(this Transform transform, TransformSpace space) => new(transform, space);

		public static void Apply(this Transform transform, in TransformSnapshot entry, bool useScale = true)
			=> entry.ApplyTo(transform, useScale);

		public static void ApplyTo(this in TransformSnapshot snapshot, Transform transform, bool useScale = true)
			=> ApplyTo(in snapshot.entry, transform, snapshot.space, useScale);

		public static void Apply(this Transform transform, in TransformEntry entry, TransformSpace space, bool useScale = true)
			=> entry.ApplyTo(transform, space, useScale);

		public static void ApplyTo(this in TransformEntry entry, Transform transform, TransformSpace space, bool useScale = true)
		{
			if (space == TransformSpace.Local)
			{
				transform.localPosition = entry.position;
				transform.localRotation = entry.rotation;
				if (useScale)
					transform.localScale = entry.scale;
				return;
			}

			transform.position = entry.position;
			transform.rotation = entry.rotation;

			if (!useScale)
				return;

			var targetLossyScale = transform.lossyScale;
			var factor = new Vector3(
				Mathf.Approximately(targetLossyScale.x, 0f) ? 0f : entry.scale.x / targetLossyScale.x,
				Mathf.Approximately(targetLossyScale.y, 0f) ? 0f : entry.scale.y / targetLossyScale.y,
				Mathf.Approximately(targetLossyScale.z, 0f) ? 0f : entry.scale.z / targetLossyScale.z
			);

			var local = transform.localScale;
			transform.localScale = new Vector3(
				local.x * factor.x,
				local.y * factor.y,
				local.z * factor.z
			);
		}
	}

	public enum TransformSpace
	{
		Local,
		World
	}

	[Serializable]
	public struct TransformSnapshot
	{
		public TransformSpace space;

		public TransformEntry entry;

		public TransformSnapshot(Transform transform, TransformSpace space) :
			this(
				space,
				space == TransformSpace.Local
					? transform.localPosition
					: transform.position,
				space == TransformSpace.Local
					? transform.localRotation
					: transform.rotation,
				space == TransformSpace.Local
					? transform.localScale
					: transform.lossyScale)
		{
		}

		public TransformSnapshot(TransformSpace space, Vector3 position, Quaternion rotation, Vector3 scale)
			: this(space, new TransformEntry(position, rotation, scale))
		{
		}

		public TransformSnapshot(TransformSpace space, in TransformEntry entry)
		{
			this.space = space;
			this.entry = entry;
		}

		public override string ToString() => $"Space: {space}, Entry [ {entry} ]";
	}

	/// <summary>
	/// У юнитеков есть <see cref="Pose"/>, но у них нет scale
	/// </summary>
	[Serializable]
	public struct TransformEntry
	{
		public Vector3 position;
		public Quaternion rotation;
		public Vector3 scale;

		public TransformEntry(Vector3 position, Quaternion rotation, Vector3 scale)
		{
			this.position = position;
			this.rotation = rotation;
			this.scale = scale;
		}

		public static TransformEntry Identity = new(Vector3.zero, Quaternion.identity, Vector3.one);

		public override string ToString()
			=> $"Position: {position}," +
				$" Rotation: {rotation}," +
				$" Scale: {scale}";
	}
}
