using System;
using System.Diagnostics;
using Fusumity.Utility;
using Sapientia;
using Sapientia.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace Game.Logic.Gizmo
{
	public class GizmoDrawer : MonoBehaviour
	{
		private const int _framesToDetectDisabling = 1;

		private int _lastDrawnFrame;

		private readonly SimpleList<(float2 positionA, float2 positionB, Color color, int frames)> DRAW_LINES = new();
		private readonly SimpleList<(float2 basePos, float2[] points, Rotation rotation, Color color, int frames)> DRAW_POLYGONS = new();
		private readonly SimpleList<(float2 position, float radius, Color color, int frames)> DRAW_CIRCLES = new();
		private readonly SimpleList<(float2 position, float3 size, Color color, bool wire, int frames)> DRAW_SPHERES = new();
		private readonly SimpleList<(float2 position, Rotation rotation, float radius, float rad, Color color, int frames)> DRAW_SECTORS = new();
		private readonly SimpleList<(float2 position, Rotation rotation, float minRadius, float maxRadius, float rad, Color color, int frames)> DRAW_CUT_SECTORS = new();
		private readonly SimpleList<(float2 position, Rotation rotation, float2 size, Color color, int frames)> DRAW_BOXES = new();
		private readonly SimpleList<(float2 position, Rotation rotation, float3 size, Color color, bool wire, int frames)> DRAW_CUBES = new();
		private readonly SimpleList<(float2 position, Rotation rotation, float2 size, Color color, int frames)> DRAW_ARROWS = new();

		public static GizmoDrawer Current { get; internal set; }

		public bool IsEnabled { get; private set; }
		public float Height { get; private set; }

		public GizmoDrawerScope SetAsCurrent()
		{
			Current = this;
			return new GizmoDrawerScope(Current);
		}

		private void Update()
		{
			Height = transform.position.y + 0.5f;
			IsEnabled = (Time.frameCount - _lastDrawnFrame) <= _framesToDetectDisabling;
		}

		private void OnDrawGizmos()
		{
			using var scope = SetAsCurrent();

			DrawGizmo();
			_lastDrawnFrame = Time.frameCount;
		}

		private void DrawGizmo()
		{
			foreach (var line in DRAW_LINES)
			{
				GizmoExt.DrawLine_TopDown(line.positionA, line.positionB, line.color);
			}
			foreach (var polygon in DRAW_POLYGONS)
			{
				GizmoExt.DrawPolygon_TopDown(polygon.basePos, polygon.points, polygon.rotation, polygon.color);
			}
			foreach (var circle in DRAW_CIRCLES)
			{
				GizmoExt.DrawCircle_TopDown(circle.position, circle.radius, circle.color);
			}
			foreach (var sphere in DRAW_SPHERES)
			{
				GizmoExt.DrawSphere_TopDown(sphere.position, sphere.size, sphere.color, sphere.wire);
			}
			foreach (var sector in DRAW_SECTORS)
			{
				GizmoExt.DrawSector_TopDown(sector.position, sector.rotation, sector.radius, sector.rad, sector.color);
			}
			foreach (var sector in DRAW_CUT_SECTORS)
			{
				GizmoExt.DrawCutSector_TopDown(sector.position, sector.rotation, sector.minRadius, sector.maxRadius, sector.rad, sector.color);
			}
			foreach (var box in DRAW_BOXES)
			{
				GizmoExt.DrawBox_TopDown(box.position, box.rotation, box.size, box.color);
			}
			foreach (var box in DRAW_CUBES)
			{
				GizmoExt.DrawCube_TopDown(box.position, box.rotation, box.size, box.color, box.wire);
			}
			foreach (var arrow in DRAW_ARROWS)
			{
				GizmoExt.DrawArrow_TopDown(arrow.position, arrow.rotation, arrow.size, arrow.color);
			}
		}

		public void ClearGizmo()
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

		[Conditional(E.UNITY_EDITOR)]
		public void RequestDrawLineOnce_TopDown(float2 positionA, float2 positionB, Color color)
		{
			RequestDrawLine_TopDown(positionA, positionB, color, GizmoExt.FRAMES_FOR_ONCE_DRAWING);
		}

		[Conditional(E.UNITY_EDITOR)]
		public void RequestDrawLine_TopDown(float2 positionA, float2 positionB, Color color, int frames = 1)
		{
			if (!IsEnabled)
				return;
			DRAW_LINES.Add((positionA, positionB, color, frames));
		}

		[Conditional(E.UNITY_EDITOR)]
		public void RequestDrawPolygon_TopDown(float2 basePos, Span<float2> pointsSpan, Rotation rotation, Color color, int frames = 1)
		{
			if (!IsEnabled)
				return;
			DRAW_POLYGONS.Add((basePos, pointsSpan.ToArray(), rotation, color, frames));
		}

		[Conditional(E.UNITY_EDITOR)]
		public void RequestDrawCircleOnce_TopDown(float2 position, float radius, Color color)
		{
			RequestDrawCircle_TopDown(position, radius, color, GizmoExt.FRAMES_FOR_ONCE_DRAWING);
		}

		[Conditional(E.UNITY_EDITOR)]
		public void RequestDrawCircle_TopDown(float2 position, float radius, Color color, int frames = 1)
		{
			if (!IsEnabled)
				return;
			DRAW_CIRCLES.Add((position, radius, color, frames));
		}

		[Conditional(E.UNITY_EDITOR)]
		public void RequestDrawSphere_TopDown(float2 position, float3 size, Color color, bool wire = false, int frames = 1)
		{
			if (!IsEnabled)
				return;
			DRAW_SPHERES.Add((position, size, color, wire, frames));
		}

		[Conditional(E.UNITY_EDITOR)]
		public void RequestDrawCube_TopDown(float2 position, Rotation rotation, float3 size, Color color, bool wire = false, int frames = 1)
		{
			if (!IsEnabled)
				return;
			DRAW_CUBES.Add((position, rotation, size, color, wire, frames));
		}

		[Conditional(E.UNITY_EDITOR)]
		public void RequestDrawSectorOnce_TopDown(float2 position, Rotation rotation, float radius, float rad, Color color)
		{
			RequestDrawSector_TopDown(position, rotation, radius, rad, color, GizmoExt.FRAMES_FOR_ONCE_DRAWING);
		}

		[Conditional(E.UNITY_EDITOR)]
		public void RequestDrawSector_TopDown(float2 position, Rotation rotation, float radius, float rad, Color color, int frames = 1)
		{
			if (!IsEnabled)
				return;
			DRAW_SECTORS.Add((position, rotation, radius, rad, color, frames));
		}

		[Conditional(E.UNITY_EDITOR)]
		public void RequestDrawCutSectorOnce_TopDown(float2 position, Rotation rotation, float minRadius, float maxRadius, float rad, Color color)
		{
			RequestDrawCutSector_TopDown(position, rotation, minRadius, maxRadius, rad, color, GizmoExt.FRAMES_FOR_ONCE_DRAWING);
		}

		[Conditional(E.UNITY_EDITOR)]
		public void RequestDrawCutSector_TopDown(float2 position, Rotation rotation, float minRadius, float maxRadius, float rad, Color color, int frames = 1)
		{
			if (!IsEnabled)
				return;
			DRAW_CUT_SECTORS.Add((position, rotation, minRadius, maxRadius, rad, color, frames));
		}

		[Conditional(E.UNITY_EDITOR)]
		public void RequestDrawBoxOnce_TopDown(float2 position, Rotation rotation, float2 size, Color color)
		{
			RequestDrawBox_TopDown(position, rotation, size, color, GizmoExt.FRAMES_FOR_ONCE_DRAWING);
		}

		[Conditional(E.UNITY_EDITOR)]
		public void RequestDrawBox_TopDown(float2 position, Rotation rotation, float2 size, Color color, int frames = 1)
		{
			if (!IsEnabled)
				return;
			DRAW_BOXES.Add((position, rotation, size, color, frames));
		}

		[Conditional(E.UNITY_EDITOR)]
		public void RequestDrawArrowOnce_TopDown(float2 position, Rotation rotation, float2 size, Color color)
		{
			RequestDrawArrow_TopDown(position, rotation, size, color, GizmoExt.FRAMES_FOR_ONCE_DRAWING);
		}

		[Conditional(E.UNITY_EDITOR)]
		public void RequestDrawArrow_TopDown(float2 position, Rotation rotation, float2 size, Color color, int frames = 1)
		{
			if (!IsEnabled)
				return;
			DRAW_ARROWS.Add((position, rotation, size, color, frames));
		}
	}

	public struct GizmoDrawerScope : IDisposable
	{
		private GizmoDrawer _previousDrawer;

		public GizmoDrawerScope(GizmoDrawer previousDrawer)
		{
			_previousDrawer = previousDrawer;
		}

		public void Dispose()
		{
			GizmoDrawer.Current = _previousDrawer;
			this = default;
		}
	}
}
