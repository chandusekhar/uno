using Android.Graphics;
using Windows.UI.Xaml.Controls;
using System;
using Uno.Logging;
using Uno.Extensions;
using System.Drawing;
using Uno.UI;
using Windows.UI.Xaml.Media;
using System.Linq;
using Uno.Disposables;
using System.Collections.Generic;
using Android.Graphics.Drawables;
using Android.Graphics.Drawables.Shapes;
using Android.Views;

namespace Windows.UI.Xaml.Shapes
{
	public partial class Shape
	{
		private SerialDisposable _layer = new SerialDisposable();

		private void Render(Android.Graphics.Path path, double? width = null, double? height = null, double scaleX = 1d, double scaleY = 1d)
		{
			//if (height == 0 || width == 0)
			//{
			//	return Disposable.Empty;
			//}

			var fill = Fill;
			var stroke = Stroke;

			if (fill == null && stroke == null)
			{
				// Nothing to draw!
				_layer.Disposable = null;
			}

			// predict list capacity to reduce memory handling
			var drawablesCapacity = fill != null && stroke != null ? 2 : 1;
			var drawables = new List<Drawable>(drawablesCapacity);

			if (path == null)
			{
				_layer.Disposable = null;
			}

			// Scale the path using its Stretch
			var matrix = new Android.Graphics.Matrix();
			var stretchMode = Stretch;

			switch (stretchMode)
			{
				case Stretch.Fill:
				case Stretch.None:
					matrix.SetScale((float)scaleX, (float)scaleY);
					break;
				case Stretch.Uniform:
					var scale = Math.Min(scaleX, scaleY);
					matrix.SetScale((float)scale, (float)scale);
					break;
				case Stretch.UniformToFill:
					scale = Math.Max(scaleX, scaleY);
					matrix.SetScale((float)scale, (float)scale);
					break;
			}
			path.Transform(matrix);

			// Move the path using its alignements
			var translation = new Android.Graphics.Matrix();

			var pathBounds = new RectF();

			// Compute the bounds. This is needed for stretched shapes and stroke thickness translation calculations.
			path.ComputeBounds(pathBounds, true);

			if (stretchMode == Stretch.None)
			{
				// Since we are not stretching, ensure we are using (0, 0) as origin.
				pathBounds.Left = 0;
				pathBounds.Top = 0;
			}

			if (!ShouldPreserveOrigin)
			{
				//We need to translate the shape to take in account the stroke thickness
				translation.SetTranslate((float)(-pathBounds.Left + PhysicalStrokeThickness * 0.5f), (float)(-pathBounds.Top + PhysicalStrokeThickness * 0.5f));
			}

			path.Transform(translation);


			// Draw the fill
			var drawArea = new Windows.Foundation.Rect(0, 0, width ?? 0, height ?? 0);

			if (fill is ImageBrush fillImageBrush)
			{
				var bitmapDrawable = new BitmapDrawable(Context.Resources, fillImageBrush.TryGetBitmap(drawArea, () => RefreshShape(forceRefresh: true), path));
				drawables.Add(bitmapDrawable);
			}
			else if (fill != null)
			{
				var fillPaint = fill.GetFillPaint(drawArea);

				var lineDrawable = new PaintDrawable
				{
					Shape = new PathShape(path, (float)width, (float)height)
				};
				lineDrawable.Paint.Color = fillPaint.Color;
				lineDrawable.Paint.SetShader(fillPaint.Shader);
				lineDrawable.Paint.SetStyle(Paint.Style.Fill);
				lineDrawable.Paint.Alpha = fillPaint.Alpha;

				drawables.Add(lineDrawable);
			}

			// Draw the contour
			if (stroke != null)
			{
				using (var strokeBrush = new Paint(stroke.GetStrokePaint(drawArea)))
				{
					var lineDrawable = new PaintDrawable
					{
						Shape = new PathShape(path, (float)width, (float)height)
					};
					lineDrawable.Paint.Color = strokeBrush.Color;
					lineDrawable.Paint.SetShader(strokeBrush.Shader);
					lineDrawable.Paint.StrokeWidth = (float)PhysicalStrokeThickness;
					lineDrawable.Paint.SetStyle(Paint.Style.Stroke);
					lineDrawable.Paint.Alpha = strokeBrush.Alpha;

					SetStrokeDashEffect(lineDrawable.Paint);

					drawables.Add(lineDrawable);
				}
			}

			var layerDrawable = new LayerDrawable(drawables.ToArray());

			// Set bounds must always be called, otherwise the android layout engine can't determine
			// the rendering size. See Drawable documentation for details.
			layerDrawable.SetBounds(0, 0, (int)width, (int)height);

			_layer.Disposable = SetOverlay(this as View, layerDrawable);
		}

		private IDisposable SetOverlay(View view, Drawable drawable)
		{
#if __ANDROID_18__
			if (Android.OS.Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.Kitkat)
			{
				using (PreventRequestLayout())
				{
					Overlay.Add(drawable);
				}

				return Disposable.Create(
					() =>
					{
						using (PreventRequestLayout())
						{
							Overlay.Remove(drawable);
						}
					}
				);
			}
			else
#endif
			{
#pragma warning disable 0618 // Used for compatibility with SetBackgroundDrawable and previous API Levels

				// Set overlay is not supported by this platform, set use the background instead.
				// It'll break some scenarios, like having borders on top of the content.
				view.SetBackgroundDrawable(drawable);
				return Disposable.Create(() => view.SetBackgroundDrawable(null));

#pragma warning restore 0618
			}
		}

		protected bool HasStroke
		{
			get { return Stroke != null && ActualStrokeThickness > 0; }
		}

