namespace ArcadiaTracker.App.Controls;

using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using GameCompanion.Module.StarRupture.Models;

/// <summary>
/// Custom WPF control that renders a 2D top-down map from save data.
/// Uses immediate-mode rendering via OnRender for performance.
/// </summary>
public sealed class MapCanvas : FrameworkElement
{
    // Rendering pens and brushes (frozen for performance)
    private static readonly Pen GridPen = CreatePen("#1A2A3A", 0.5);
    private static readonly Pen GridPenMajor = CreatePen("#2A3A4A", 1.0);
    private static readonly Brush FogBrush = CreateBrush("#CC0A0A0F");
    private static readonly Brush PlayerBrush = CreateBrush("#00D4FF");
    private static readonly Pen PlayerPen = CreatePen("#00D4FF", 2.0);
    private static readonly Brush BaseActiveBrush = CreateBrush("#332ED573");
    private static readonly Pen BaseActivePen = CreatePen("#2ED573", 2.0);
    private static readonly Brush BaseWarningBrush = CreateBrush("#33FF6B35");
    private static readonly Pen BaseWarningPen = CreatePen("#FF6B35", 2.0);
    private static readonly Brush BaseBrokenBrush = CreateBrush("#33FF4757");
    private static readonly Pen BaseBrokenPen = CreatePen("#FF4757", 2.0);
    private static readonly Pen ConnectionActivePen = CreatePen("#2ED573", 1.5);
    private static readonly Pen ConnectionIdlePen = CreatePen("#FF6B35", 1.0, DashStyles.Dash);
    private static readonly Pen ConnectionBrokenPen = CreatePen("#FF4757", 1.5, DashStyles.Dash);
    private static readonly Brush LabelBrush = CreateBrush("#FFFFFF");
    private static readonly Brush LabelBackgroundBrush = CreateBrush("#AA1A1A2E");
    private static readonly Typeface LabelTypeface = new("Segoe UI");

    // Transform state
    private double _scale = 1.0;
    private double _offsetX;
    private double _offsetY;
    private Point _lastMousePosition;
    private bool _isDragging;
    private bool _needsInitialFit = true;

