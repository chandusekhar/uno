using Windows.UI.Composition;
using Windows.Foundation;
using Windows.Graphics;
using Android.Graphics;
using Uno.UI;

namespace Windows.UI.Xaml.Shapes
{
	public partial class Rectangle : Shape
	{
		static Rectangle()
		{
			StretchProperty.OverrideMetadata(typeof(Rectangle), new FrameworkPropertyMetadata(defaultValue: Media.Stretch.Fill));
		}

		public Rectangle()
		{
		}
			
		/// <inheritdoc />
		protected override Size MeasureOverride(Size availableSize)
			=> base.MeasureRelativeShape(availableSize);

		/// <inheritdoc />
		protected override Size ArrangeOverride(Size finalSize)
		{
			var (shapeSize, renderingArea) = ArrangeRelativeShape(finalSize);

			Android.Graphics.Path path;

			if (renderingArea.Width > 0 && renderingArea.Height > 0)
			{
				var rx = ViewHelper.LogicalToPhysicalPixels(RadiusX);
				var ry = ViewHelper.LogicalToPhysicalPixels(RadiusY);

				path = new Android.Graphics.Path();
				path.AddRoundRect(renderingArea.ToRectF(), rx, ry, Android.Graphics.Path.Direction.Cw);

			}
			else
			{
				path = null;
			}

			Render(path);

			return shapeSize;
		}
		//protected override void OnDraw(Canvas canvas)
		//{
		//	base.OnDraw(canvas);
		//	var drawArea = GetDrawArea(canvas);
		//	var rx = ViewHelper.LogicalToPhysicalPixels(RadiusX);
		//	var ry = ViewHelper.LogicalToPhysicalPixels(RadiusY);

		//	var fillRect = new Android.Graphics.Path();
		//	fillRect.AddRoundRect(drawArea.ToRectF(), rx, ry, Android.Graphics.Path.Direction.Cw);

		//	DrawFill(canvas, drawArea, fillRect);
		//	DrawStroke(canvas, drawArea, (c, r, p) => c.DrawRoundRect(r.ToRectF(), rx, ry, p));
		//}

		//partial void OnRadiusXChangedPartial()
		//{
		//	Invalidate();
		//}

		//partial void OnRadiusYChangedPartial()
		//{
		//	Invalidate();
		//}

		//protected override void RefreshShape(bool forceRefresh = false)
		//{
		//	Invalidate();
		//}
	}
}
