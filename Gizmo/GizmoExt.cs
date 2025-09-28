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
		private const float HEIGHT = 0.5f;
		private const int CIRCLE_PRECISE = 60;
		private const int FRAMES_FOR_ONCE_DRAWING = 10;

		private const float ARROW_HANDLE_ANGLE_RAD = FloatMathExt.DEG_TO_RAD * 20f;
		private static readonly float3 ARROW_RIGHT_DIR = (new Rotation(ARROW_HANDLE_ANGLE_RAD).ToDirection() * 0.5f).XZ();
		private static readonly float3 ARROW_LEFT_DIR = (new Rotation(-ARROW_HANDLE_ANGLE_RAD).ToDirection() * 0.5f).XZ();

		private static readonly Vector3[] CIRCLE_POINTS = new Vector3[CIRCLE_PRECISE];

		private static readonly SimpleList<(float2 positionA, float2 positionB, Color color, int frames)> DRAW_LINES = new();
		private static readonly SimpleList<(float2 basePos, float2[] points, Rotation rotation, Color color, int frames)> DRAW_POLYGONS = new();
		private static readonly SimpleList<(float2 position, float radius, Color color, int frames)> DRAW_CIRCLES = new();
		private static readonly SimpleList<(float2 position, float3 size, Color color, bool wire, int frames)> DRAW_SPHERES = new();
		private static readonly SimpleList<(float2 position, Rotation rotation, float radius, float rad, Color color, int frames)> DRAW_SECTORS = new();
		private static readonly SimpleList<(float2 position, Rotation rotation, float minRadius, float maxRadius, float rad, Color color, int frames)> DRAW_CUT_SECTORS = new();
		private static readonly SimpleList<(float2 position, Rotation rotation, float2 size, Color color, int frames)> DRAW_BOXES = new();
		private static readonly SimpleList<(float2 position, Rotation rotation, float3 size, Color color, bool wire, int frames)> DRAW_CUBES = new();
		private static readonly SimpleList<(float2 position, Rotation rotation, float2 size, Color color, int frames)> DRAW_ARROWS = new();

		public static readonly Color ORANGE = new (1f, 0.5f, 0f, 1f);
		public static readonly Color PURPLE = new (0.5f, 0f, 1f, 1f);
		public static readonly Color JADE_WHISPER = new (0.3f, 0.8f, 0.6f, 1f);

		public static bool IsEnabled { get; internal set; }

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

		public static void ClearGizmo()
		{
			for (var i = 0; i < DRAW_LINES.Count;)
			{
				var circle = DRAW_LINES[i];
				if (circle.frames-- < 1)
				{
					DRAW_LINES.RemoveAtSwapBack(i);
				}
				else
				{
					DRAW_LINES[i] = circle;
					i++;
				}
			}
			for (var i = 0; i < DRAW_POLYGONS.Count;)
			{
				var circle = DRAW_POLYGONS[i];
				if (circle.frames-- < 1)
				{
					DRAW_POLYGONS.RemoveAtSwapBack(i);
				}
				else
				{
					DRAW_POLYGONS[i] = circle;
					i++;
				}
			}
			for (var i = 0; i < DRAW_CIRCLES.Count;)
			{
				var circle = DRAW_CIRCLES[i];
				if (circle.frames-- < 1)
				{
					DRAW_CIRCLES.RemoveAtSwapBack(i);
				}
				else
				{
					DRAW_CIRCLES[i] = circle;
					i++;
				}
			}
			for (var i = 0; i < DRAW_SPHERES.Count;)
			{
				var sphere = DRAW_SPHERES[i];
				if (sphere.frames-- < 1)
				{
					DRAW_SPHERES.RemoveAtSwapBack(i);
				}
				else
				{
					DRAW_SPHERES[i] = sphere;
					i++;
				}
			}
			for (var i = 0; i < DRAW_SECTORS.Count;)
			{
				var sector = DRAW_SECTORS[i];
				if (sector.frames-- < 1)
				{
					DRAW_SECTORS.RemoveAtSwapBack(i);
				}
				else
				{
					DRAW_SECTORS[i] = sector;
					i++;
				}
			}
			for (var i = 0; i < DRAW_CUT_SECTORS.Count;)
			{
				var sector = DRAW_CUT_SECTORS[i];
				if (sector.frames-- < 1)
				{
					DRAW_CUT_SECTORS.RemoveAtSwapBack(i);
				}
				else
				{
					DRAW_CUT_SECTORS[i] = sector;
					i++;
				}
			}
			for (var i = 0; i < DRAW_BOXES.Count;)
			{
				var box = DRAW_BOXES[i];
				if (box.frames-- < 1)
				{
					DRAW_BOXES.RemoveAtSwapBack(i);
				}
				else
				{
					DRAW_BOXES[i] = box;
					i++;
				}
			}
			for (var i = 0; i < DRAW_CUBES.Count;)
			{
				var cube = DRAW_CUBES[i];
				if (cube.frames-- < 1)
				{
					DRAW_CUBES.RemoveAtSwapBack(i);
				}
				else
				{
					DRAW_CUBES[i] = cube;
					i++;
				}
			}
			for (var i = 0; i < DRAW_ARROWS.Count;)
			{
				var arrow = DRAW_ARROWS[i];
				if (arrow.frames-- < 1)
				{
					DRAW_ARROWS.RemoveAtSwapBack(i);
				}
				else
				{
					DRAW_ARROWS[i] = arrow;
					i++;
				}
			}
		}

		public static void DrawGizmo()
		{
			foreach (var line in DRAW_LINES)
			{
				DrawLine_TopDown(line.positionA, line.positionB, line.color);
			}
			foreach (var polygon in DRAW_POLYGONS)
			{
				DrawPolygon_TopDown(polygon.basePos, polygon.points, polygon.rotation, polygon.color);
			}
			foreach (var circle in DRAW_CIRCLES)
			{
				DrawCircle_TopDown(circle.position, circle.radius, circle.color);
			}
			foreach (var sphere in DRAW_SPHERES)
			{
				DrawSphere_TopDown(sphere.position, sphere.size, sphere.color, sphere.wire);
			}
			foreach (var sector in DRAW_SECTORS)
			{
				DrawSector_TopDown(sector.position, sector.rotation, sector.radius, sector.rad, sector.color);
			}
			foreach (var sector in DRAW_CUT_SECTORS)
			{
				DrawCutSector_TopDown(sector.position, sector.rotation, sector.minRadius, sector.maxRadius, sector.rad, sector.color);
			}
			foreach (var box in DRAW_BOXES)
			{
				DrawBox_TopDown(box.position, box.rotation, box.size, box.color);
			}
			foreach (var box in DRAW_CUBES)
			{
				DrawCube_TopDown(box.position, box.rotation, box.size, box.color, box.wire);
			}
			foreach (var arrow in DRAW_ARROWS)
			{
				DrawArrow_TopDown(arrow.position, arrow.rotation, arrow.size, arrow.color);
			}
		}

		[Conditional(E.UNITY_EDITOR)]
		public static void RequestDrawLineOnce_TopDown(float2 positionA, float2 positionB, Color color)
		{
			RequestDrawLine_TopDown(positionA, positionB, color, FRAMES_FOR_ONCE_DRAWING);
		}

		[Conditional(E.UNITY_EDITOR)]
		public static void RequestDrawLine_TopDown(float2 positionA, float2 positionB, Color color, int frames = 1)
		{
			if (!IsEnabled)
				return;
			DRAW_LINES.Add((positionA, positionB, color, frames));
		}

		[Conditional(E.UNITY_EDITOR)]
		public static void RequestDrawPolygon_TopDown(float2 basePos, Span<float2> pointsSpan, Rotation rotation, Color color, int frames = 1)
		{
			if (!IsEnabled)
				return;
			DRAW_POLYGONS.Add((basePos, pointsSpan.ToArray(), rotation, color, frames));
		}

		[Conditional(E.UNITY_EDITOR)]
		public static void RequestDrawCircleOnce_TopDown(float2 position, float radius, Color color)
		{
			RequestDrawCircle_TopDown(position, radius, color, FRAMES_FOR_ONCE_DRAWING);
		}

		[Conditional(E.UNITY_EDITOR)]
		public static void RequestDrawCircle_TopDown(float2 position, float radius, Color color, int frames = 1)
		{
			if (!IsEnabled)
				return;
			DRAW_CIRCLES.Add((position, radius, color, frames));
		}

		[Conditional(E.UNITY_EDITOR)]
		public static void RequestDrawSphere_TopDown(float2 position, float3 size, Color color, bool wire = false, int frames = 1)
		{
			if (!IsEnabled)
				return;
			DRAW_SPHERES.Add((position, size, color, wire, frames));
		}

		[Conditional(E.UNITY_EDITOR)]
		public static void RequestDrawCube_TopDown(float2 position, Rotation rotation, float3 size, Color color, bool wire = false, int frames = 1)
		{
			if (!IsEnabled)
				return;
			DRAW_CUBES.Add((position, rotation, size, color, wire, frames));
		}

		[Conditional(E.UNITY_EDITOR)]
		public static void RequestDrawSectorOnce_TopDown(float2 position, Rotation rotation, float radius, float rad, Color color)
		{
			RequestDrawSector_TopDown(position, rotation, radius, rad, color, FRAMES_FOR_ONCE_DRAWING);
		}

		[Conditional(E.UNITY_EDITOR)]
		public static void RequestDrawSector_TopDown(float2 position, Rotation rotation, float radius, float rad, Color color, int frames = 1)
		{
			if (!IsEnabled)
				return;
			DRAW_SECTORS.Add((position, rotation, radius, rad, color, frames));
		}

		[Conditional(E.UNITY_EDITOR)]
		public static void RequestDrawCutSectorOnce_TopDown(float2 position, Rotation rotation, float minRadius, float maxRadius, float rad, Color color)
		{
			RequestDrawCutSector_TopDown(position, rotation, minRadius, maxRadius, rad, color, FRAMES_FOR_ONCE_DRAWING);
		}

		[Conditional(E.UNITY_EDITOR)]
		public static void RequestDrawCutSector_TopDown(float2 position, Rotation rotation, float minRadius, float maxRadius, float rad, Color color, int frames = 1)
		{
			if (!IsEnabled)
				return;
			DRAW_CUT_SECTORS.Add((position, rotation, minRadius, maxRadius, rad, color, frames));
		}

		[Conditional(E.UNITY_EDITOR)]
		public static void RequestDrawBoxOnce_TopDown(float2 position, Rotation rotation, float2 size, Color color)
		{
			RequestDrawBox_TopDown(position, rotation, size, color, FRAMES_FOR_ONCE_DRAWING);
		}

		[Conditional(E.UNITY_EDITOR)]
		public static void RequestDrawBox_TopDown(float2 position, Rotation rotation, float2 size, Color color, int frames = 1)
		{
			if (!IsEnabled)
				return;
			DRAW_BOXES.Add((position, rotation, size, color, frames));
		}

		[Conditional(E.UNITY_EDITOR)]
		public static void RequestDrawArrowOnce_TopDown(float2 position, Rotation rotation, float2 size, Color color)
		{
			RequestDrawArrow_TopDown(position, rotation, size, color, FRAMES_FOR_ONCE_DRAWING);
		}

		[Conditional(E.UNITY_EDITOR)]
		public static void RequestDrawArrow_TopDown(float2 position, Rotation rotation, float2 size, Color color, int frames = 1)
		{
			if (!IsEnabled)
				return;
			DRAW_ARROWS.Add((position, rotation, size, color, frames));
		}

		[Conditional(E.UNITY_EDITOR)]
		public static void DrawLine_TopDown(float2 positionA, float2 positionB, Color color)
		{
			var delta = positionB - positionA;

			var oldMatrix = Gizmos.matrix;
			Gizmos.color = color;
			Gizmos.matrix = Matrix4x4.TRS(positionA.XZ(HEIGHT), Quaternion.identity, new Vector3(delta.x, 1f, delta.y));

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
			Gizmos.matrix = Matrix4x4.TRS(basePos.XZ(HEIGHT), rotation.ToQuaternion(), new Vector3(scale.x, scale.x, scale.y));

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
			Gizmos.matrix = Matrix4x4.TRS(position.XZ(HEIGHT), Quaternion.identity, new Vector3(radius, 1f, radius));

			Gizmos.DrawLineStrip(CIRCLE_POINTS, true);

			Gizmos.matrix = oldMatrix;
		}

		[Conditional(E.UNITY_EDITOR)]
		public static void DrawSphere_TopDown(float2 position, float3 size, Color color, bool wire)
		{
			var oldMatrix = Gizmos.matrix;
			Gizmos.color = color;
			Gizmos.matrix = Matrix4x4.TRS(position.XZ(HEIGHT), Quaternion.identity, size);

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
			Gizmos.matrix = Matrix4x4.TRS(position.XZ(HEIGHT), quaternion.RotateY(-rotation), new Vector3(radius, 1f, radius));

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
			Gizmos.matrix = Matrix4x4.TRS(position.XZ(HEIGHT), quaternion.RotateY(-rotation), new Vector3(maxRadius, 1f, maxRadius));

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
			Gizmos.matrix = Matrix4x4.TRS(position.XZ(HEIGHT), rotation.ToQuaternion(), size.XZ(1f));

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
			Gizmos.matrix = Matrix4x4.TRS(position.XZ(HEIGHT), rotation.ToQuaternion(), size);

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
			Gizmos.matrix = Matrix4x4.TRS(position.XZ(HEIGHT), rotation.ToQuaternion(), size.XZ(0f));

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
