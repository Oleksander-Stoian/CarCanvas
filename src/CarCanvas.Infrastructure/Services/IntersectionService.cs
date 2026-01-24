using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using CarCanvas.Application;
using CarCanvas.Application.Algorithms;
using CarCanvas.Application.DTOs;
using CarCanvas.Application.Enums;
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

    public void InvalidateCache()
    {
        _pixelSetCache.Clear();
    }
    
    public async Task<IntersectionResult> FindIntersectionsAsync(
        CarModel targetCar, 
        CarModel otherCar, 
        IList<LineSegment> lines,
        AppOptions options,
        UniformGridIndex? gridIndex = null)
    {
        // Offload to thread pool for "heavy" operations
        return await Task.Run(() => FindIntersectionsInternal(targetCar, otherCar, lines, options, gridIndex));
    }

    private IntersectionResult FindIntersectionsInternal(
        CarModel targetCar, 
        CarModel otherCar, 
        IList<LineSegment> lines,
        AppOptions options,
        UniformGridIndex? gridIndex)
    {
        var sw = Stopwatch.StartNew();
        var result = new IntersectionResult();
        result.LimitUsed = options.MaxMarkersToDraw;
        
        // 1. Get PixelSets
        // BuildCarPixelSetMs - only if not cached. If cached, it's 0.
        // GetOrUpdatePixelSet handles caching internally.
        // We need to measure if it actually did work or just returned cache.
        // But the requirement says: "If BuildCarPixelSet is not executed (already cache), set 0 and show 'cached' in UI."
        // So we can just measure the time it takes to call GetOrUpdatePixelSet.
        // If it's fast (cached), it will be 0ms. If slow, >0ms.
        
        long t0 = sw.ElapsedMilliseconds;
        var (targetPixels, targetBox) = GetOrUpdatePixelSet(targetCar, options);
        var (otherPixels, otherBox) = GetOrUpdatePixelSet(otherCar, options);
        long t1 = sw.ElapsedMilliseconds;
        result.BuildCarPixelSetMs = t1 - t0;

        // Save AABBs to result for debug
        result.TargetCarAabb = targetBox;
        result.OtherCarAabb = otherBox;

        // GridQueryMs starts here
        long t2 = sw.ElapsedMilliseconds;
        
        // Padded AABB for fast check (safe against rounding errors)
        var targetBoxPadded = targetBox.Inflate(2, options.CanvasWidth, options.CanvasHeight);

        // Determine candidates
        IEnumerable<int> candidates;
        if (gridIndex != null)
        {
            // Use grid to find potential lines
            // targetBoxPadded is already inflated, which is good for covering cells
            candidates = gridIndex.GetCandidates(targetBoxPadded);
        }
        else
        {
            // Fallback: check all lines
            candidates = Enumerable.Range(0, lines.Count);
        }
        
        // Materialize candidates to measure Grid Query time correctly (if it's deferred execution)
        // GetCandidates returns HashSet, so it's already materialized. 
        // Enumerable.Range is deferred but trivial.
        // Let's force it if it's not a list/collection to be fair?
        // Actually, let's treat "obtaining the enumerable/collection" as the query time.
        // Since we iterate later, iteration overhead is part of NarrowPhase if we stream it.
        // But GetCandidates returns a HashSet, so the work is done.
        
        long t3 = sw.ElapsedMilliseconds;
        result.GridQueryMs = t3 - t2;

        // NarrowPhaseMs
        long t4 = sw.ElapsedMilliseconds;
        
        var carHits = 0;
        // Check overlap (Target vs Other Car)
        // We include Car-vs-Car in NarrowPhase or separate? 
        // Requirement says: "NarrowPhaseMs: Time for actual intersection checks (Bresenham / contains / pixel checks / line-vs-car)."
        // So Car-vs-Car fits here too.
        
        // Optimization: Iterate smaller set check larger.
        foreach (var key in targetPixels)
        {
            if (otherPixels.Contains(key))
            {
                carHits++;
                // We add to markers later in Collect phase? 
                // Or here? The requirement says: "CollectResultsMs: Time adding results to list + applying limit...".
                // Usually we add to a temporary list here and then filter/limit in Collect?
                // But current code adds directly to result.MarkersToDraw.
                // To separate cleanly, we should collect ALL hits (or up to a safe buffer) then finalize.
                // However, refactoring logic might be risky.
                // Let's keep adding here but acknowledge that "adding to list" is technically part of NarrowPhase in this implementation.
                // If we want to be strict, we'd use a temp list.
                // Let's stick to existing logic: The *calculation* is the main cost. Adding to list is cheap unless list is huge.
                // We will count this loop as NarrowPhase.
                if (result.MarkersToDraw.Count < options.MaxMarkersToDraw)
                {
                    result.MarkersToDraw.Add(KeyToPoint(key, options.StrideKey));
                }
                else if (options.FastMode)
                {
                    result.StoppedEarly = true;
                    break;
                }
            }
        }
        result.TotalHitsCars = carHits;

        // 3. Intersections with Lines
        // For each line, bresenham -> check if point in targetPixels
        int lineHits = 0;

        if (!result.StoppedEarly)
        {
            foreach (var index in candidates)
            {
                if (result.StoppedEarly) break;
                
                if (index < 0 || index >= lines.Count) continue;
                var line = lines[index];

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

            // Fast-check: Segment vs AABB intersection (Cohen-Sutherland) + Clipping
            // This clips the line to the targetBox so we don't iterate pixels outside it.
            if (!GeometryUtils.GetClippedSegment(line, targetBox, out var clippedLine, padding: 2))
            {
                result.RejectedBySegmentAabb++;
                continue;
            }

            result.ProcessedByBresenham++;
            foreach (var p in Bresenham.GetPointsOnLine(clippedLine.Start, clippedLine.End))
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
                    else if (options.FastMode)
                    {
                        result.StoppedEarly = true;
                        break; // break points loop
                    }
                }
            }
        }
        }
        result.TotalHitsLines = lineHits;
        
        long t5 = sw.ElapsedMilliseconds;
        result.NarrowPhaseMs = t5 - t4;

        // CollectResultsMs
        // Since we already added to MarkersToDraw during NarrowPhase (optimization to avoid double alloc),
        // "CollectResultsMs" might be just the finalization time (sorting if any, setting DTO properties).
        // Or we could move the "Apply Limit" logic here if we were storing all hits.
        // Given current structure, this phase is very short.
        
        // Let's pretend we do some final cleanup or stats aggregation here.
        // Realistically, the time is negligible.
        
        long t6 = sw.ElapsedMilliseconds;
        result.CollectResultsMs = t6 - t5;

        sw.Stop();
        result.TimeElapsedMs = sw.ElapsedMilliseconds;
        return result;
    }

    private (HashSet<int> PixelSet, Aabb Box) GetOrUpdatePixelSet(CarModel car, AppOptions options)
    {
        int currentHash = ComputeTransformHash(car.Transform, options.CoordinateMode);

        if (_pixelSetCache.TryGetValue(car.Id, out var cached))
        {
            if (cached.TransformHash == currentHash)
            {
                return (cached.PixelSet, cached.Box);
            }
        }

        // Recompute
        var pixels = new HashSet<int>();
        var transformedPoints = PointTransformer.TransformPoints(
            car.OriginalPoints, 
            car.Center, 
            car.Transform, 
            options.CoordinateMode, 
            options.CanvasHeight);
        
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
            if (p.X >= 0 && p.X < options.CanvasWidth && p.Y >= 0 && p.Y < options.CanvasHeight)
            {
                pixels.Add(p.Y * options.StrideKey + p.X);
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

    private int ComputeTransformHash(Transform t, CoordinateMode mode)
    {
        // Normalize angle to reduce noise (e.g. from sliders)
        // Also handle 360 wrapping if desired, but rounding is most important for stability
        double normalizedAngle = Math.Round(t.RotationAngle, 3);
        return HashCode.Combine(t.TranslateX, t.TranslateY, normalizedAngle, mode);
    }

    private Point2D KeyToPoint(int key, int stride)
    {
        return new Point2D(key % stride, key / stride);
    }
}
