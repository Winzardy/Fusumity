using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using AssetManagement;
using Content;
using Sapientia.Extensions;
using Unity.Notifications.iOS;
using UnityEngine;

namespace Notifications.iOS
{
	using UnityObject = UnityEngine.Object;

	internal static class IOSNotificationAttachmentCache
	{
		public static void Apply(iOSNotification notification, AssetReference<Sprite> attachment)
		{
			if (attachment.IsEmptyOrInvalid())
				return;

			var path = GetOrCreatePath(attachment);
			if (path.IsNullOrEmpty())
				return;

			notification.Attachments = new List<iOSNotificationAttachment>
			{
				new() { Url = new Uri(path).AbsoluteUri }
			};
		}

		public static async UniTask PrepareConfiguredAsync(CancellationToken ct)
		{
			var preparedKeys = new HashSet<string>();

			foreach (var (_, contentEntry) in ContentManager.GetAllEntries<NotificationConfig>())
			{
				NotificationConfig config = contentEntry;
				if (!config.TryGet<IOSPlatformNotificationConfig>(out var platformConfig) ||
					platformConfig.attachment.IsEmptyOrInvalid())
					continue;

				var key = GetKey(platformConfig.attachment);
				if (!preparedKeys.Add(key))
					continue;

				try
				{
					await PrepareAsync(platformConfig.attachment, ct);
				}
				catch (OperationCanceledException)
				{
					throw;
				}
				catch (Exception exception)
				{
					NotificationsDebug.LogException(exception);
				}
			}
		}

		private static async UniTask PrepareAsync(AssetReference<Sprite> attachment, CancellationToken ct)
		{
			var path = GetPath(attachment);
			if (File.Exists(path))
				return;

			var sprite = await attachment.LoadAsync(ct);
			try
			{
				Write(sprite, path);
			}
			finally
			{
				attachment.Release();
			}
		}

		private static string GetOrCreatePath(AssetReference<Sprite> attachment)
		{
			var path = GetPath(attachment);
			if (File.Exists(path))
				return path;

			try
			{
				var sprite = attachment.Load();
				try
				{
					Write(sprite, path);
					return path;
				}
				finally
				{
					attachment.Release();
				}
			}
			catch (Exception exception)
			{
				NotificationsDebug.LogException(exception);
				return null;
			}
		}

		private static void Write(Sprite sprite, string path)
		{
			if (sprite == null)
				throw NotificationsDebug.Exception("Could not create iOS notification attachment from null sprite");

			var bytes = EncodeToPng(sprite);
			var directory = Path.GetDirectoryName(path)!;
			var temporaryPath = $"{path}.tmp";
			Directory.CreateDirectory(directory);
			try
			{
				File.WriteAllBytes(temporaryPath, bytes);
				if (!File.Exists(path))
					File.Move(temporaryPath, path);

#if UNITY_IOS || UNITY_EDITOR
				UnityEngine.iOS.Device.SetNoBackupFlag(path);
#endif
			}
			finally
			{
				if (File.Exists(temporaryPath))
					File.Delete(temporaryPath);
			}
		}

		private static byte[] EncodeToPng(Sprite sprite)
		{
			var rect = sprite.rect;
			var width = Mathf.Max(1, Mathf.RoundToInt(rect.width));
			var height = Mathf.Max(1, Mathf.RoundToInt(rect.height));
			var shader = Shader.Find("Sprites/Default");
			if (shader == null)
				throw NotificationsDebug.Exception("Could not find shader [ Sprites/Default ] for iOS notification attachment");

			var renderTexture = RenderTexture.GetTemporary(
				width,
				height,
				0,
				RenderTextureFormat.ARGB32,
				RenderTextureReadWrite.sRGB);
			var activeRenderTexture = RenderTexture.active;
			Mesh mesh = null;
			Material material = null;
			Texture2D readableTexture = null;

			try
			{
				mesh = CreateMesh(sprite);
				material = new Material(shader)
				{
					mainTexture = sprite.texture,
					color = Color.white
				};

				RenderTexture.active = renderTexture;
				GL.PushMatrix();
				try
				{
					GL.LoadPixelMatrix(0, width, 0, height);
					GL.Clear(true, true, Color.clear);
					material.SetPass(0);
					Graphics.DrawMeshNow(mesh, Matrix4x4.identity);
				}
				finally
				{
					GL.PopMatrix();
				}

				readableTexture = new Texture2D(width, height, TextureFormat.RGBA32, false);
				readableTexture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
				readableTexture.Apply(false);
				UnpremultiplyAlpha(readableTexture);
				return readableTexture.EncodeToPNG();
			}
			finally
			{
				if (readableTexture != null)
					UnityObject.Destroy(readableTexture);

				if (material != null)
					UnityObject.Destroy(material);

				if (mesh != null)
					UnityObject.Destroy(mesh);

				RenderTexture.active = activeRenderTexture;
				RenderTexture.ReleaseTemporary(renderTexture);
			}
		}

		private static void UnpremultiplyAlpha(Texture2D texture)
		{
			var pixels = texture.GetPixels32();
			for (var i = 0; i < pixels.Length; i++)
			{
				ref var pixel = ref pixels[i];
				if (pixel.a is 0 or byte.MaxValue)
					continue;

				pixel.r = (byte) Mathf.Min(byte.MaxValue, pixel.r * byte.MaxValue / pixel.a);
				pixel.g = (byte) Mathf.Min(byte.MaxValue, pixel.g * byte.MaxValue / pixel.a);
				pixel.b = (byte) Mathf.Min(byte.MaxValue, pixel.b * byte.MaxValue / pixel.a);
			}

			texture.SetPixels32(pixels);
			texture.Apply(false);
		}

		private static Mesh CreateMesh(Sprite sprite)
		{
			var sourceVertices = sprite.vertices;
			var vertices = new Vector3[sourceVertices.Length];
			for (var i = 0; i < sourceVertices.Length; i++)
			{
				var source = sourceVertices[i];
				vertices[i] = new Vector3(
					source.x * sprite.pixelsPerUnit + sprite.pivot.x,
					source.y * sprite.pixelsPerUnit + sprite.pivot.y);
			}

			var sourceTriangles = sprite.triangles;
			var triangles = new int[sourceTriangles.Length];
			for (var i = 0; i < sourceTriangles.Length; i++)
				triangles[i] = sourceTriangles[i];

			return new Mesh
			{
				vertices = vertices,
				uv = sprite.uv,
				triangles = triangles
			};
		}

		private static string GetPath(AssetReference<Sprite> attachment)
		{
			var buildKey = Application.buildGUID.IsNullOrEmpty() ? Application.version : Application.buildGUID;
			var buildDirectory = Hash128.Compute(buildKey).ToString();
			var fileName = $"{Hash128.Compute(GetKey(attachment))}.png";
			return Path.Combine(Application.persistentDataPath, "Notifications", "Attachments", buildDirectory, fileName);
		}

		private static string GetKey(AssetReference<Sprite> attachment) =>
			attachment.AssetGuid;
	}
}
