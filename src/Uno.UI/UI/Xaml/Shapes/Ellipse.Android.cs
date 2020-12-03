using Windows.Foundation;
using Android.Graphics;
using Uno.UI;

namespace Windows.UI.Xaml.Shapes
{
	public partial class Ellipse : Shape
	{
		static Ellipse()
		{
			StretchProperty.OverrideMetadata(typeof(Ellipse), new FrameworkPropertyMetadata(defaultValue: Media.Stretch.Fill));
		}

		/// <inheritdoc />
		protected override Size MeasureOverride(Size availableSize)
			=> base.MeasureRelativeShape(availableSize);

		/// <inheritdoc />
		protected override Size ArrangeOverride(Size finalSize)
		{
			var (shapeSize, renderingArea) = ArrangeRelativeShape(finalSize);

			Render(renderingArea.Width > 0 && renderingArea.Height > 0
				? GetPath(renderingArea.Size)
				: null);

			return shapeSize;
		}

		private Android.Graphics.Path GetPath(Size availableSize)
		{
			var bounds = availableSize.LogicalToPhysicalPixels();

			var output = new Android.Graphics.Path();
			output.AddOval(
				new RectF(0, 0, (float)bounds.Width, (float)bounds.Height),
				Android.Graphics.Path.Direction.Cw);

			return output;
		}
	}
}
