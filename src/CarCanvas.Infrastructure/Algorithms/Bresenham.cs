using System;
using System.Collections.Generic;
using CarCanvas.Domain.Entities;

namespace CarCanvas.Infrastructure.Algorithms;

public static class Bresenham
{
    public static IEnumerable<Point2D> GetPointsOnLine(Point2D p1, Point2D p2)
    {
        int x0 = p1.X;
        int y0 = p1.Y;
        int x1 = p2.X;
        int y1 = p2.Y;

        int dx = Math.Abs(x1 - x0);
        int dy = Math.Abs(y1 - y0);
        int sx = x0 < x1 ? 1 : -1;
        int sy = y0 < y1 ? 1 : -1;
        int err = dx - dy;

        while (true)
        {
            yield return new Point2D(x0, y0);

            if (x0 == x1 && y0 == y1) break;

            int e2 = 2 * err;
            if (e2 > -dy)
            {
                err -= dy;
                x0 += sx;
            }
            if (e2 < dx)
            {
                err += dx;
                y0 += sy;
            }
        }
    }

    /// <summary>
    /// Non-allocating version of Bresenham's algorithm for checking intersections against a PixelSet.
    /// </summary>
    /// <param name="p1">Start point</param>
    /// <param name="p2">End point</param>
    /// <param name="pixels">Target PixelSet</param>
    /// <param name="strideKey">Stride key for coordinate packing</param>
    /// <param name="canvasWidth">Canvas Width for boundary checks</param>
    /// <param name="canvasHeight">Canvas Height for boundary checks</param>
    /// <param name="maxHitsToCollect">Max markers to collect (if collecting)</param>
    /// <param name="markers">List to add markers to (can be null if not collecting)</param>
    /// <param name="stopEarlyAt">Stop if total hits reaches this number (FastMode)</param>
    /// <param name="currentTotalHits">Current total hits count (for early stop)</param>
    /// <returns>Number of hits found on this line</returns>
    public static int CountHitsOnLine(
        Point2D p1, 
        Point2D p2, 
        HashSet<int> pixels, 
        int strideKey, 
        int canvasWidth, 
        int canvasHeight,
        int maxHitsToCollect,
        List<Point2D>? markers,
        int stopEarlyAt = -1,
        int currentTotalHits = 0)
    {
        int x0 = p1.X;
        int y0 = p1.Y;
        int x1 = p2.X;
        int y1 = p2.Y;

        int dx = Math.Abs(x1 - x0);
        int dy = Math.Abs(y1 - y0);
        int sx = x0 < x1 ? 1 : -1;
        int sy = y0 < y1 ? 1 : -1;
        int err = dx - dy;

        int hits = 0;
        bool checkBounds = true; // Optimization: we could skip if we know line is clipped inside, but safe default is true.

        while (true)
        {
            // Check Point
            if (checkBounds)
            {
                // Simple bounds check
                if (x0 >= 0 && x0 < canvasWidth && y0 >= 0 && y0 < canvasHeight)
                {
                    int key = y0 * strideKey + x0;
                    if (pixels.Contains(key))
                    {
                        hits++;
                        
                        // Add marker if needed
                        if (markers != null && markers.Count < maxHitsToCollect)
                        {
                            markers.Add(new Point2D(x0, y0));
                        }

                        // Early Stop Check
                        if (stopEarlyAt > 0 && (currentTotalHits + hits) >= stopEarlyAt)
                        {
                            return hits;
                        }
                    }
                }
            }

            if (x0 == x1 && y0 == y1) break;

            int e2 = 2 * err;
            if (e2 > -dy)
            {
                err -= dy;
                x0 += sx;
            }
            if (e2 < dx)
            {
                err += dx;
                y0 += sy;
            }
        }
        
        return hits;
    }
}
