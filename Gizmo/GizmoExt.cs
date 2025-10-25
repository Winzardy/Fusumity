using System;
using System.Collections.Generic;
using System.Diagnostics;
using Fusumity.Utility;
using Sapientia;
using Sapientia.Collections;
using Sapientia.Extensions;
using Unity.Mathematics;
using UnityEngine;
using Object = UnityEngine.Object;
using Vector3 = UnityEngine.Vector3;

namespace Game.Logic.Gizmo
{
	public static class GizmoExt
	{
		private const float DEFAULT_HEIGHT = 0.5f;
		private const int CIRCLE_PRECISE = 60;
		public const int FRAMES_FOR_ONCE_DRAWING = 10;

		private const float ARROW_HANDLE_ANGLE_RAD = FloatMathExt.DEG_TO_RAD * 20f;
		private static readonly float3 ARROW_RIGHT_DIR = (new Rotation(ARROW_HANDLE_ANGLE_RAD).ToDirection() * 0.5f).XZ();
		private static readonly float3 ARROW_LEFT_DIR = (new Rotation(-ARROW_HANDLE_ANGLE_RAD).ToDirection() * 0.5f).XZ();

		private static readonly Vector3[] CIRCLE_POINTS = new Vector3[CIRCLE_PRECISE];

		public static readonly Color ORANGE = new (1f, 0.5f, 0f, 1f);
		public static readonly Color PURPLE = new (0.5f, 0f, 1f, 1f);
		public static readonly Color JADE_WHISPER = new (0.3f, 0.8f, 0.6f, 1f);

		public static float Height => GizmoDrawer.Current == null ? DEFAULT_HEIGHT : GizmoDrawer.Current.Height;

		public static Color GetRandomDebugColor()
		{
			var seed = DateTime.Now.Millisecond;
			return GetRandomDebugColor(seed);
		}

		public static Color GetRandomDebugColor(int seed)
		{
			var currentState = UnityEngine.Random.state;

			UnityEngine.Random.InitState(seed);

			var result = UnityEngine.Random.ColorHSV(
				0f, 1f, // H: весь круг оттенков
				0.7f, 1f, // S: высокая насыщенность
				0.6f, 0.9f, // V: умеренно высокая яркость
				1f, 1f // A: полностью непрозрачный
			);

			UnityEngine.Random.state = currentState;
			return result;
		}

		static GizmoExt()
		{
			var radStep = FloatMathExt.TWO_PI / (CIRCLE_PRECISE - 1);
			var currentRad = 0f;

			CIRCLE_POINTS[0] = Vector2.right;
			for (var i = 1; i < CIRCLE_PRECISE; i++)
			{
				currentRad += radStep;
				CIRCLE_POINTS[i] = currentRad.CosSin().XZ();
			}

			if (!Application.isPlaying)
				return;

			var gameObject = new GameObject($"{nameof(GizmoDrawer)}");
			Object.DontDestroyOnLoad(gameObject);
			gameObject.AddComponent<GizmoDrawer>();
		}

		[Conditional(E.UNITY_EDITOR)]
		public static void DrawLine_TopDown(float2 positionA, float2 positionB, Color color)
		{
			var delta = positionB - positionA;

			var oldMatrix = Gizmos.matrix;
			Gizmos.color = color;
			Gizmos.matrix = Matrix4x4.TRS(positionA.XZ(Height), Quaternion.identity, new Vector3(delta.x, 1f, delta.y));

			var from = new Vector3(0, 0f, 0);
			var to = new Vector3(1f, 0f, 1f);

			Gizmos.DrawLine(from, to);

			Gizmos.matrix = oldMatrix;
		}

		[Conditional(E.UNITY_EDITOR)]
		public static void DrawPolygon_TopDown(float2 basePos, IList<float2> points, Rotation rotation, Color color)
		{
			DrawPolygon_TopDown(basePos, points, new float2(1f), rotation, color);
		}

