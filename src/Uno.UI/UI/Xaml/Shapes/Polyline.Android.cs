using Uno.Media;
using Windows.Foundation;

namespace Windows.UI.Xaml.Shapes
{
	public partial class Polyline : Shape
	{
		/// <inheritdoc />
		protected override Size MeasureOverride(Size availableSize)
			=> MeasureAbsoluteShape(availableSize, GetPath());

		/// <inheritdoc />
		protected override Size ArrangeOverride(Size finalSize)
			=> ArrangeAbsoluteShape(finalSize, GetPath());

		private Android.Graphics.Path GetPath()
		{
			var coords = Points;

			if (coords == null || coords.Count <= 1)
			{
				return null;
			}

			var streamGeometry = GeometryHelper.Build(c =>
			{
				c.BeginFigure(new Point(coords[0].X, coords[0].Y), true, false);
				for (var i = 1; i < coords.Count; i++)
				{
					c.LineTo(new Point(coords[i].X, coords[i].Y), true, false);
				}
			});

			return streamGeometry.ToPath();

		}
	}
}
