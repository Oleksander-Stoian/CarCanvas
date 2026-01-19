using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using CarCanvas.Application.Interfaces;
using CarCanvas.Domain.Entities;

namespace CarCanvas.Infrastructure.Services;

public class CarFileLoader : ICarLoader
{
    private readonly HttpClient _httpClient;

    public CarFileLoader(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<IEnumerable<Point2D>> LoadPointsAsync(string source)
    {
        var content = await _httpClient.GetStringAsync(source);
        var lines = content.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        var points = new List<Point2D>();

        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            if (string.IsNullOrWhiteSpace(trimmed)) continue;

            // Handle optional prefix like "2->123 456" or just "123 456"
            // The prompt says "possible prefix type 2->123"
            // We need to parse robustly.
            // Let's replace '->' with space and then split by space/tab
            
            var cleanLine = trimmed.Replace("->", " ");
            var parts = cleanLine.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);

            // We expect at least 2 numbers for X and Y.
            // If prefix exists "2 123", it might be index? Or just garbage.
            // Prompt says "2->123". "2" might be line number or count?
            // "Format: lines with coordinates X/Y".
            // Let's look for the last two integers or first two?
            // "Ignore header".
            
            // Heuristic: try to parse all parts as integers. Take the last two valid ones?
            // Or first two?
            // Let's try to parse first two.
            
            var numbers = new List<int>();
            foreach (var part in parts)
            {
                if (int.TryParse(part, out int val))
                {
                    numbers.Add(val);
                }
            }

            if (numbers.Count >= 2)
            {
                // Assuming X Y are the last two numbers if there's a prefix
                // Or X Y are the ONLY numbers.
                // If "2->123", parts are "2", "123". That's only 2 numbers. Is it X=2 Y=123?
                // Probably not. "2->123" sounds like a mapping.
                // Let's assume standard format "X Y" or "Idx X Y".
                // If 3 numbers, take last 2.
                // If 2 numbers, take them.
                
                int x = numbers[numbers.Count - 2];
                int y = numbers[numbers.Count - 1];
                points.Add(new Point2D(x, y));
            }
        }

        return points;
    }
}
