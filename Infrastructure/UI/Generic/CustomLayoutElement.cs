using UnityEngine;
using UnityEngine.UI;

namespace UI
{
	/// <summary>
	/// Добавляет настройку максимальных размеров и немного модифицирует минимальное
	/// </summary>
	[RequireComponent(typeof(RectTransform))]
	//TODO: возможно понадобится поддержка rect для prefered
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
			get => _maxWidthRect ? _maxWidthRect.rect.width : _maxWidth;
			set
			{
				if (CustomSetPropertyUtility.SetStruct(ref _maxWidth, value))
					SetDirty();
			}
		}

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
			get => _maxWidthRect ? _maxWidthRect.rect.height : _maxHeight;
			set
			{
				if (CustomSetPropertyUtility.SetStruct(ref _maxHeight, value))
					SetDirty();
			}
		}

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
			set => base.minWidth = value;
		}

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
			set => base.minHeight = value;
		}

		#endregion

		private bool _ignoreOnGettingPreferredSize;

		public override int layoutPriority
		{
			get => _ignoreOnGettingPreferredSize ? -1 : base.layoutPriority;
			set => base.layoutPriority = value;
		}

		public override float preferredHeight
		{
			get
			{
				if (_useMaxHeight)
				{
					var defaultIgnoreValue = _ignoreOnGettingPreferredSize;
					_ignoreOnGettingPreferredSize = true;

					var baseValue = LayoutUtility.GetPreferredHeight(transform as RectTransform);

					_ignoreOnGettingPreferredSize = defaultIgnoreValue;

					return baseValue > maxHeight ? maxHeight : baseValue;
				}
				else
				{
					return base.preferredHeight;
				}
			}
			set => base.preferredHeight = value;
		}

		public override float preferredWidth
		{
			get
			{
				if (_useMaxWidth)
				{
					var defaultIgnoreValue = _ignoreOnGettingPreferredSize;
					_ignoreOnGettingPreferredSize = true;

					var baseValue = LayoutUtility.GetPreferredWidth(transform as RectTransform);

					_ignoreOnGettingPreferredSize = defaultIgnoreValue;

					return baseValue > maxWidth ? maxWidth : baseValue;
				}
				else
				{
					return base.preferredWidth;
				}
			}
			set => base.preferredWidth = value;
		}
	}
}
