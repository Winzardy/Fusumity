using System.Collections.Generic;
using UnityEngine;

namespace UI
{
	/// <summary>
	/// Group that lets you get an empty marker that acts as a
	/// snapping point (tracks world position of an object),
	/// to which you can attach any UI object.
	/// </summary>
	public class UIEmptyMarkersGroup : UIGroup<UIMarker, UIMarkerLayout, UIMarkerArgs<EmptyArgs>>
    {
        private Camera _customCamera;
        private Dictionary<Transform, WidgetGroupToken> _usedMarkers = new Dictionary<Transform, WidgetGroupToken>();

        public UIEmptyMarkersGroup(UIGroupLayout layout, Camera customCamera = null)
        {
			SetupLayout(layout);
			SetCamera(customCamera);

			SetActive(true, true);
		}

		public void SetCamera(Camera customCamera)
		{
			_customCamera = customCamera;
		}

        public UIMarker SnapMarkerTo(Transform worldObject, Vector2 offset = default)
        {
            var token = Add(new UIMarkerArgs<EmptyArgs>
            {
                target = worldObject,
				camera = _customCamera,
				offset = offset
            });

            _usedMarkers.Add(worldObject, token);
            return token.Widget;
        }

        public void ReleaseMarkerFrom(Transform worldObject)
        {
            if (!_usedMarkers.Remove(worldObject, out var token))
            {
                Debug.LogError(
                    $"Could not find marker used for " +
                    $"world object: [ {worldObject.name} ]",
                    worldObject);

                return;
            }

            token.Release();
        }

		public void ReleaseAll()
		{
			foreach (var tokens in _usedMarkers.Values)
			{
				tokens.Release();
			}

			_usedMarkers.Clear();
		}
    }
}
