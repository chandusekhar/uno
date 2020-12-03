using Windows.Foundation;
using Uno.UI;

namespace Windows.UI.Xaml.Shapes
{
    public partial class Line : Shape
	{
		/// <inheritdoc />
		protected override Size MeasureOverride(Size availableSize)
			=> MeasureAbsoluteShape(availableSize, GetPath());

		/// <inheritdoc />
		protected override Size ArrangeOverride(Size finalSize)
			=> ArrangeAbsoluteShape(finalSize, GetPath());

		private Android.Graphics.Path GetPath()
		{
			var output = new Android.Graphics.Path();

			output.MoveTo(ViewHelper.LogicalToPhysicalPixels(X1), ViewHelper.LogicalToPhysicalPixels(Y1));
			output.LineTo(ViewHelper.LogicalToPhysicalPixels(X2), ViewHelper.LogicalToPhysicalPixels(Y2));

			return output;
		}
	}
}
