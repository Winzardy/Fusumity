using System;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
	/// <summary>
	/// Добавляет настройку максимальных размеров и немного модифицирует минимальное
	/// </summary>
	[RequireComponent(typeof(RectTransform))]
	public class CustomLayoutElement : LayoutElement
	{
		#region Max Width

		[SerializeField]
		private RectTransform _maxWidthRect;

		[SerializeField]
		private bool _useMaxWidth;

		[SerializeField]
		private float _maxWidth;

		public float maxWidth
		{
			get
			{
				if (!_useMaxWidth)
					return -1;

				return _maxWidthRect ? _maxWidthRect.rect.width : _maxWidth;
			}
			set
			{
				_useMaxWidth = value >= 0;
				if (CustomSetPropertyUtility.SetStruct(ref _maxWidth, value))
					SetDirty();
			}
		}

		public RectTransform MaxWidthRect { get => _maxWidthRect; set => _maxWidthRect = value; }

		#endregion

		#region Min Width

		[SerializeField]
		private RectTransform _minWidthRect;

		[SerializeField]
		private bool _useMinWidth;

		public override float minWidth
		{
			get
			{
				if (!_useMinWidth)
					return -1;

				return _minWidthRect ? _minWidthRect.rect.width : base.minWidth;
			}
			set
			{
				_useMinWidth  = value >= 0;
				base.minWidth = value;
			}
		}

		public RectTransform MinWidthRect { get => _minWidthRect; set => _minWidthRect = value; }

		#endregion

		#region Max Height

		[SerializeField]
		private bool _useMaxHeight;

		[SerializeField]
		private RectTransform _maxHeightRect;

		[SerializeField]
		private float _maxHeight;

		public float maxHeight
		{
			get
			{
				if (!_useMaxHeight)
					return -1;

				return _maxHeightRect ? _maxHeightRect.rect.height : _maxHeight;
			}
			set
			{
				_useMaxHeight = value >= 0;
				if (CustomSetPropertyUtility.SetStruct(ref _maxHeight, value))
					SetDirty();
			}
		}

		public RectTransform MaxHeightRect { get => _maxHeightRect; set => _maxHeightRect = value; }

		#endregion

		#region Min Height

		[SerializeField]
		private RectTransform _minHeightRect;

		[SerializeField]
		private bool _useMinHeight;

		public override float minHeight
		{
			get
			{
				if (!_useMinHeight)
					return -1;

				return _minHeightRect ? _minHeightRect.rect.height : base.minHeight;
			}
			set
			{
				_useMinHeight  = value >= 0;
				base.minHeight = value;
			}
		}

		public RectTransform MinHeightRect { get => _minHeightRect; set => _minHeightRect = value; }

		#endregion

		private bool _ignoreOnGettingPreferredSize;

		public override int layoutPriority
		{
			get => _ignoreOnGettingPreferredSize ? -1 : base.layoutPriority;
			set => base.layoutPriority = value;
		}

		public override float preferredWidth
		{
			get => _useMaxWidth ? GetMaxWidth() : base.preferredWidth;
			set => base.preferredWidth = _useMaxWidth ? GetMaxWidth() : value;
		}

		public override float preferredHeight
		{
			get => _useMaxHeight ? GetMaxHeight() : base.preferredHeight;
			set => base.preferredHeight = _useMaxHeight ? GetMaxHeight() : value;
		}

		private float GetMaxWidth()
		{
			var defaultIgnoreValue = _ignoreOnGettingPreferredSize;
			try
			{
				if (transform.TryGetComponent(out Graphic graphic) && !graphic.enabled)
					return base.preferredWidth;

				if (_ignoreOnGettingPreferredSize)
					return base.preferredWidth;

				_ignoreOnGettingPreferredSize = true;

				var baseValue = LayoutUtility.GetPreferredWidth(transform as RectTransform);

				return baseValue > maxWidth ? maxWidth : baseValue;
			}
			catch (Exception e)
			{
				GUIDebug.LogWarning(e.Message, transform);
				return base.preferredWidth;
			}
			finally
			{
				_ignoreOnGettingPreferredSize = defaultIgnoreValue;
			}
		}

		private float GetMaxHeight()
		{
			var defaultIgnoreValue = _ignoreOnGettingPreferredSize;
			try
			{
				if (transform.TryGetComponent(out Graphic graphic) && !graphic.enabled)
					return base.preferredHeight;

				if (_ignoreOnGettingPreferredSize)
					return base.preferredHeight;

				_ignoreOnGettingPreferredSize = true;

				var baseValue = LayoutUtility.GetPreferredHeight(transform as RectTransform);
				return baseValue > maxHeight ? maxHeight : baseValue;
			}
			catch (Exception e)
			{
				GUIDebug.LogWarning(e.Message, transform);
				return base.preferredHeight;
			}
			finally
			{
				_ignoreOnGettingPreferredSize = defaultIgnoreValue;
			}
		}
	}
}
