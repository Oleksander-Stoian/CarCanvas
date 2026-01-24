using System;
using CarCanvas.Domain.Entities;
using CarCanvas.Domain.ValueObjects;

namespace CarCanvas.Infrastructure.Algorithms;

public static class GeometryUtils
{
    public static bool GetClippedSegment(LineSegment line, Aabb box, out LineSegment clipped, int padding = 0)
    {
        clipped = default;

        double xmin = box.MinX - padding;
        double xmax = box.MaxX + padding;
        double ymin = box.MinY - padding;
        double ymax = box.MaxY + padding;

        double x0 = line.Start.X;
        double y0 = line.Start.Y;
        double x1 = line.End.X;
        double y1 = line.End.Y;

        double dx = x1 - x0;
        double dy = y1 - y0;

        double t0 = 0.0;
        double t1 = 1.0;

        if (ClipT(-dx, x0 - xmin, ref t0, ref t1) &&  // Left
            ClipT(dx, xmax - x0, ref t0, ref t1) &&   // Right
            ClipT(-dy, y0 - ymin, ref t0, ref t1) &&  // Top
            ClipT(dy, ymax - y0, ref t0, ref t1))     // Bottom
        {
            // Accepted
            int newX0 = (int)Math.Round(x0 + t0 * dx);
            int newY0 = (int)Math.Round(y0 + t0 * dy);
            int newX1 = (int)Math.Round(x0 + t1 * dx);
            int newY1 = (int)Math.Round(y0 + t1 * dy);

            clipped = new LineSegment(new Point2D(newX0, newY0), new Point2D(newX1, newY1));
            return true;
        }

        return false;
    }

    private static bool ClipT(double p, double q, ref double t0, ref double t1)
    {
        if (Math.Abs(p) < 1e-9) // Parallel line (allowing for small epsilon)
        {
            if (q < 0) return false; // Outside
            return true; // Inside or on boundary
        }

        double r = q / p;
        if (p < 0)
        {
            if (r > t1) return false;
            if (r > t0) t0 = r;
        }
        else // p > 0
        {
            if (r < t0) return false;
            if (r < t1) t1 = r;
        }
        return true;
    }
}
