using System;
using System.Collections.Generic;
using CarCanvas.Domain.Entities;
using CarCanvas.Domain.ValueObjects;

namespace CarCanvas.Application.Algorithms;

public class UniformGridIndex
{
    private readonly int _cellSize;
    private readonly int _cols;
    private readonly int _rows;
    private readonly Dictionary<int, List<int>> _grid; // CellKey -> List of Line Indices

    public UniformGridIndex(int width, int height, int cellSize)
    {
        _cellSize = cellSize;
        _cols = (int)Math.Ceiling((double)width / cellSize);
        _rows = (int)Math.Ceiling((double)height / cellSize);
        _grid = new Dictionary<int, List<int>>();
    }

    public void Clear()
    {
        _grid.Clear();
    }

    public void Add(LineSegment line, int lineIndex)
    {
        // Fix #2: Use Grid Traversal (DDA) instead of AABB scan
        // This reduces complexity from O(Area) to O(Length)
        
        // Handle point-sized segments or segments within same cell quickly
        if (line.Start.X == line.End.X && line.Start.Y == line.End.Y)
        {
             int col = DivFloor(line.Start.X, _cellSize);
             int row = DivFloor(line.Start.Y, _cellSize);
             AddInternal(col, row, lineIndex);
             return;
        }

        var visited = new HashSet<long>(); // To prevent duplicates in edge cases

        foreach (var (col, row) in TraverseCells(line.Start.X, line.Start.Y, line.End.X, line.End.Y))
        {
            // Create a unique key for visited set (assuming grid size won't exceed int limits for col/row)
            // Combine col and row into a long. Since col/row can be negative, just shifting is fine if range is safe.
            // Let's use simple packing: (long)row << 32 | (uint)col
            long cellKey = ((long)row << 32) | (uint)col;
            
            if (!visited.Add(cellKey)) continue;

            AddInternal(col, row, lineIndex);
        }
    }

    private void AddInternal(int col, int row, int lineIndex)
    {
        if (col < 0 || col >= _cols || row < 0 || row >= _rows) return;

        int key = GetKey(col, row);
        if (!_grid.TryGetValue(key, out var list))
        {
            list = new List<int>();
            _grid[key] = list;
        }
        list.Add(lineIndex);
    }

