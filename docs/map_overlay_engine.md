# Map Overlay Engine — Architecture Reference

## Overview

The map overlay system renders game data (bases, connections, player position) on top of the world map image inside `NativeMapView`. It uses WebView2 as the primary rendering surface, with a fallback to the native WPF `MapCanvas` control when WebView2 is unavailable.

The core design constraint is the **WPF airspace issue**: WPF cannot composite its own visual tree on top of a hosted HwndHost (which WebView2 is). All overlay drawing must happen inside the browser DOM, not as a WPF overlay panel placed above the WebView2 control.

---

## Component Map

```
NativeMapViewModel
  └── Serializes MapData → JSON string
        └── NativeMapView.xaml.cs (code-behind)
              └── WebView2.CoreWebView2.ExecuteScriptAsync()
                    └── HTML5 Canvas (in-DOM)
                          └── Renders: grid, connections, bases, player, labels
```

Fallback path:
```
NativeMapView.xaml.cs
  └── Detects WebView2 unavailable
        └── Shows MapCanvas (WPF custom control)
              └── MapCanvas.cs overrides OnRender()
```

---

## ViewModel to JSON Pipeline

`NativeMapViewModel` owns the serialization step. It exposes a method `SerializeMapDataToJson()` that converts the current `MapData` state into a JSON string the JS overlay can consume.

### MapData Contract

```csharp
// Serialized fields (all must be present for overlay to render)
public sealed record MapData
{
    public required IReadOnlyList<BaseMarker> Bases { get; init; }
    public required IReadOnlyList<Connection> Connections { get; init; }
    public required PlayerPosition Player { get; init; }
    public required LayerVisibility Layers { get; init; }
}

public sealed record BaseMarker
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required double X { get; init; }    // normalized 0.0–1.0
    public required double Y { get; init; }    // normalized 0.0–1.0
    public required string Color { get; init; } // hex string e.g. "#00FFC8"
    public required string Faction { get; init; }
}

public sealed record Connection
{
    public required string FromId { get; init; }
    public required string ToId { get; init; }
    public required string LineColor { get; init; }
    public required double LineWidth { get; init; }
}

public sealed record PlayerPosition
{
    public required double X { get; init; }
    public required double Y { get; init; }
}

public sealed record LayerVisibility
{
    public required bool ShowGrid { get; init; }
    public required bool ShowConnections { get; init; }
    public required bool ShowBases { get; init; }
    public required bool ShowPlayer { get; init; }
    public required bool ShowLabels { get; init; }
}
```

### Serialization

Use `System.Text.Json` with camelCase naming:

```csharp
private static readonly JsonSerializerOptions _jsonOptions = new()
{
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    WriteIndented = false
};

public string SerializeMapDataToJson()
{
    return JsonSerializer.Serialize(_mapData, _jsonOptions);
}
```

Coordinates are normalized (0.0–1.0) so the JS layer can scale them to any canvas resolution without a re-inject.

---

## JS Injection Protocol

### Timing: DOMContentLoaded

The overlay script must not be injected before the page is ready. Register the handler once, after `CoreWebView2InitializationCompleted`:

```csharp
_webView.CoreWebView2InitializationCompleted += (_, args) =>
{
    if (!args.IsSuccess) return;
    _webView.CoreWebView2.DOMContentLoaded += OnDomContentLoaded;
};

private async void OnDomContentLoaded(object? sender, CoreWebView2DOMContentLoadedEventArgs e)
{
    await InjectOverlayScriptAsync();
    await RefreshOverlayAsync();
}
```

### Script Injection

`InjectOverlayScriptAsync()` injects the canvas setup code exactly once. It creates a fixed-position `<canvas>` element inside the DOM and defines the `window.updateOverlay(json)` function the C# side calls on every data update.

