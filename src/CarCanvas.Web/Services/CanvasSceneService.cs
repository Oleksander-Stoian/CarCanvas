using System.Threading.Tasks;
using Microsoft.JSInterop;
using CarCanvas.Application.Interfaces;
using CarCanvas.Domain.Entities;
using System.Collections.Generic;
using System.Linq;

namespace CarCanvas.Web.Services;

public class CanvasSceneService : ICanvasSceneService
{
    private readonly IJSRuntime _js;

    public CanvasSceneService(IJSRuntime js)
    {
        _js = js;
    }

    public async Task InitializeAsync(string canvasId)
    {
        await _js.InvokeVoidAsync("canvasHelper.init", canvasId);
    }

    public async Task SetCoordinateModeAsync(int mode)
    {
        await _js.InvokeVoidAsync("canvasHelper.setCoordinateMode", mode);
    }

    public async Task ClearAsync()
    {
        await _js.InvokeVoidAsync("canvasHelper.clear");
    }

    public async Task DrawCarAsync(IEnumerable<Point2D> points, string color)
    {
        // Flatten points for JS performance
        var flatPoints = points.SelectMany(p => new[] { p.X, p.Y }).ToArray();
        await _js.InvokeVoidAsync("canvasHelper.drawPoints", color, flatPoints);
    }

    public async Task DrawLineAsync(LineSegment line)
    {
        await _js.InvokeVoidAsync("canvasHelper.drawLine", line.Start.X, line.Start.Y, line.End.X, line.End.Y);
    }
    
    public async Task DrawLinesAsync(IEnumerable<LineSegment> lines)
    {
        var flatLines = lines.SelectMany(l => new[] { l.Start.X, l.Start.Y, l.End.X, l.End.Y }).ToArray();
         await _js.InvokeVoidAsync("canvasHelper.drawLinesBatch", flatLines);
    }

    public async Task DrawMarkerAsync(Point2D point)
    {
        await _js.InvokeVoidAsync("canvasHelper.drawMarker", point.X, point.Y);
    }
    
    public async Task DrawMarkersAsync(IEnumerable<Point2D> points)
    {
        var flatPoints = points.SelectMany(p => new[] { p.X, p.Y }).ToArray();
        await _js.InvokeVoidAsync("canvasHelper.drawMarkersBatch", flatPoints);
    }

    public async Task DrawRectAsync(int x, int y, int w, int h, string color)
    {
        await _js.InvokeVoidAsync("canvasHelper.drawRect", x, y, w, h, color);
    }

    public async Task DrawRulerAsync(Point2D start, Point2D end, string label)
    {
        await _js.InvokeVoidAsync("canvasHelper.drawRuler", start.X, start.Y, end.X, end.Y, label);
    }
}
