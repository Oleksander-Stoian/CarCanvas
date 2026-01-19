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
    // Cache: CarId -> (TransformHash, PixelSet, Aabb)
    // We need a way to compare Transforms efficiently.
    private readonly Dictionary<int, (int TransformHash, HashSet<int> PixelSet, Aabb Box)> _pixelSetCache = new();
    
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
        var (targetPixels, targetBox) = GetOrUpdatePixelSet(targetCar, options.StrideKey, options.CanvasWidth, options.CanvasHeight);
        var (otherPixels, otherBox) = GetOrUpdatePixelSet(otherCar, options.StrideKey, options.CanvasWidth, options.CanvasHeight);
        
        // Save AABBs to result for debug
        result.TargetCarAabb = targetBox;
        result.OtherCarAabb = otherBox;

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
        
        // Padded AABB for fast check (safe against rounding errors)
        var targetBoxPadded = targetBox.Inflate(2, options.CanvasWidth, options.CanvasHeight);

        foreach (var line in lines)
        {
            // Super-fast check: AABB vs AABB
            // Construct line AABB inline (O(1)) with 1px padding
            int lxMin, lxMax, lyMin, lyMax;
            if (line.Start.X < line.End.X) { lxMin = line.Start.X - 1; lxMax = line.End.X + 1; }
            else { lxMin = line.End.X - 1; lxMax = line.Start.X + 1; }
            
            if (line.Start.Y < line.End.Y) { lyMin = line.Start.Y - 1; lyMax = line.End.Y + 1; }
            else { lyMin = line.End.Y - 1; lyMax = line.Start.Y + 1; }

            var lineAabb = new Aabb(lxMin, lxMax, lyMin, lyMax);

            // Check intersection
            if (!targetBoxPadded.Intersects(lineAabb))
            {
                result.RejectedByLineAabb++;
                continue;
            }

            // Fast-check: Segment vs AABB intersection (Cohen-Sutherland)
            // Still useful because a diagonal line might cross the box's AABB but not the box itself
            if (!GeometryUtils.SegmentIntersectsAabb(line, targetBox, padding: 2))
            {
                result.RejectedBySegmentAabb++;
                continue;
            }

            result.ProcessedByBresenham++;
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

    private (HashSet<int> PixelSet, Aabb Box) GetOrUpdatePixelSet(CarModel car, int stride, int width, int height)
    {
        int currentHash = ComputeTransformHash(car.Transform);

        if (_pixelSetCache.TryGetValue(car.Id, out var cached))
        {
            if (cached.TransformHash == currentHash)
            {
                return (cached.PixelSet, cached.Box);
            }
        }

        // Recompute
        var pixels = new HashSet<int>();
        var transformedPoints = PointTransformer.TransformPoints(car.OriginalPoints, car.Center, car.Transform);
        
        int minX = int.MaxValue;
        int maxX = int.MinValue;
        int minY = int.MaxValue;
        int maxY = int.MinValue;

        foreach (var p in transformedPoints)
        {
            // Update AABB
            if (p.X < minX) minX = p.X;
            if (p.X > maxX) maxX = p.X;
            if (p.Y < minY) minY = p.Y;
            if (p.Y > maxY) maxY = p.Y;

            // Filter out of bounds points if necessary, or just allow them but they won't match anything on canvas [0..W, 0..H]
            // We should filter to keep HashSet clean and matching valid canvas keys
            if (p.X >= 0 && p.X < width && p.Y >= 0 && p.Y < height)
            {
                pixels.Add(p.Y * stride + p.X);
            }
        }

        // Handle case if no points (e.g. empty car)
        if (minX > maxX)
        {
            minX = maxX = minY = maxY = 0;
        }

        var box = new Aabb(minX, maxX, minY, maxY);
        _pixelSetCache[car.Id] = (currentHash, pixels, box);
        return (pixels, box);
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