```csharp
private async Task InjectOverlayScriptAsync()
{
    const string script = """
        (function() {
            if (document.getElementById('arcadia-overlay')) return;
            const canvas = document.createElement('canvas');
            canvas.id = 'arcadia-overlay';
            canvas.style.cssText = 'position:fixed;top:0;left:0;width:100%;height:100%;pointer-events:none;z-index:9999;';
            document.body.appendChild(canvas);

            window.updateOverlay = function(jsonStr) {
                const data = JSON.parse(jsonStr);
                const ctx = canvas.getContext('2d');
                canvas.width = canvas.offsetWidth;
                canvas.height = canvas.offsetHeight;
                ctx.clearRect(0, 0, canvas.width, canvas.height);
                if (data.layers.showGrid)        drawGrid(ctx, canvas);
                if (data.layers.showConnections) drawConnections(ctx, canvas, data);
                if (data.layers.showBases)       drawBases(ctx, canvas, data);
                if (data.layers.showPlayer)      drawPlayer(ctx, canvas, data);
                if (data.layers.showLabels)      drawLabels(ctx, canvas, data);
            };

            function toCanvas(norm, size) { return norm * size; }

            function drawGrid(ctx, canvas) {
                ctx.strokeStyle = 'rgba(0,255,200,0.08)';
                ctx.lineWidth = 1;
                const step = canvas.width / 20;
                for (let x = 0; x < canvas.width; x += step) {
                    ctx.beginPath(); ctx.moveTo(x, 0); ctx.lineTo(x, canvas.height); ctx.stroke();
                }
                for (let y = 0; y < canvas.height; y += step) {
                    ctx.beginPath(); ctx.moveTo(0, y); ctx.lineTo(canvas.width, y); ctx.stroke();
                }
            }

            function drawConnections(ctx, canvas, data) {
                const baseMap = {};
                data.bases.forEach(b => { baseMap[b.id] = b; });
                data.connections.forEach(c => {
                    const a = baseMap[c.fromId], b = baseMap[c.toId];
                    if (!a || !b) return;
                    ctx.strokeStyle = c.lineColor;
                    ctx.lineWidth = c.lineWidth;
                    ctx.beginPath();
                    ctx.moveTo(toCanvas(a.x, canvas.width), toCanvas(a.y, canvas.height));
                    ctx.lineTo(toCanvas(b.x, canvas.width), toCanvas(b.y, canvas.height));
                    ctx.stroke();
                });
            }

            function drawBases(ctx, canvas, data) {
                data.bases.forEach(b => {
                    const cx = toCanvas(b.x, canvas.width);
                    const cy = toCanvas(b.y, canvas.height);
                    ctx.beginPath();
                    ctx.arc(cx, cy, 8, 0, Math.PI * 2);
                    ctx.fillStyle = b.color;
                    ctx.fill();
                    ctx.strokeStyle = '#ffffff';
                    ctx.lineWidth = 1.5;
                    ctx.stroke();
                });
            }

            function drawPlayer(ctx, canvas, data) {
                const px = toCanvas(data.player.x, canvas.width);
                const py = toCanvas(data.player.y, canvas.height);
                const s = 10;
                ctx.beginPath();
                ctx.moveTo(px,      py - s);
                ctx.lineTo(px + s,  py);
                ctx.lineTo(px,      py + s);
                ctx.lineTo(px - s,  py);
                ctx.closePath();
                ctx.fillStyle = '#FFD700';
                ctx.fill();
                ctx.strokeStyle = '#ffffff';
                ctx.lineWidth = 1.5;
                ctx.stroke();
            }

            function drawLabels(ctx, canvas, data) {
                ctx.font = '11px monospace';
                ctx.fillStyle = '#ffffff';
                ctx.textAlign = 'center';
                data.bases.forEach(b => {
                    const cx = toCanvas(b.x, canvas.width);
                    const cy = toCanvas(b.y, canvas.height);
                    ctx.fillText(b.name, cx, cy + 20);
                });
            }
        })();
        """;

    await _webView.CoreWebView2.ExecuteScriptAsync(script);
}
```

### Data Refresh

`RefreshOverlayAsync()` is called whenever `MapData` changes. It serializes the current state and calls `window.updateOverlay`:

```csharp
private async Task RefreshOverlayAsync()
{
    if (_webView.CoreWebView2 is null) return;
    var json = _viewModel.SerializeMapDataToJson();
    // Escape backticks and backslashes before embedding in JS template literal
    var escaped = json.Replace("\\", "\\\\").Replace("`", "\\`");
    await _webView.CoreWebView2.ExecuteScriptAsync($"window.updateOverlay && window.updateOverlay(`{escaped}`);");
}
```

