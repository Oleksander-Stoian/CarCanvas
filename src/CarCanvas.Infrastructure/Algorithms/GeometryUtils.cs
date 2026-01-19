using System;
using CarCanvas.Domain.Entities;
using CarCanvas.Domain.ValueObjects;

namespace CarCanvas.Infrastructure.Algorithms;

public static class GeometryUtils
{
    private const int INSIDE = 0; // 0000
    private const int LEFT = 1;   // 0001
    private const int RIGHT = 2;  // 0010
    private const int BOTTOM = 4; // 0100
    private const int TOP = 8;    // 1000

    public static bool SegmentIntersectsAabb(LineSegment line, Aabb box, int padding = 0)
    {
        int xmin = box.MinX - padding;
        int xmax = box.MaxX + padding;
        int ymin = box.MinY - padding;
        int ymax = box.MaxY + padding;

        int x0 = line.Start.X;
        int y0 = line.Start.Y;
        int x1 = line.End.X;
        int y1 = line.End.Y;

        int outcode0 = ComputeOutCode(x0, y0, xmin, xmax, ymin, ymax);
        int outcode1 = ComputeOutCode(x1, y1, xmin, xmax, ymin, ymax);

        bool accept = false;

        while (true)
        {
            if ((outcode0 | outcode1) == 0)
            {
                // Both points inside
                accept = true;
                break;
            }
            else if ((outcode0 & outcode1) != 0)
            {
                // Both are outside the same zone (e.g. both left of rect)
                break;
            }
            else
            {
                // Some segment is inside
                // Pick an outside point
                int outcodeOut = outcode0 != 0 ? outcode0 : outcode1;
                double x = 0, y = 0;

                // Find intersection point
                if ((outcodeOut & TOP) != 0)
                {
                    x = x0 + (x1 - x0) * (ymax - y0) / (double)(y1 - y0);
                    y = ymax;
                }
                else if ((outcodeOut & BOTTOM) != 0)
                {
                    x = x0 + (x1 - x0) * (ymin - y0) / (double)(y1 - y0);
                    y = ymin;
                }
                else if ((outcodeOut & RIGHT) != 0)
                {
                    y = y0 + (y1 - y0) * (xmax - x0) / (double)(x1 - x0);
                    x = xmax;
                }
                else if ((outcodeOut & LEFT) != 0)
                {
                    y = y0 + (y1 - y0) * (xmin - x0) / (double)(x1 - x0);
                    x = xmin;
                }

                // Move outside point to intersection point
                if (outcodeOut == outcode0)
                {
                    x0 = (int)x;
                    y0 = (int)y;
                    outcode0 = ComputeOutCode(x0, y0, xmin, xmax, ymin, ymax);
                }
                else
                {
                    x1 = (int)x;
                    y1 = (int)y;
                    outcode1 = ComputeOutCode(x1, y1, xmin, xmax, ymin, ymax);
                }
            }
        }

        return accept;
    }

    private static int ComputeOutCode(int x, int y, int xmin, int xmax, int ymin, int ymax)
    {
        int code = INSIDE;

        if (x < xmin) code |= LEFT;
        else if (x > xmax) code |= RIGHT;

        if (y < ymin) code |= BOTTOM;
        else if (y > ymax) code |= TOP;

        return code;
    }
}
