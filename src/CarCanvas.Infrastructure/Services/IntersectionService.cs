using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using CarCanvas.Application;
using CarCanvas.Application.DTOs;
using CarCanvas.Application.Interfaces;
using CarCanvas.Domain.Entities;
using CarCanvas.Domain.ValueObjects;
using CarCanvas.Infrastructure.Algorithms;

namespace CarCanvas.Infrastructure.Services;

public class IntersectionService : IIntersectionService
{
    // Cache: CarId -> (TransformHash, PixelSet)
    // We need a way to compare Transforms efficiently.
    private readonly Dictionary<int, (int TransformHash, HashSet<int> PixelSet)> _pixelSetCache = new();
    
    public async Task<IntersectionResult> FindIntersectionsAsync(
        CarModel targetCar, 
        CarModel otherCar, 
        IEnumerable<LineSegment> lines,
        AppOptions options)
    {
        // Offload to thread pool for "heavy" operations
        return await Task.Run(() => FindIntersectionsInternal(targetCar, otherCar, lines, options));
    }

    private IntersectionResult FindIntersectionsInternal(
        CarModel targetCar, 
        CarModel otherCar, 
        IEnumerable<LineSegment> lines,
        AppOptions options)
    {
        var sw = Stopwatch.StartNew();
        var result = new IntersectionResult();
        
        // 1. Get PixelSets
        var targetPixels = GetOrUpdatePixelSet(targetCar, options.StrideKey, options.CanvasWidth, options.CanvasHeight);
        var otherPixels = GetOrUpdatePixelSet(otherCar, options.StrideKey, options.CanvasWidth, options.CanvasHeight);

        // 2. Intersections with Other Car (PixelSet Intersection)
        // We only care if target intersects other.
        // Intersect sets.
        // Optimization: Iterate smaller set check larger.
        
        var carHits = 0;
        // Check overlap
        foreach (var key in targetPixels)
        {
            if (otherPixels.Contains(key))
            {
                carHits++;
                if (result.MarkersToDraw.Count < options.MaxMarkersToDraw)
                {
                    result.MarkersToDraw.Add(KeyToPoint(key, options.StrideKey));
                }
            }
        }
        result.TotalHitsCars = carHits;

        // 3. Intersections with Lines
        // For each line, bresenham -> check if point in targetPixels
        int lineHits = 0;
        foreach (var line in lines)
        {
            foreach (var p in Bresenham.GetPointsOnLine(line.Start, line.End))
            {
                // Check bounds (optional, but good for safety)
                if (p.X < 0 || p.X >= options.CanvasWidth || p.Y < 0 || p.Y >= options.CanvasHeight)
                    continue;

                int key = p.Y * options.StrideKey + p.X;
                if (targetPixels.Contains(key))
                {
                    lineHits++;
                    // Add marker
                    if (result.MarkersToDraw.Count < options.MaxMarkersToDraw)
                    {
                        result.MarkersToDraw.Add(p);
                    }
                }
            }
        }
        result.TotalHitsLines = lineHits;

        sw.Stop();
        result.TimeElapsedMs = sw.ElapsedMilliseconds;
        return result;
    }

    private HashSet<int> GetOrUpdatePixelSet(CarModel car, int stride, int width, int height)
    {
        int currentHash = ComputeTransformHash(car.Transform);

        if (_pixelSetCache.TryGetValue(car.Id, out var cached))
        {
            if (cached.TransformHash == currentHash)
            {
                return cached.PixelSet;
            }
        }

        // Recompute
        var pixels = new HashSet<int>();
        var transformedPoints = PointTransformer.TransformPoints(car.OriginalPoints, car.Center, car.Transform);
        
        foreach (var p in transformedPoints)
        {
            // Filter out of bounds points if necessary, or just allow them but they won't match anything on canvas [0..W, 0..H]
            // We should filter to keep HashSet clean and matching valid canvas keys
            if (p.X >= 0 && p.X < width && p.Y >= 0 && p.Y < height)
            {
                pixels.Add(p.Y * stride + p.X);
            }
        }

        _pixelSetCache[car.Id] = (currentHash, pixels);
        return pixels;
    }

    private int ComputeTransformHash(Transform t)
    {
        // Normalize angle to reduce noise (e.g. from sliders)
        // Also handle 360 wrapping if desired, but rounding is most important for stability
        double normalizedAngle = Math.Round(t.RotationAngle, 3);
        return HashCode.Combine(t.TranslateX, t.TranslateY, normalizedAngle);
    }

    private Point2D KeyToPoint(int key, int stride)
    {
        return new Point2D(key % stride, key / stride);
    }
}
