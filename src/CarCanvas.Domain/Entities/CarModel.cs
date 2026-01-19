using System;
using System.Collections.Generic;
using System.Linq;
using CarCanvas.Domain.ValueObjects;

namespace CarCanvas.Domain.Entities;

public class CarModel
{
    public int Id { get; }
    public IReadOnlyList<Point2D> OriginalPoints { get; }
    public Transform Transform { get; } = new();
    
    // Bounding box center of original points
    public Point2D Center { get; private set; }

    public CarModel(int id, IEnumerable<Point2D> points)
    {
        Id = id;
        OriginalPoints = points.ToList();
        CalculateCenter();
    }

    private void CalculateCenter()
    {
        if (OriginalPoints.Count == 0)
        {
            Center = new Point2D(0, 0);
            return;
        }

        int minX = OriginalPoints.Min(p => p.X);
        int maxX = OriginalPoints.Max(p => p.X);
        int minY = OriginalPoints.Min(p => p.Y);
        int maxY = OriginalPoints.Max(p => p.Y);

        Center = new Point2D((minX + maxX) / 2, (minY + maxY) / 2);
    }
}
