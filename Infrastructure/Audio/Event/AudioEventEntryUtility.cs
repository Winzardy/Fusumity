using System.Collections.Generic;
using Sapientia;
using Sapientia.Collections;
using Sapientia.Pooling;

namespace Audio
{
	public static class AudioEventEntryUtility
	{
		/// <summary>
		/// Нужно для кеширования последнего воспроизведенного трека для режиме SelectionMode.ByOrder
		/// </summary>
		private static Dictionary<int, int> eventToPosition = new(8);

		public static AudioTrackScheme[] RollPlaylist(this AudioEventConfig config, int? hash = null)
		{
			if (config.tracks.Length > 1)
			{
				using (ListPool<AudioTrackScheme>.Get(out var result))
				{
					var range = config.selectionRange;

					switch (config.selection)
					{
						case SelectionMode.Random:

							using (ListPool<AudioTrackScheme>.Get(out var list))
							{
								list.AddRange(config.tracks);

								for (int i = 0; i < range; i++)
								{
									if (list.Roll(AudioManager.GetRandomizer<int>(),out var index))
									{
										result.Add(list[index]);
										list.RemoveAt(index);
									}
								}
							}

							break;
						case SelectionMode.ByOrder:

							eventToPosition.TryGetValue(config.GetHashCode(), out var globalPosition);
							for (int i = 0; i < range; i++)
							{
								result.Add(config.tracks[globalPosition]);
								globalPosition++;
								globalPosition = globalPosition >= config.tracks.Length ? 0 : globalPosition;
								eventToPosition[config.GetHashCode()] = globalPosition;
							}

							break;
						case SelectionMode.ByLocalOrder:
							var x = hash ?? config.GetHashCode();

							eventToPosition.TryGetValue(x, out var localPosition);
							for (int i = 0; i < range; i++)
							{
								result.Add(config.tracks[localPosition]);
								localPosition++;
								localPosition = localPosition >= config.tracks.Length ? 0 : localPosition;
								eventToPosition[x] = localPosition;
							}

							break;

						default:
							result.AddRange(config.tracks);
							break;
					}

					if (config.playMode == AudioPlayMode.Sequence &&
					    config.sequenceType == SequenceType.Shuffle &&
					    result.Count > 1)
					{
						result.Shuffle();
					}

					return result.ToArray();
				}
			}

			return config.tracks;
		}
	}
}
