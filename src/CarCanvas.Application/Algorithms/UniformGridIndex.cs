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
        // Compute Line AABB
        int minX = Math.Min(line.Start.X, line.End.X);
        int maxX = Math.Max(line.Start.X, line.End.X);
        int minY = Math.Min(line.Start.Y, line.End.Y);
        int maxY = Math.Max(line.Start.Y, line.End.Y);

        // Determine cell range
        int startCol = minX / _cellSize;
        int endCol = maxX / _cellSize;
        int startRow = minY / _cellSize;
        int endRow = maxY / _cellSize;

        // Add to cells
        for (int row = startRow; row <= endRow; row++)
        {
            for (int col = startCol; col <= endCol; col++)
            {
                if (col < 0 || col >= _cols || row < 0 || row >= _rows) continue;

                int key = GetKey(col, row);
                if (!_grid.TryGetValue(key, out var list))
                {
                    list = new List<int>();
                    _grid[key] = list;
                }
                list.Add(lineIndex);
            }
        }
    }

    public HashSet<int> GetCandidates(Aabb area)
    {
        var candidates = new HashSet<int>();

        int startCol = area.MinX / _cellSize;
        int endCol = area.MaxX / _cellSize;
        int startRow = area.MinY / _cellSize;
        int endRow = area.MaxY / _cellSize;

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
}