    /// <summary>
    /// Implements Amanatides & Woo 2D Grid Traversal (DDA).
    /// Returns all cells intersected by the line segment from (x0, y0) to (x1, y1).
    /// </summary>
    public IEnumerable<(int col, int row)> TraverseCells(int x0, int y0, int x1, int y1)
    {
        int col = DivFloor(x0, _cellSize);
        int row = DivFloor(y0, _cellSize);
        int endCol = DivFloor(x1, _cellSize);
        int endRow = DivFloor(y1, _cellSize);

        int dx = x1 - x0;
        int dy = y1 - y0;

        // Check for boundary conditions (inclusive boundary requirement)
        bool startXOnBoundary = (x0 % _cellSize == 0);
        bool startYOnBoundary = (y0 % _cellSize == 0);
        bool endXOnBoundary = (x1 % _cellSize == 0);
        bool endYOnBoundary = (y1 % _cellSize == 0);

        // Yield start neighbors if starting on boundary (covers all 4 cells around intersection)
        if (startXOnBoundary) yield return (col - 1, row);
        if (startYOnBoundary) yield return (col, row - 1);
        if (startXOnBoundary && startYOnBoundary) yield return (col - 1, row - 1);

        int stepX = Math.Sign(dx);
        int stepY = Math.Sign(dy);

        double tMaxX, tMaxY;
        double tDeltaX, tDeltaY;

        // Calculate tDelta and initial tMax
        if (dx == 0)
        {
            stepX = 0;
            tMaxX = double.PositiveInfinity;
            tDeltaX = double.PositiveInfinity;
        }
        else
        {
            tDeltaX = (double)_cellSize / Math.Abs(dx);
            if (stepX > 0)
            {
                tMaxX = ((col + 1) * _cellSize - x0) / (double)dx;
            }
            else
            {
                tMaxX = (col * _cellSize - x0) / (double)dx;
            }
        }

        if (dy == 0)
        {
            stepY = 0;
            tMaxY = double.PositiveInfinity;
            tDeltaY = double.PositiveInfinity;
        }
        else
        {
            tDeltaY = (double)_cellSize / Math.Abs(dy);
            if (stepY > 0)
            {
                tMaxY = ((row + 1) * _cellSize - y0) / (double)dy;
            }
            else
            {
                tMaxY = (row * _cellSize - y0) / (double)dy;
            }
        }
        
        // Always yield the starting cell
        yield return (col, row);
        
        // Special case: Parallel to boundary
        if (dx == 0 && startXOnBoundary) yield return (col - 1, row);
        if (dy == 0 && startYOnBoundary) yield return (col, row - 1);

        // If start and end are same cell, we are done (but checked start extras above)
        // Wait, if dx=0, dy=0 (point). 
        // Logic handles it (dx=0, dy=0 -> step=0. tMax=Inf).
        // Loop runs 0 times or 1?
        // col==endCol, row==endRow. yield break immediately.
        // Start extras yielded.
        // Parallel checks yielded.
        if (col == endCol && row == endRow) 
        {
             // Check End Extras before leaving (since we skip the bottom of the function)
             if (endXOnBoundary) yield return (endCol - 1, endRow);
             if (endYOnBoundary) yield return (endCol, endRow - 1);
             if (endXOnBoundary && endYOnBoundary) yield return (endCol - 1, endRow - 1);
             yield break;
        }

        // DDA Loop
        // We limit iterations to avoid infinite loops in case of numerical instability, 
        // though logically bounded by Manhattan distance.
        int maxSteps = Math.Abs(endCol - col) + Math.Abs(endRow - row) + 2; 

        for (int i = 0; i < maxSteps; i++)
        {
            double diff = Math.Abs(tMaxX - tMaxY);
            if (diff < 1e-12)
            {
                // Corner crossing: advance both to cover the diagonal step
                tMaxX += tDeltaX;
                col += stepX;

                tMaxY += tDeltaY;
                row += stepY;

                // When crossing a corner exactly, we touch all 4 cells sharing that corner.
                // We are moving from (c, r) to (c+sx, r+sy).
                // We must also include the side neighbors: (c+sx, r) and (c, r+sy).
                // Note: col and row are already updated to the new diagonal cell.
                yield return (col, row - stepY); // (c+sx, r)
                yield return (col - stepX, row); // (c, r+sy)
            }
            else if (tMaxX < tMaxY)
            {
                tMaxX += tDeltaX;
                col += stepX;
            }
            else
            {
                tMaxY += tDeltaY;
                row += stepY;
            }

            yield return (col, row);
            
            // Parallel to boundary check inside loop (for long parallel lines)
            if (dx == 0 && startXOnBoundary) yield return (col - 1, row);
            if (dy == 0 && startYOnBoundary) yield return (col, row - 1);

            if (col == endCol && row == endRow) break;
        }
        
        // Yield end neighbors if ending on boundary
        if (endXOnBoundary) yield return (endCol - 1, endRow);
        if (endYOnBoundary) yield return (endCol, endRow - 1);
        if (endXOnBoundary && endYOnBoundary) yield return (endCol - 1, endRow - 1);
    }

    public HashSet<int> GetCandidates(Aabb area)
    {
        var candidates = new HashSet<int>();

        int startCol = DivFloor(area.MinX, _cellSize);
        int endCol = DivFloor(area.MaxX, _cellSize);
        int startRow = DivFloor(area.MinY, _cellSize);
        int endRow = DivFloor(area.MaxY, _cellSize);

        for (int row = startRow; row <= endRow; row++)
        {
            for (int col = startCol; col <= endCol; col++)
            {
                 if (col < 0 || col >= _cols || row < 0 || row >= _rows) continue;

                 int key = GetKey(col, row);
                 if (_grid.TryGetValue(key, out var list))
                 {
                     foreach (var id in list)
                     {
                         candidates.Add(id);
                     }
                 }
            }
        }
        return candidates;
    }

    private int GetKey(int col, int row)
    {
        return row * _cols + col;
    }

    public static int DivFloor(int x, int d)
    {
        if (x >= 0) return x / d;
        return (x - d + 1) / d;
    }
}