---

## Rendering Order

The canvas is cleared and redrawn in this sequence on every update. Order is load-bearing — do not change it.

| Order | Layer | Purpose |
|-------|-------|---------|
| 1 | Grid | Reference lines, lowest visual priority |
| 2 | Connections | Lines between bases, drawn under base circles |
| 3 | Bases | Colored circles, drawn above connection lines |
| 4 | Player | Diamond marker, always on top of base markers |
| 5 | Labels | Text, drawn last so it is never clipped by shapes |

---

## Layer Visibility Flag Mapping

C# `bool` properties on `LayerVisibility` map directly to JS `data.layers.*` fields via camelCase JSON serialization. The JS guard pattern is:

```javascript
if (data.layers.showGrid) drawGrid(ctx, canvas);
```

The C# side maps:

| C# Property | JSON Key | JS Path |
|---|---|---|
| `ShowGrid` | `showGrid` | `data.layers.showGrid` |
| `ShowConnections` | `showConnections` | `data.layers.showConnections` |
| `ShowBases` | `showBases` | `data.layers.showBases` |
| `ShowPlayer` | `showPlayer` | `data.layers.showPlayer` |
| `ShowLabels` | `showLabels` | `data.layers.showLabels` |

Layer toggle commands in `NativeMapViewModel` update the `LayerVisibility` record, then fire `RefreshOverlayAsync()` through the view's event subscription.

---

## Performance: Debounce Strategy

Do not call `RefreshOverlayAsync()` on every property change notification. Rapid property updates (e.g., player position ticking every frame) will flood `ExecuteScriptAsync` calls.

Apply a debounce in the code-behind:

```csharp
private CancellationTokenSource? _debounce;

private async Task ScheduleOverlayRefresh()
{
    _debounce?.Cancel();
    _debounce = new CancellationTokenSource();
    try
    {
        await Task.Delay(100, _debounce.Token); // 100ms debounce window
        await RefreshOverlayAsync();
    }
    catch (OperationCanceledException) { }
}
```

Subscribe to ViewModel changes via `PropertyChanged` and route through `ScheduleOverlayRefresh()`.

---

## Fallback: Native WPF MapCanvas

When `CoreWebView2InitializationCompleted` fires with `IsSuccess = false`, or when `WebView2` is not installed on the machine, the view falls back to the native `MapCanvas` WPF control.

```csharp
_webView.CoreWebView2InitializationCompleted += (_, args) =>
{
    if (!args.IsSuccess)
    {
        Dispatcher.Invoke(() =>
        {
            _webView.Visibility = Visibility.Collapsed;
            _fallbackCanvas.Visibility = Visibility.Visible;
        });
    }
};
```

`MapCanvas` (`Controls/MapCanvas.cs`) overrides `OnRender(DrawingContext dc)` and draws the same visual information using WPF primitives (`EllipseGeometry`, `LineGeometry`, `FormattedText`). It does not support the grid layer due to WPF rendering cost at scale.

---

## Known Limitation: WPF Airspace Issue

WPF cannot render its own visual tree on top of a `HwndHost`-based control (which `WebView2` is). Attempting to place a WPF `Canvas` or `Grid` above the `WebView2` in Z-order will result in the WPF elements being invisible — the WebView2 surface always wins.

**Workaround in use:** All overlay drawing is done inside the browser DOM via the injected `<canvas>` element. The canvas is styled with `pointer-events: none` so it does not intercept mouse events intended for the map background.

**Do not attempt** to layer WPF controls over the `WebView2` control. Use in-DOM injection exclusively.

---

## Files Involved

| File | Role |
|------|------|
| `ViewModels/NativeMapViewModel.cs` | Owns `MapData`, serialization, layer toggle commands |
| `Views/NativeMapView.xaml.cs` | WebView2 lifecycle, script injection, debounced refresh |
| `Views/NativeMapView.xaml` | Layout: WebView2 + fallback MapCanvas stacked in Grid |
| `Controls/MapCanvas.cs` | WPF fallback renderer using `OnRender` |
| `Models/MapData.cs` | `MapData`, `BaseMarker`, `Connection`, `PlayerPosition`, `LayerVisibility` records |
