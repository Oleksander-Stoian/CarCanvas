using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CarCanvas.Application.Interfaces;
using CarCanvas.Domain.Entities;

namespace CarCanvas.Infrastructure.Services;

public class CarPointsParser : ICarPointsParser
{
    private const int MaxPoints = 200_000;
    // We can also enforce file size check before stream processing in ViewModel, 
    // but here we enforce points count.

    public async Task<IReadOnlyList<Point2D>> ParseAsync(Stream fileStream, CancellationToken ct = default)
    {
        var uniquePoints = new HashSet<Point2D>();
        using var reader = new StreamReader(fileStream);

        int lineNumber = 0;
        string? line;
        while ((line = await reader.ReadLineAsync(ct)) != null)
        {
            if (ct.IsCancellationRequested) break;

            lineNumber++;

            if (string.IsNullOrWhiteSpace(line)) continue;
            
            var trimmed = line.Trim();
            if (trimmed.StartsWith("#") || trimmed.StartsWith("//")) continue;

            // Handle potential "2->123" format or just "123 456"
            // Replacing "->" with space
            var cleanLine = trimmed.Replace("->", " ");
            
            var parts = cleanLine.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);

            // We expect at least 2 numbers. 
            // Heuristic from previous loader: take last 2 valid integers.
            var numbers = new List<int>();
            foreach (var part in parts)
            {
                if (int.TryParse(part, out int val))
                {
                    numbers.Add(val);
                }
            }

            if (numbers.Count < 2)
            {
                // Skip lines that don't contain enough coordinates (e.g. headers, garbage)
                continue;
            }

            int x = numbers[numbers.Count - 2];
            int y = numbers[numbers.Count - 1];
            
            uniquePoints.Add(new Point2D(x, y));

            if (uniquePoints.Count > MaxPoints)
            {
                throw new InvalidDataException($"Too many points: {uniquePoints.Count} (max {MaxPoints})");
            }
        }

        if (uniquePoints.Count < 3)
        {
            throw new InvalidDataException("Not enough points (minimum 3 required).");
        }

        return uniquePoints.ToList();
    }
}
