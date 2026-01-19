using System.Collections.Generic;
using CarCanvas.Domain.Entities;
using CarCanvas.Domain.ValueObjects;

namespace CarCanvas.Application.DTOs;

public class IntersectionResult
{
    public long TimeElapsedMs { get; set; }
    public int TotalHitsLines { get; set; }
    public int TotalHitsCars { get; set; }
    public int TotalIntersections => TotalHitsLines + TotalHitsCars;
    public List<Point2D> MarkersToDraw { get; set; } = new();

    // Debug Stats
    public int RejectedByLineAabb { get; set; }
    public int RejectedBySegmentAabb { get; set; }
    public int ProcessedByBresenham { get; set; }

    // Debug AABBs
    public Aabb? TargetCarAabb { get; set; }
    public Aabb? OtherCarAabb { get; set; }
}
