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
}
