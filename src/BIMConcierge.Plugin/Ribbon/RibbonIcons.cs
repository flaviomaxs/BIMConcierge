using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace BIMConcierge.Plugin.Ribbon;

/// <summary>
/// Generates BitmapSource icons for Revit ribbon buttons at runtime.
/// Uses DrawingVisual for vector rendering — no external image files needed.
/// </summary>
internal static class RibbonIcons
{
    private static readonly Color Primary = (Color)ColorConverter.ConvertFromString("#6A7D90");
    private static readonly Color PrimaryLight = (Color)ColorConverter.ConvertFromString("#8FA3B4");

    /// <summary>Dashboard icon: 2x2 grid layout.</summary>
    public static BitmapSource Dashboard(int size)
    {
        return Render(size, (dc, s) =>
        {
            var brush = new SolidColorBrush(Primary);
            var pen = new Pen(brush, 0);
            double m = s * 0.18;   // margin
            double g = s * 0.08;   // gap
            double cellW = (s - 2 * m - g) / 2;
            double cellH = (s - 2 * m - g) / 2;
            double r = s * 0.08;   // corner radius

            dc.DrawRoundedRectangle(brush, pen, new Rect(m, m, cellW, cellH), r, r);
            dc.DrawRoundedRectangle(brush, pen, new Rect(m + cellW + g, m, cellW, cellH), r, r);
            dc.DrawRoundedRectangle(brush, pen, new Rect(m, m + cellH + g, cellW, cellH), r, r);
            dc.DrawRoundedRectangle(brush, pen, new Rect(m + cellW + g, m + cellH + g, cellW, cellH), r, r);
        });
    }

    /// <summary>Tutorial icon: play triangle.</summary>
    public static BitmapSource Tutorial(int size)
    {
        return Render(size, (dc, s) =>
        {
            var brush = new SolidColorBrush(Primary);
            double m = s * 0.2;

            // Circle background
            var circleBrush = new SolidColorBrush(Primary);
            circleBrush.Opacity = 0.15;
            dc.DrawEllipse(circleBrush, null, new Point(s / 2.0, s / 2.0), s / 2.0 - m * 0.5, s / 2.0 - m * 0.5);

            // Play triangle (shifted right slightly for optical center)
            double offset = s * 0.05;
            var tri = new StreamGeometry();
            using (var ctx = tri.Open())
            {
                ctx.BeginFigure(new Point(m + offset + s * 0.08, m), true, true);
                ctx.LineTo(new Point(s - m + offset, s / 2.0), true, false);
                ctx.LineTo(new Point(m + offset + s * 0.08, s - m), true, false);
            }
            tri.Freeze();
            dc.DrawGeometry(brush, null, tri);
        });
    }

    /// <summary>Standards icon: shield with checkmark.</summary>
    public static BitmapSource Standards(int size)
    {
        return Render(size, (dc, s) =>
        {
            var brush = new SolidColorBrush(Primary);
            double m = s * 0.15;
            double cx = s / 2.0;

            // Shield shape
            var shield = new StreamGeometry();
            using (var ctx = shield.Open())
            {
                ctx.BeginFigure(new Point(cx, m), true, true);
                ctx.LineTo(new Point(s - m, m + s * 0.15), true, false);
                ctx.LineTo(new Point(s - m, s * 0.55), true, false);
                ctx.BezierTo(
                    new Point(s - m, s * 0.7),
                    new Point(cx + s * 0.1, s * 0.8),
                    new Point(cx, s - m),
                    true, false);
                ctx.BezierTo(
                    new Point(cx - s * 0.1, s * 0.8),
                    new Point(m, s * 0.7),
                    new Point(m, s * 0.55),
                    true, false);
                ctx.LineTo(new Point(m, m + s * 0.15), true, false);
            }
            shield.Freeze();
            dc.DrawGeometry(brush, null, shield);

            // Checkmark inside shield
            var checkPen = new Pen(Brushes.White, s * 0.08) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round };
            dc.DrawLine(checkPen, new Point(cx - s * 0.13, s * 0.48), new Point(cx - s * 0.02, s * 0.58));
            dc.DrawLine(checkPen, new Point(cx - s * 0.02, s * 0.58), new Point(cx + s * 0.15, s * 0.38));
        });
    }

    /// <summary>Progress icon: ascending bar chart.</summary>
    public static BitmapSource Progress(int size)
    {
        return Render(size, (dc, s) =>
        {
            var brush = new SolidColorBrush(Primary);
            var pen = new Pen(brush, 0);
            double m = s * 0.18;
            double barW = (s - 2 * m - s * 0.12) / 3; // 3 bars with gaps
            double gap = s * 0.06;
            double bottom = s - m;
            double r = s * 0.06;

            // Bar 1 (shortest)
            double h1 = (s - 2 * m) * 0.4;
            dc.DrawRoundedRectangle(brush, pen, new Rect(m, bottom - h1, barW, h1), r, r);

            // Bar 2 (medium)
            double h2 = (s - 2 * m) * 0.65;
            dc.DrawRoundedRectangle(brush, pen, new Rect(m + barW + gap, bottom - h2, barW, h2), r, r);

            // Bar 3 (tallest)
            double h3 = (s - 2 * m) * 0.95;
            dc.DrawRoundedRectangle(brush, pen, new Rect(m + 2 * (barW + gap), bottom - h3, barW, h3), r, r);
        });
    }

    private static BitmapSource Render(int size, Action<DrawingContext, double> draw)
    {
        var dv = new DrawingVisual();
        using (var dc = dv.RenderOpen())
        {
            draw(dc, size);
        }

        var rtb = new RenderTargetBitmap(size, size, 96, 96, PixelFormats.Pbgra32);
        rtb.Render(dv);
        rtb.Freeze();
        return rtb;
    }
}