		[Conditional(E.UNITY_EDITOR)]
		public static void DrawPolygon_TopDown(float2 basePos, IList<float2> points, float2 scale, Rotation rotation, Color color)
		{
			var oldMatrix = Gizmos.matrix;
			Gizmos.color = color;
			Gizmos.matrix = Matrix4x4.TRS(basePos.XZ(Height), rotation.ToQuaternion(), new Vector3(scale.x, scale.x, scale.y));

			Span<Vector3> vectors = stackalloc Vector3[points.Count];
			for (var i = 0; i < points.Count; i++)
			{
				vectors[i] = points[i].XZ(0);
			}

			Gizmos.DrawLineStrip(vectors, true);

			Gizmos.matrix = oldMatrix;
		}

		[Conditional(E.UNITY_EDITOR)]
		public static void DrawCircle_TopDown(float2 position, float radius, Color color)
		{
			var oldMatrix = Gizmos.matrix;
			Gizmos.color = color;
			Gizmos.matrix = Matrix4x4.TRS(position.XZ(Height), Quaternion.identity, new Vector3(radius, 1f, radius));

			Gizmos.DrawLineStrip(CIRCLE_POINTS, true);

			Gizmos.matrix = oldMatrix;
		}

		[Conditional(E.UNITY_EDITOR)]
		public static void DrawSphere_TopDown(float2 position, float3 size, Color color, bool wire)
		{
			var oldMatrix = Gizmos.matrix;
			Gizmos.color = color;
			Gizmos.matrix = Matrix4x4.TRS(position.XZ(Height), Quaternion.identity, size);

			if (wire)
				Gizmos.DrawWireSphere(Vector3.zero, 0.5f);
			else
				Gizmos.DrawSphere(Vector3.zero, 0.5f);

			Gizmos.matrix = oldMatrix;
		}

		[Conditional(E.UNITY_EDITOR)]
		public static void DrawSector_TopDown(float2 position, Rotation rotation, float radius, float rad, Color color)
		{
			var oldMatrix = Gizmos.matrix;
			Gizmos.color = color;
			// Don't use rotation.ToQuaternion because subsequent code calculates points in 2D.
			// If we use rotation.ToQuaternion so we need to work with vector float2(0, 1) as rotation pivot point
			Gizmos.matrix = Matrix4x4.TRS(position.XZ(Height), quaternion.RotateY(-rotation), new Vector3(radius, 1f, radius));

			var halfRad = rad / 2;
			var precise = (CIRCLE_PRECISE * (rad / FloatMathExt.TWO_PI)).FloorToInt_Positive();
			if (precise <= 3)
			{
				var left = halfRad.CosSin();
				var right = (-halfRad).CosSin();

				Span<Vector3> lines = stackalloc Vector3[]
				{
					Vector3.zero,
					left.XZ(),
					Vector3.right,
					right.XZ(),
				};
				Gizmos.DrawLineStrip(lines, true);
			}
			else
			{
				var radStep = rad / precise;
				var currentRad = -halfRad;

				Span<Vector3> lines = stackalloc Vector3[precise + 2];

				for (var i = 0; i <= precise; i++, currentRad += radStep)
				{
					lines[i] = currentRad.CosSin().XZ();
				}
				lines[^1] = Vector3.zero;

				Gizmos.DrawLineStrip(lines, true);
			}

			Gizmos.matrix = oldMatrix;
		}

