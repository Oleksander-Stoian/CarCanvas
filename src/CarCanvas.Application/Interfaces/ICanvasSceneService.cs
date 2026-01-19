using System.Collections.Generic;
using System.Threading.Tasks;
using CarCanvas.Domain.Entities;

namespace CarCanvas.Application.Interfaces;

public interface ICanvasSceneService
{
    Task InitializeAsync(string canvasId);
    Task ClearAsync();
    Task DrawCarAsync(IEnumerable<Point2D> points, string color);
    Task DrawLineAsync(LineSegment line);
    Task DrawLinesAsync(IEnumerable<LineSegment> lines);
    Task DrawMarkerAsync(Point2D point);
    Task DrawMarkersAsync(IEnumerable<Point2D> points);
}
