using System.Collections.Generic;
using CarCanvas.Domain.Entities;

namespace CarCanvas.Application.DTOs;

public class IntersectionResult
{
    public long TimeElapsedMs { get; set; }
    public int TotalHitsLines { get; set; }
    public int TotalHitsCars { get; set; }
    public int TotalIntersections => TotalHitsLines + TotalHitsCars;
    public List<Point2D> MarkersToDraw { get; set; } = new();
}
