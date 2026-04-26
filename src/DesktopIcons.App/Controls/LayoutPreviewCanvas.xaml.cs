using DesktopIcons.Core.Models;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;

namespace DesktopIcons.App.Controls;

public sealed partial class LayoutPreviewCanvas : UserControl
{
    public static readonly DependencyProperty LayoutProperty = DependencyProperty.Register(
        nameof(Layout), typeof(LayoutFile), typeof(LayoutPreviewCanvas),
        new PropertyMetadata(null, (d, _) => ((LayoutPreviewCanvas)d).Redraw()));

    public static readonly DependencyProperty DotSizeProperty = DependencyProperty.Register(
        nameof(DotSize), typeof(double), typeof(LayoutPreviewCanvas),
        new PropertyMetadata(4.0, (d, _) => ((LayoutPreviewCanvas)d).Redraw()));

    public LayoutFile? Layout
    {
        get => (LayoutFile?)GetValue(LayoutProperty);
        set => SetValue(LayoutProperty, value);
    }

    public double DotSize
    {
        get => (double)GetValue(DotSizeProperty);
        set => SetValue(DotSizeProperty, value);
    }

    public LayoutPreviewCanvas()
    {
        InitializeComponent();
        SizeChanged += (_, _) => Redraw();
    }

    private void Redraw()
    {
        Surface.Children.Clear();
        if (Layout is null) return;

        // Icon coords: virtual-screen origin (>= 0) from LVM_GETITEMPOSITION.
        // Monitor / work-area coords: primary-screen origin (can be negative).
        // Shift monitor frames into virtual-screen origin so they align with icons.
        double monShiftX = 0, monShiftY = 0;
        if (Layout.Monitors is { Count: > 0 })
        {
            monShiftX = -Math.Min(0, Layout.Monitors.Min(m => m.X));
            monShiftY = -Math.Min(0, Layout.Monitors.Min(m => m.Y));
        }

        var (minX, minY, maxX, maxY) = ComputeBounds(Layout, monShiftX, monShiftY);
        if (maxX <= minX || maxY <= minY) return;

        var w = ActualWidth;
        var h = ActualHeight;
        if (w <= 0 || h <= 0) return;

        const double padding = 4;
        var availW = Math.Max(1, w - padding * 2);
        var availH = Math.Max(1, h - padding * 2);
        var scale = Math.Min(availW / (maxX - minX), availH / (maxY - minY));

        var contentW = (maxX - minX) * scale;
        var contentH = (maxY - minY) * scale;
        var offsetX = (w - contentW) / 2.0;
        var offsetY = (h - contentH) / 2.0;

        double MapX(double x) => offsetX + (x - minX) * scale;
        double MapY(double y) => offsetY + (y - minY) * scale;

        var frameBrush = (Brush)Application.Current.Resources["MonitorFrameBrush"];
        var dotBrush = (Brush)Application.Current.Resources["IconDotBrush"];

        // Build per-monitor display geometries (frame + inset inner rect for dots).
        var geometries = new List<MonitorGeometry>();
        if (Layout.Monitors is { Count: > 0 })
        {
            foreach (var m in Layout.Monitors)
            {
                var (fx, fy, fw, fh) = FrameSource(m);
                var srcLeft = fx + monShiftX;
                var srcTop = fy + monShiftY;
                var dx = MapX(srcLeft);
                var dy = MapY(srcTop);
                var dw = Math.Max(1, fw * scale);
                var dh = Math.Max(1, fh * scale);

                // Inner padding keeps dots away from the frame edge regardless of canvas size.
                var innerPad = Math.Clamp(Math.Min(dw, dh) * 0.05, 3.0, 10.0);
                innerPad = Math.Min(innerPad, (Math.Min(dw, dh) - 1) / 2.0);
                innerPad = Math.Max(0, innerPad);

                geometries.Add(new MonitorGeometry(
                    srcLeft, srcTop, fw, fh,
                    dx + innerPad, dy + innerPad,
                    Math.Max(1, dw - 2 * innerPad),
                    Math.Max(1, dh - 2 * innerPad)));

                var rect = new Rectangle
                {
                    Width = dw,
                    Height = dh,
                    Stroke = frameBrush,
                    StrokeThickness = 1.0,
                    RadiusX = 2,
                    RadiusY = 2,
                    Fill = new SolidColorBrush(Colors.Transparent)
                };
                Canvas.SetLeft(rect, dx);
                Canvas.SetTop(rect, dy);
                Surface.Children.Add(rect);
            }
        }

        // LVM_GETITEMPOSITION reports the TOP-LEFT of each icon's grid cell (~75-90px @ 100% DPI).
        // Visual icon center sits roughly +40,+40 inside the cell.
        const double iconCenterOffsetX = 40;
        const double iconCenterOffsetY = 40;

        // 75 ≈ icon grid step in source px @ 100% DPI; 0.7 caps dot at ~70% of cell so neighbors
        // stay visually distinct without overlapping when icons are dense.
        var dotCap = scale * 75.0 * 0.7;
        var dot = Math.Max(1.0, Math.Min(DotSize, dotCap));

        foreach (var icon in Layout.Icons)
        {
            var ix = icon.X + iconCenterOffsetX;
            var iy = icon.Y + iconCenterOffsetY;

            double dotX = double.NaN, dotY = double.NaN;
            foreach (var g in geometries)
            {
                if (ix >= g.SrcLeft && ix < g.SrcLeft + g.SrcWidth &&
                    iy >= g.SrcTop && iy < g.SrcTop + g.SrcHeight)
                {
                    var fracX = (ix - g.SrcLeft) / g.SrcWidth;
                    var fracY = (iy - g.SrcTop) / g.SrcHeight;
                    dotX = g.InnerLeft + fracX * g.InnerWidth;
                    dotY = g.InnerTop + fracY * g.InnerHeight;
                    break;
                }
            }
            if (double.IsNaN(dotX))
            {
                dotX = MapX(ix);
                dotY = MapY(iy);
            }

            var e = new Ellipse
            {
                Width = dot,
                Height = dot,
                Fill = dotBrush
            };
            Canvas.SetLeft(e, dotX - dot / 2.0);
            Canvas.SetTop(e, dotY - dot / 2.0);
            Surface.Children.Add(e);
        }
    }