		internal double PhysicalStrokeThickness
		{
			get { return ViewHelper.LogicalToPhysicalPixels((double)ActualStrokeThickness); }
		}
		private Windows.Foundation.Rect GetPathBoundingBox(Android.Graphics.Path path)
		{
			var pathBounds = new RectF();
			path.ComputeBounds(pathBounds, true);
			return pathBounds;
		}

		protected Windows.Foundation.Rect GetDrawArea(Android.Graphics.Canvas canvas)
		{
			var drawSize = default(Windows.Foundation.Size);

			var suggestedWidth = canvas.Width;
			var suggestedHeight = canvas.Height;

			switch (Stretch)
			{
				case Stretch.Fill:
					drawSize.Width = suggestedWidth;
					drawSize.Height = suggestedHeight;
					break;
				case Stretch.None:
					drawSize.Width = double.IsNaN(Width) || Width == 0 ? 0 : suggestedWidth;
					drawSize.Height = double.IsNaN(Height) || Height == 0 ? 0 : suggestedHeight;
					break;
				case Stretch.Uniform:
					drawSize.Width = Math.Min(suggestedWidth, suggestedHeight);
					drawSize.Height = Math.Min(suggestedWidth, suggestedHeight);
					break;
				case Stretch.UniformToFill:
					drawSize.Width = Math.Max(suggestedWidth, suggestedHeight);
					drawSize.Height = Math.Max(suggestedWidth, suggestedHeight);
					break;
			}

			var drawArea = HasStroke
				? GetAdjustedRectangle(drawSize, PhysicalStrokeThickness)
				: new Windows.Foundation.Rect(Windows.Foundation.Point.Zero, drawSize);

			return drawArea;
		}

		protected void DrawFill(Android.Graphics.Canvas canvas, Windows.Foundation.Rect fillArea, Android.Graphics.Path fillPath)
		{
			if (!fillArea.HasZeroArea())
			{
				var imageBrushFill = Fill as ImageBrush;
				if (imageBrushFill != null)
				{
					imageBrushFill.ScheduleRefreshIfNeeded(fillArea, Invalidate);
					imageBrushFill.DrawBackground(canvas, fillArea, fillPath);
				}
				else
				{
					var fill = Fill ?? SolidColorBrushHelper.Transparent;
					var fillPaint = fill.GetFillPaint(fillArea);
					canvas.DrawPath(fillPath, fillPaint);
				}
			}
		}

		protected void DrawStroke(Android.Graphics.Canvas canvas, Windows.Foundation.Rect strokeArea, Action<Android.Graphics.Canvas, Windows.Foundation.Rect, Paint> drawingMethod)
		{
			if (HasStroke)
			{
				var strokeThickness = PhysicalStrokeThickness;

				using (var strokePaint = new Paint(Stroke.GetStrokePaint(strokeArea)))
				{
					SetStrokeDashEffect(strokePaint);

					if (strokeArea.HasZeroArea())
					{
						//Draw the stroke as a fill because the shape has no area
						strokePaint.SetStyle(Paint.Style.Fill);
						canvas.DrawCircle((float)(strokeThickness / 2), (float)(strokeThickness / 2), (float)(strokeThickness / 2), strokePaint);
					}
					else
					{
						strokePaint.StrokeWidth = (float)strokeThickness;
						drawingMethod(canvas, strokeArea, strokePaint);
					}
				}
			}
		}

		protected void SetStrokeDashEffect(Paint strokePaint)
		{
			var strokeDashArray = StrokeDashArray;

			if (strokeDashArray != null && strokeDashArray.Count > 0)
			{
				// If only value specified in the dash array, copy and add it
				if (strokeDashArray.Count == 1)
				{
					strokeDashArray.Add(strokeDashArray[0]);
				}

				// Make sure the dash array has a positive number of items, Android cannot have an odd number
				// of items in the array (in such a case we skip the dash effect and log the error)
				//		https://developer.android.com/reference/android/graphics/DashPathEffect.html
				//		**  The intervals array must contain an even number of entries (>=2), with
				//			the even indices specifying the "on" intervals, and the odd indices
				//			specifying the "off" intervals.  **
				if (strokeDashArray.Count % 2 == 0)
				{
					var pattern = strokeDashArray.Select(d => (float)d).ToArray();
					strokePaint.SetPathEffect(new DashPathEffect(pattern, 0));
				}
				else
				{
					this.Log().ErrorIfEnabled(() => "StrokeDashArray containing an odd number of values is not supported on Android.");
				}
			}
		}

		/// <summary>
		/// Convert the drawSize to a drawArea adjusted to prevent the shape's stroke from exceeding its bounds (half the strokeThickness is drawn outside the drawArea)
		/// </summary>
		/// <param name="drawSize"></param>
		/// <param name="strokeThickness"></param>
		/// <returns></returns>
		private static Windows.Foundation.Rect GetAdjustedRectangle(Windows.Foundation.Size drawSize, double strokeThickness)
		{
			if (drawSize == default(Windows.Foundation.Size) || double.IsNaN(drawSize.Width) || double.IsNaN(drawSize.Height))
			{
				return Windows.Foundation.Rect.Empty;
			}

			return new Windows.Foundation.Rect
			(
				x: (float)(strokeThickness / 2), 
				y: (float)(strokeThickness / 2),
				width: (float)(drawSize.Width - strokeThickness), 
				height: (float)(drawSize.Height - strokeThickness)
			);
		}

		partial void OnFillUpdatedPartial()
		{
			this.Invalidate();
		}

		partial void OnStrokeUpdatedPartial()
		{
			this.Invalidate();
		}
		partial void OnStretchUpdatedPartial()
		{
			this.Invalidate();
		}
		partial void OnStrokeThicknessUpdatedPartial()
		{
			this.Invalidate();
		}
	}
}
