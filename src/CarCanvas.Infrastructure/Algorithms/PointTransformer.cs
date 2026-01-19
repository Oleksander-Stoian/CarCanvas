using System;
using System.Collections.Generic;
using System.Linq;
using CarCanvas.Domain.Entities;
using CarCanvas.Domain.ValueObjects;

namespace CarCanvas.Infrastructure.Algorithms;

public static class PointTransformer
{
    public static IEnumerable<Point2D> TransformPoints(IEnumerable<Point2D> points, Point2D center, Transform transform)
    {
        double rad = transform.RotationAngle * Math.PI / 180.0;
        double cos = Math.Cos(rad);
        double sin = Math.Sin(rad);
        int tx = transform.TranslateX;
        int ty = transform.TranslateY;
        int cx = center.X;
        int cy = center.Y;

        foreach (var p in points)
        {
            // Translate to center (0,0) relative to rotation center
            double x = p.X - cx;
            double y = p.Y - cy;

            // Rotate
            double xRot = x * cos - y * sin;
            double yRot = x * sin + y * cos;

            // 3 & 4. Translate back (cx, cy) and apply offset (tx, ty)
            int xFinal = (int)Math.Round(xRot + cx + tx, MidpointRounding.AwayFromZero);
            int yFinal = (int)Math.Round(yRot + cy + ty, MidpointRounding.AwayFromZero);

            yield return new Point2D(xFinal, yFinal);
        }
    }
}
