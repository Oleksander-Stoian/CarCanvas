using System.Collections.Generic;
using System.Threading.Tasks;
using CarCanvas.Domain.Entities;

namespace CarCanvas.Application.Interfaces;

public interface ICanvasSceneService
{
    Task InitializeAsync(string canvasId);
    Task SetCoordinateModeAsync(int mode); // 0 = Canvas, 1 = Math
    Task ClearAsync();
    Task DrawCarAsync(IEnumerable<Point2D> points, string color);
    Task DrawLineAsync(LineSegment line);
    Task DrawLinesAsync(IEnumerable<LineSegment> lines);
    Task DrawMarkerAsync(Point2D point);
    Task DrawMarkersAsync(IEnumerable<Point2D> points);
    Task DrawRectAsync(int x, int y, int w, int h, string color);
    Task DrawRulerAsync(Point2D start, Point2D end, string label);
}