		[Conditional(E.UNITY_EDITOR)]
		public static void DrawCutSector_TopDown(float2 position, Rotation rotation, float minRadius, float maxRadius, float rad, Color color)
		{
			var oldMatrix = Gizmos.matrix;
			Gizmos.color = color;
			// Don't use rotation.ToQuaternion because subsequent code calculates points in 2D.
			// If we use rotation.ToQuaternion so we need to work with vector float2(0, 1) as rotation pivot point
			Gizmos.matrix = Matrix4x4.TRS(position.XZ(Height), quaternion.RotateY(-rotation), new Vector3(maxRadius, 1f, maxRadius));

			var halfRad = rad / 2;
			var minRadiusCoef = minRadius / maxRadius;

			var precise = (CIRCLE_PRECISE * (rad / FloatMathExt.TWO_PI)).FloorToInt_Positive();
			if (precise <= 3)
			{
				var left = halfRad.CosSin();
				var right = (-halfRad).CosSin();

				Span<Vector3> lines = stackalloc Vector3[]
				{
					left.XZ(),
					Vector3.right,
					right.XZ(),
					right.XZ() * minRadiusCoef,
					Vector3.right * minRadiusCoef,
					left.XZ() * minRadiusCoef,
				};

				Gizmos.DrawLineStrip(lines, true);
			}
			else
			{
				var radStep = rad / precise;
				var currentRad = -halfRad;

				Span<Vector3> lines = stackalloc Vector3[(precise + 1) * 2];

				for (var i = 0; i <= precise; i++, currentRad += radStep)
				{
					var cosSin = currentRad.CosSin();
					lines[i] = cosSin.XZ();
					lines[precise + 1 + i] = (cosSin * minRadiusCoef).XZ();
				}

				Gizmos.DrawLineStrip(lines, true);
			}

			Gizmos.matrix = oldMatrix;
		}

		[Conditional(E.UNITY_EDITOR)]
		public static void DrawBox_TopDown(float2 position, Rotation rotation, float2 size, Color color)
		{
			var oldMatrix = Gizmos.matrix;
			Gizmos.color = color;
			Gizmos.matrix = Matrix4x4.TRS(position.XZ(Height), rotation.ToQuaternion(), size.XZ(1f));

			var leftUp = new Vector3(-1f, 0f, 1f);
			var rightUp = new Vector3(1f, 0f, 1f);
			var rightDown = -leftUp;
			var leftDown = -rightUp;

			Span<Vector3> lines = stackalloc Vector3[]
			{
				leftUp,
				rightUp,
				rightDown,
				leftDown,
			};

			Gizmos.DrawLineStrip(lines, true);

			Gizmos.matrix = oldMatrix;
		}

		[Conditional(E.UNITY_EDITOR)]
		public static void DrawCube_TopDown(float2 position, Rotation rotation, float3 size, Color color, bool wire)
		{
			var oldMatrix = Gizmos.matrix;
			Gizmos.color = color;
			Gizmos.matrix = Matrix4x4.TRS(position.XZ(Height), rotation.ToQuaternion(), size);

			if (wire)
				Gizmos.DrawWireCube(Vector3.zero, Vector3.one);
			else
				Gizmos.DrawCube(Vector3.zero, Vector3.one);

			Gizmos.matrix = oldMatrix;
		}

		[Conditional(E.UNITY_EDITOR)]
		public static void DrawArrow_TopDown(float2 position, Rotation rotation, float2 size, Color color)
		{
			var oldMatrix = Gizmos.matrix;
			Gizmos.color = color;
			Gizmos.matrix = Matrix4x4.TRS(position.XZ(Height), rotation.ToQuaternion(), size.XZ(0f));

			var arrowHandle = (Span<Vector3>)stackalloc Vector3[3];
			arrowHandle[0] = new float3(0.5f, 0f, 0f) - ARROW_RIGHT_DIR;
			arrowHandle[1] = new float3(0.5f, 0f, 0f);
			arrowHandle[2] = new float3(0.5f, 0f, 0f) - ARROW_LEFT_DIR;

			Gizmos.DrawLine(new float3(-0.5f, 0f, 0f), new float3(0.5f, 0f, 0f));
			Gizmos.DrawLineStrip(arrowHandle, false);

			Gizmos.matrix = oldMatrix;
		}
	}
}
