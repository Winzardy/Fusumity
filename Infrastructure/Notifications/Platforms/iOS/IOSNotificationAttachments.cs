using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using AssetManagement;
using Content;
using Sapientia.Extensions;
using Sapientia.Pooling;
using Unity.Notifications.iOS;
using UnityEngine;

namespace Notifications.iOS
{
	using UnityObject = UnityEngine.Object;

	internal static class IOSNotificationAttachments
	{
		private const string ROOT_DIRECTORY = "Notifications/Attachments";
		private const string STAGING_DIRECTORY = "Staging";

		public static void Initialize()
		{
			try
			{
				Cleanup();
			}
			catch (Exception exception)
			{
				NotificationsDebug.LogException(exception);
			}
		}

		public static async UniTask<string> ApplyAsync(
			iOSNotification notification,
			AssetReference<Sprite> attachment,
			CancellationToken ct)
		{
			if (attachment.IsEmptyOrInvalid())
				return null;

			var path = await GetOrCreatePathAsync(attachment, ct);
			if (path.IsNullOrEmpty())
				return null;

			var stagingPath = CreateStagingCopy(path);
			if (stagingPath.IsNullOrEmpty())
				return null;

			notification.Attachments = new List<iOSNotificationAttachment>
			{
				new() {Url = new Uri(stagingPath).AbsoluteUri}
			};

			return stagingPath;
		}

		public static void DeleteStaging(string path)
		{
			if (path.IsNullOrEmpty())
				return;

			try
			{
				if (File.Exists(path))
					File.Delete(path);
			}
			catch (Exception exception)
			{
				NotificationsDebug.LogException(exception);
			}
		}

		private static void Cleanup()
		{
			var rootPath = GetRootPath();
			if (!Directory.Exists(rootPath))
				return;

			var currentBuildPath = GetBuildPath();
			foreach (var directory in Directory.GetDirectories(rootPath))
			{
				if (directory.Equals(currentBuildPath, StringComparison.OrdinalIgnoreCase))
					continue;

				Directory.Delete(directory, true);
			}
		}

		public static async UniTask PrepareConfiguredAsync(CancellationToken ct)
		{
			using (HashSetPool<string>.Get(out var preparedKeys))
			{
				foreach (var reference in ContentManager.GetAll<NotificationConfig>())
				{
					var config = reference.Read();
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

		private static async UniTask<string> GetOrCreatePathAsync(
			AssetReference<Sprite> attachment,
			CancellationToken ct)
		{
			var path = GetPath(attachment);
			if (File.Exists(path))
				return path;

			try
			{
				await PrepareAsync(attachment, ct);
				return path;
			}
			catch (OperationCanceledException)
			{
				throw;
			}
			catch (Exception exception)
			{
				NotificationsDebug.LogException(exception);
				return null;
			}
		}

		private static string CreateStagingCopy(string sourcePath)
		{
			var stagingPath = default(string);
			try
			{
				var stagingDirectory = Path.Combine(GetRootPath(), STAGING_DIRECTORY);
				Directory.CreateDirectory(stagingDirectory);
				stagingPath = Path.Combine(stagingDirectory, $"{Guid.NewGuid():N}.png");
				File.Copy(sourcePath, stagingPath);
				SetNoBackupFlag(stagingPath);
				return stagingPath;
			}
			catch (Exception exception)
			{
				if (!stagingPath.IsNullOrEmpty() && File.Exists(stagingPath))
					File.Delete(stagingPath);

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

				SetNoBackupFlag(path);
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
			var fileName = $"{Hash128.Compute(GetKey(attachment))}.png";
			return Path.Combine(GetBuildPath(), fileName);
		}

		private static string GetBuildPath()
		{
			var buildKey = Application.buildGUID.IsNullOrEmpty() ? Application.version : Application.buildGUID;
			return Path.Combine(GetRootPath(), Hash128.Compute(buildKey).ToString());
		}

		private static string GetRootPath() => Path.Combine(Application.persistentDataPath, ROOT_DIRECTORY);

		private static void SetNoBackupFlag(string path)
		{
#if UNITY_IOS || UNITY_EDITOR
			UnityEngine.iOS.Device.SetNoBackupFlag(path);
#endif
		}

		private static string GetKey(AssetReference<Sprite> attachment) =>
			attachment.AssetGuid;
	}
}