    public static readonly DependencyProperty MapDataProperty =
        DependencyProperty.Register(nameof(MapData), typeof(MapData), typeof(MapCanvas),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender, OnMapDataChanged));

    public static readonly DependencyProperty VisibleLayersProperty =
        DependencyProperty.Register(nameof(VisibleLayers), typeof(MapLayerFlags), typeof(MapCanvas),
            new FrameworkPropertyMetadata(MapLayerFlags.All, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty SelectedBaseProperty =
        DependencyProperty.Register(nameof(SelectedBase), typeof(BaseCluster), typeof(MapCanvas),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

    public MapData? MapData
    {
        get => (MapData?)GetValue(MapDataProperty);
        set => SetValue(MapDataProperty, value);
    }

    public MapLayerFlags VisibleLayers
    {
        get => (MapLayerFlags)GetValue(VisibleLayersProperty);
        set => SetValue(VisibleLayersProperty, value);
    }

    public BaseCluster? SelectedBase
    {
        get => (BaseCluster?)GetValue(SelectedBaseProperty);
        set => SetValue(SelectedBaseProperty, value);
    }

    public double ZoomLevel => _scale;

    public event EventHandler<BaseCluster>? BaseClicked;
    public event EventHandler? ViewChanged;

    public MapCanvas()
    {
        ClipToBounds = true;
        Focusable = true;
    }

    // Enable hit testing on the entire area (FrameworkElement doesn't have Background)
    protected override HitTestResult? HitTestCore(PointHitTestParameters hitTestParameters)
    {
        return new PointHitTestResult(this, hitTestParameters.HitPoint);
    }

    private static void OnMapDataChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is MapCanvas canvas)
        {
            canvas._needsInitialFit = true;
            canvas.InvalidateVisual();
        }
    }

    protected override void OnRender(DrawingContext dc)
    {
        base.OnRender(dc);

        var mapData = MapData;
        if (mapData == null)
        {
            DrawEmptyState(dc);
            return;
        }

        if (_needsInitialFit)
        {
            FitToContent();
            _needsInitialFit = false;
        }

        var layers = VisibleLayers;

        // Layer 1: Background
        dc.DrawRectangle(CreateBrush("#0A0A0F"), null, new Rect(0, 0, ActualWidth, ActualHeight));

        // Layer 2: Grid
        if (layers.HasFlag(MapLayerFlags.Grid))
            DrawGrid(dc);

        // Layer 3: Connections
        if (layers.HasFlag(MapLayerFlags.Connections))
            DrawConnections(dc, mapData);

        // Layer 4: Bases
        if (layers.HasFlag(MapLayerFlags.Bases))
            DrawBases(dc, mapData);

        // Layer 5: Player
        if (layers.HasFlag(MapLayerFlags.Player))
            DrawPlayer(dc, mapData);

        // Layer 6: Labels
        if (layers.HasFlag(MapLayerFlags.Labels))
            DrawLabels(dc, mapData);
    }

    private void DrawEmptyState(DrawingContext dc)
    {
        dc.DrawRectangle(CreateBrush("#0A0A0F"), null, new Rect(0, 0, ActualWidth, ActualHeight));

        var text = new FormattedText(
            "No map data available.\nLoad a save file to see your world.",
            System.Globalization.CultureInfo.CurrentCulture,
            FlowDirection.LeftToRight,
            LabelTypeface,
            16,
            CreateBrush("#606070"),
            VisualTreeHelper.GetDpi(this).PixelsPerDip);

        var textPos = new Point(
            (ActualWidth - text.Width) / 2,
            (ActualHeight - text.Height) / 2);

        dc.DrawText(text, textPos);
    }

    private void DrawGrid(DrawingContext dc)
    {
        // Determine grid spacing based on zoom level
        var baseSpacing = 10000.0; // World units
        var screenSpacing = baseSpacing * _scale;

        // Adjust grid density so lines are 30-150px apart on screen
        while (screenSpacing < 30) { baseSpacing *= 5; screenSpacing = baseSpacing * _scale; }
        while (screenSpacing > 150) { baseSpacing /= 5; screenSpacing = baseSpacing * _scale; }

        // Calculate visible world bounds
        var worldLeft = ScreenToWorldX(0);
        var worldRight = ScreenToWorldX(ActualWidth);
        var worldTop = ScreenToWorldY(0);
        var worldBottom = ScreenToWorldY(ActualHeight);

        var startX = Math.Floor(worldLeft / baseSpacing) * baseSpacing;
        var startY = Math.Floor(worldTop / baseSpacing) * baseSpacing;

        for (var wx = startX; wx <= worldRight; wx += baseSpacing)
        {
            var sx = WorldToScreenX(wx);
            var isMajor = Math.Abs(wx % (baseSpacing * 5)) < 1;
            dc.DrawLine(isMajor ? GridPenMajor : GridPen,
                new Point(sx, 0), new Point(sx, ActualHeight));
        }

        for (var wy = startY; wy <= worldBottom; wy += baseSpacing)
        {
            var sy = WorldToScreenY(wy);
            var isMajor = Math.Abs(wy % (baseSpacing * 5)) < 1;
            dc.DrawLine(isMajor ? GridPenMajor : GridPen,
                new Point(0, sy), new Point(ActualWidth, sy));
        }
    }

    private void DrawConnections(DrawingContext dc, MapData mapData)
    {
        foreach (var conn in mapData.Connections)
        {
            var start = WorldToScreen(conn.Start);
            var end = WorldToScreen(conn.End);

            var pen = conn.Status switch
            {
                ConnectionStatus.Active => ConnectionActivePen,
                ConnectionStatus.Idle => ConnectionIdlePen,
                ConnectionStatus.Broken => ConnectionBrokenPen,
                _ => ConnectionActivePen
            };

            dc.DrawLine(pen, start, end);
        }
    }

    private void DrawBases(DrawingContext dc, MapData mapData)
    {
        foreach (var baseCluster in mapData.Bases)
        {
            var center = WorldToScreen(baseCluster.Center);
            var screenRadius = Math.Max(baseCluster.Radius * _scale, 12);

            // Color based on health status
            Brush fill;
            Pen outline;
            if (baseCluster.MalfunctionCount > 0)
            {
                fill = BaseBrokenBrush;
                outline = BaseBrokenPen;
            }
            else if (baseCluster.DisabledCount > 0)
            {
                fill = BaseWarningBrush;
                outline = BaseWarningPen;
            }
            else
            {
                fill = BaseActiveBrush;
                outline = BaseActivePen;
            }

            // Selected base highlight
            if (SelectedBase?.Id == baseCluster.Id)
            {
                dc.DrawEllipse(null, CreatePen("#00D4FF", 3.0),
                    center, screenRadius + 4, screenRadius + 4);
            }

            dc.DrawEllipse(fill, outline, center, screenRadius, screenRadius);

            // Draw core marker at center
            dc.DrawEllipse(outline.Brush, null, center, 4, 4);
        }
    }

    private void DrawPlayer(DrawingContext dc, MapData mapData)
    {
        var pos = WorldToScreen(mapData.PlayerPosition);

        // Draw player marker (diamond shape)
        const double size = 8;
        var geometry = new StreamGeometry();
        using (var ctx = geometry.Open())
        {
            ctx.BeginFigure(new Point(pos.X, pos.Y - size), true, true);
            ctx.LineTo(new Point(pos.X + size, pos.Y), true, false);
            ctx.LineTo(new Point(pos.X, pos.Y + size), true, false);
            ctx.LineTo(new Point(pos.X - size, pos.Y), true, false);
        }
        geometry.Freeze();

        dc.DrawGeometry(PlayerBrush, PlayerPen, geometry);
    }

    private void DrawLabels(DrawingContext dc, MapData mapData)
    {
        var dpi = VisualTreeHelper.GetDpi(this).PixelsPerDip;

        foreach (var baseCluster in mapData.Bases)
        {
            var center = WorldToScreen(baseCluster.Center);
            var screenRadius = Math.Max(baseCluster.Radius * _scale, 12);

            // Base name
            var nameText = new FormattedText(
                baseCluster.Name,
                System.Globalization.CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                LabelTypeface,
                12,
                LabelBrush,
                dpi);

            var namePos = new Point(
                center.X - nameText.Width / 2,
                center.Y - screenRadius - nameText.Height - 6);

            // Background for readability
            dc.DrawRoundedRectangle(LabelBackgroundBrush, null,
                new Rect(namePos.X - 4, namePos.Y - 2, nameText.Width + 8, nameText.Height + 4), 3, 3);

            dc.DrawText(nameText, namePos);

            // Building count subtitle
            var countText = new FormattedText(
                $"{baseCluster.TotalBuildingCount} buildings",
                System.Globalization.CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                LabelTypeface,
                10,
                CreateBrush("#A0A0B0"),
                dpi);

            var countPos = new Point(
                center.X - countText.Width / 2,
                center.Y + screenRadius + 6);

            dc.DrawRoundedRectangle(LabelBackgroundBrush, null,
                new Rect(countPos.X - 4, countPos.Y - 2, countText.Width + 8, countText.Height + 4), 3, 3);

            dc.DrawText(countText, countPos);
        }

        // Player label
        var playerPos = WorldToScreen(mapData.PlayerPosition);
        var playerText = new FormattedText(
            "You",
            System.Globalization.CultureInfo.CurrentCulture,
            FlowDirection.LeftToRight,
            LabelTypeface,
            10,
            PlayerBrush,
            dpi);

        dc.DrawText(playerText, new Point(playerPos.X + 12, playerPos.Y - 6));
    }

    // --- Pan/Zoom ---

    protected override void OnMouseWheel(MouseWheelEventArgs e)
    {
        var mousePos = e.GetPosition(this);
        var worldX = ScreenToWorldX(mousePos.X);
        var worldY = ScreenToWorldY(mousePos.Y);

        var factor = e.Delta > 0 ? 1.15 : 1.0 / 1.15;
        _scale = Math.Clamp(_scale * factor, 0.0001, 100.0);

        // Adjust offset to zoom centered on mouse
        _offsetX = mousePos.X - worldX * _scale;
        _offsetY = mousePos.Y - worldY * _scale;

        InvalidateVisual();
        ViewChanged?.Invoke(this, EventArgs.Empty);
        e.Handled = true;
    }

    protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
    {
        _lastMousePosition = e.GetPosition(this);
        _isDragging = true;
        CaptureMouse();
        e.Handled = true;
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        if (!_isDragging) return;

        var currentPos = e.GetPosition(this);
        var dx = currentPos.X - _lastMousePosition.X;
        var dy = currentPos.Y - _lastMousePosition.Y;

        _offsetX += dx;
        _offsetY += dy;

        _lastMousePosition = currentPos;
        InvalidateVisual();
        ViewChanged?.Invoke(this, EventArgs.Empty);
    }

    protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
    {
        if (_isDragging)
        {
            var currentPos = e.GetPosition(this);
            var dragDistance = (currentPos - _lastMousePosition).Length;

            // If it was a click (not a drag), check for base selection
            if (dragDistance < 5)
            {
                HandleClick(currentPos);
            }

            _isDragging = false;
            ReleaseMouseCapture();
        }
    }

    private void HandleClick(Point screenPos)
    {
        var mapData = MapData;
        if (mapData == null) return;

        // Check if click hit a base
        foreach (var baseCluster in mapData.Bases)
        {
            var center = WorldToScreen(baseCluster.Center);
            var screenRadius = Math.Max(baseCluster.Radius * _scale, 12);

            if ((screenPos - center).Length <= screenRadius + 5)
            {
                SelectedBase = baseCluster;
                BaseClicked?.Invoke(this, baseCluster);
                InvalidateVisual();
                return;
            }
        }

        // Click on empty space deselects
        SelectedBase = null;
        InvalidateVisual();
    }

    // --- Public commands ---

    public void FitToContent()
    {
        var mapData = MapData;
        if (mapData == null || ActualWidth <= 0 || ActualHeight <= 0) return;

        var bounds = mapData.WorldBounds;
        if (bounds.Width <= 0 || bounds.Height <= 0) return;

        var scaleX = ActualWidth / bounds.Width;
        var scaleY = ActualHeight / bounds.Height;
        _scale = Math.Min(scaleX, scaleY) * 0.9; // 90% to leave margin

        _offsetX = (ActualWidth / 2) - (bounds.CenterX * _scale);
        _offsetY = (ActualHeight / 2) - (bounds.CenterY * _scale);

        InvalidateVisual();
        ViewChanged?.Invoke(this, EventArgs.Empty);
    }

    public void CenterOnPosition(WorldPosition pos)
    {
        _offsetX = (ActualWidth / 2) - (pos.X * _scale);
        _offsetY = (ActualHeight / 2) - (pos.Y * _scale);

        InvalidateVisual();
        ViewChanged?.Invoke(this, EventArgs.Empty);
    }

    // --- Coordinate transforms ---

    private double WorldToScreenX(double worldX) => worldX * _scale + _offsetX;
    private double WorldToScreenY(double worldY) => worldY * _scale + _offsetY;
    private double ScreenToWorldX(double screenX) => (screenX - _offsetX) / _scale;
    private double ScreenToWorldY(double screenY) => (screenY - _offsetY) / _scale;

    private Point WorldToScreen(WorldPosition pos) =>
        new(WorldToScreenX(pos.X), WorldToScreenY(pos.Y));

    // --- Static helpers ---

    private static Pen CreatePen(string color, double thickness, DashStyle? dashStyle = null)
    {
        var pen = new Pen(CreateBrush(color), thickness);
        if (dashStyle != null)
            pen.DashStyle = dashStyle;
        pen.Freeze();
        return pen;
    }

    private static SolidColorBrush CreateBrush(string color)
    {
        var brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(color));
        brush.Freeze();
        return brush;
    }
}

[Flags]
public enum MapLayerFlags
{
    None = 0,
    Grid = 1,
    Bases = 2,
    Connections = 4,
    Player = 8,
    Labels = 16,
    All = Grid | Bases | Connections | Player | Labels
}
