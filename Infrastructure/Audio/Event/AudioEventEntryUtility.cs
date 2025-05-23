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

		public static AudioTrackEntry[] RollPlaylist(this AudioEventEntry entry, int? hash = null)
		{
			if (entry.tracks.Length > 1)
			{
				using (ListPool<AudioTrackEntry>.Get(out var result))
				{
					var range = entry.selectionRange;

					switch (entry.selection)
					{
						case SelectionMode.Random:

							using (ListPool<AudioTrackEntry>.Get(out var list))
							{
								list.AddRange(entry.tracks);

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

							eventToPosition.TryGetValue(entry.GetHashCode(), out var globalPosition);
							for (int i = 0; i < range; i++)
							{
								result.Add(entry.tracks[globalPosition]);
								globalPosition++;
								globalPosition = globalPosition >= entry.tracks.Length ? 0 : globalPosition;
								eventToPosition[entry.GetHashCode()] = globalPosition;
							}

							break;
						case SelectionMode.ByLocalOrder:
							var x = hash ?? entry.GetHashCode();

							eventToPosition.TryGetValue(x, out var localPosition);
							for (int i = 0; i < range; i++)
							{
								result.Add(entry.tracks[localPosition]);
								localPosition++;
								localPosition = localPosition >= entry.tracks.Length ? 0 : localPosition;
								eventToPosition[x] = localPosition;
							}

							break;

						default:
							result.AddRange(entry.tracks);
							break;
					}

					if (entry.playMode == AudioPlayMode.Sequence &&
					    entry.sequenceType == SequenceType.Shuffle &&
					    result.Count > 1)
					{
						result.Shuffle();
					}

					return result.ToArray();
				}
			}

			return entry.tracks;
		}
	}
}