    private static (double X, double Y, double W, double H) FrameSource(MonitorRect m)
    {
        if (m.WorkX is { } wx && m.WorkY is { } wy &&
            m.WorkW is { } ww && ww > 0 &&
            m.WorkH is { } wh && wh > 0)
        {
            return (wx, wy, ww, wh);
        }
        return (m.X, m.Y, m.W, m.H);
    }

    private static (double minX, double minY, double maxX, double maxY) ComputeBounds(
        LayoutFile layout, double monShiftX, double monShiftY)
    {
        double minX = double.MaxValue, minY = double.MaxValue;
        double maxX = double.MinValue, maxY = double.MinValue;

        if (layout.Monitors is { Count: > 0 })
        {
            foreach (var m in layout.Monitors)
            {
                var (fx, fy, fw, fh) = FrameSource(m);
                var mx = fx + monShiftX;
                var my = fy + monShiftY;
                if (mx < minX) minX = mx;
                if (my < minY) minY = my;
                if (mx + fw > maxX) maxX = mx + fw;
                if (my + fh > maxY) maxY = my + fh;
            }
        }

        foreach (var icon in layout.Icons)
        {
            if (icon.X < minX) minX = icon.X;
            if (icon.Y < minY) minY = icon.Y;
            if (icon.X > maxX) maxX = icon.X;
            if (icon.Y > maxY) maxY = icon.Y;
        }

        if (minX == double.MaxValue)
            return (0, 0, 0, 0);
        return (minX, minY, maxX, maxY);
    }

    private readonly record struct MonitorGeometry(
        double SrcLeft, double SrcTop, double SrcWidth, double SrcHeight,
        double InnerLeft, double InnerTop, double InnerWidth, double InnerHeight);
}
