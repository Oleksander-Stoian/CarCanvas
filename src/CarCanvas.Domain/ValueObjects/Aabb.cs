using System;

namespace CarCanvas.Domain.ValueObjects;

public readonly struct Aabb
{
    public int MinX { get; }
    public int MaxX { get; }
    public int MinY { get; }
    public int MaxY { get; }

    public Aabb(int minX, int maxX, int minY, int maxY)
    {
        MinX = minX;
        MaxX = maxX;
        MinY = minY;
        MaxY = maxY;
    }

    public bool Intersects(Aabb other)
    {
        return !(MaxX < other.MinX || MinX > other.MaxX || MaxY < other.MinY || MinY > other.MaxY);
    }

    public Aabb Inflate(int padding, int canvasWidth, int canvasHeight)
    {
        int minX = Math.Max(0, MinX - padding);
        int maxX = Math.Min(canvasWidth - 1, MaxX + padding);
        int minY = Math.Max(0, MinY - padding);
        int maxY = Math.Min(canvasHeight - 1, MaxY + padding);
        
        return new Aabb(minX, maxX, minY, maxY);
    }
}
