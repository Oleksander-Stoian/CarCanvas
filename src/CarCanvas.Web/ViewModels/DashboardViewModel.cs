using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CarCanvas.Application;
using CarCanvas.Application.Algorithms;
using CarCanvas.Application.DTOs;
using CarCanvas.Application.Interfaces;
using CarCanvas.Domain.Entities;
using CarCanvas.Domain.ValueObjects;

namespace CarCanvas.Web.ViewModels;

public class DashboardViewModel
{
    private readonly ICarLoader _carLoader;
    private readonly ICarPointsParser _pointsParser;
    private readonly IIntersectionService _intersectionService;
    private readonly ICanvasSceneService _canvasService;
    private readonly AppOptions _options;
    private readonly UniformGridIndex _gridIndex;

    public event Action? OnChange;

    public CarModel? Car1 { get; private set; }
    public CarModel? Car2 { get; private set; }
    public List<LineSegment> Lines { get; private set; } = new();

    public IntersectionResult? ResultCar1 { get; private set; }
    public IntersectionResult? ResultCar2 { get; private set; }

    public bool IsLoading { get; private set; }
    public string? ErrorMessage { get; private set; }
    public int? LoadedPointsCount { get; private set; }
    public string? LoadedSourceName { get; private set; }
    
    public bool ShowDebug { get; set; } = false;

    public DashboardViewModel(
        ICarLoader carLoader, 
        ICarPointsParser pointsParser,
        IIntersectionService intersectionService,
        ICanvasSceneService canvasService,
        AppOptions options)
    {
        _carLoader = carLoader;
        _pointsParser = pointsParser;
        _intersectionService = intersectionService;
        _canvasService = canvasService;
        _options = options;
        _gridIndex = new UniformGridIndex(options.CanvasWidth, options.CanvasHeight, 100); // CellSize = 100
    }

    public void SetError(string message)
    {
        ErrorMessage = message;
        NotifyStateChanged();
    }

    public async Task InitializeAsync()
    {
        try
        {
            await LoadSampleCarAsync();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to initialize: {ex.Message}";
        }
    }

    public async Task LoadSampleCarAsync()
    {
        try
        {
            IsLoading = true;
            ErrorMessage = null;
            NotifyStateChanged();

            var points = await _carLoader.LoadPointsAsync("Logan.txt");
            var pointsList = points.ToList();

            InitializeCars(pointsList, "sample (Logan.txt)");
            
            await DrawSceneAsync();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to load sample: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
            NotifyStateChanged();
        }
    }

    public async Task LoadCarFromFileAsync(Stream fileStream, string fileName)
    {
        try
        {
            IsLoading = true;
            ErrorMessage = null;
            NotifyStateChanged();

            var points = await _pointsParser.ParseAsync(fileStream);
            InitializeCars(points, fileName);

            await DrawSceneAsync();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to load file: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
            NotifyStateChanged();
        }
    }

    private void InitializeCars(IReadOnlyList<Point2D> points, string sourceName)
    {
        LoadedPointsCount = points.Count;
        LoadedSourceName = sourceName;

        Car1 = new CarModel(1, points);
        Car2 = new CarModel(2, points);

        // Reset transforms
        // Car 1 defaults
        Car1.Transform.TranslateX = 0;
        Car1.Transform.TranslateY = 0;
        Car1.Transform.RotationAngle = 0;

        // Car 2 defaults (offset)
        Car2.Transform.TranslateX = 410; // Or 900 as before, but user mentioned 410 or default
        Car2.Transform.TranslateY = 0;
        Car2.Transform.RotationAngle = 0;

        // Clear intersection results
        ResultCar1 = null;
        ResultCar2 = null;
    }

    public async Task UpdateTransformAsync(int carId, int tx, int ty, double rot)
    {
        var car = carId == 1 ? Car1 : Car2;
        if (car != null)
        {
            car.Transform.TranslateX = tx;
            car.Transform.TranslateY = ty;
            car.Transform.RotationAngle = rot;
            await DrawSceneAsync();
        }
    }

    public async Task AddLineAsync(int x1, int y1, int x2, int y2)
    {
        var line = new LineSegment(new Point2D(x1, y1), new Point2D(x2, y2));
        Lines.Add(line);
        _gridIndex.Add(line, Lines.Count - 1);
        await _canvasService.DrawLineAsync(line);
        NotifyStateChanged();
    }

    public async Task GenerateRandomLinesAsync(int count)
    {
        if (count <= 0) return;
        if (count > _options.MaxRandomLines) count = _options.MaxRandomLines; // Safety cap

        var rnd = new Random();
        var newLines = new List<LineSegment>(count);
        int startIndex = Lines.Count;

        for (int i = 0; i < count; i++)
        {
            var p1 = new Point2D(rnd.Next(0, _options.CanvasWidth), rnd.Next(0, _options.CanvasHeight));
            var p2 = new Point2D(rnd.Next(0, _options.CanvasWidth), rnd.Next(0, _options.CanvasHeight));
            var line = new LineSegment(p1, p2);
            newLines.Add(line);
            _gridIndex.Add(line, startIndex + i);
        }

        Lines.AddRange(newLines);
        // Optimize: draw only new lines?
        // But for simplicity and layering, we might redraw everything or just new lines.
        // Let's draw batch.
        await _canvasService.DrawLinesAsync(newLines);
        NotifyStateChanged();
    }

    public async Task ClearLinesAsync()
    {
        Lines.Clear();
        _gridIndex.Clear();
        ResultCar1 = null;
        ResultCar2 = null;
        await DrawSceneAsync();
    }

    public async Task FindIntersectionsAsync(int carId)
    {
        if (Car1 == null || Car2 == null) return;

        IsLoading = true;
        NotifyStateChanged();
        
        // Wait a bit to let UI render loading state
        await Task.Delay(10);

        var target = carId == 1 ? Car1 : Car2;
        var other = carId == 1 ? Car2 : Car1;

        var result = await _intersectionService.FindIntersectionsAsync(target, other, Lines, _options, _gridIndex);

        if (carId == 1) ResultCar1 = result;
        else ResultCar2 = result;

        // Draw markers
        // Clear previous markers? Usually "Clear scene" clears markers. 
        // But "Find Intersection" might want to clear previous markers first.
        // Let's redraw scene + markers.
        await DrawSceneAsync();
        
        // Draw new markers
        await _canvasService.DrawMarkersAsync(result.MarkersToDraw);

        IsLoading = false;
        NotifyStateChanged();
    }

    private async Task DrawSceneAsync()
    {
        await _canvasService.ClearAsync();
        
        // Draw Lines
        if (Lines.Any())
        {
            await _canvasService.DrawLinesAsync(Lines);
        }

        // Draw Cars
        if (Car1 != null)
        {
            var points1 = CarCanvas.Infrastructure.Algorithms.PointTransformer.TransformPoints(Car1.OriginalPoints, Car1.Center, Car1.Transform);
            await _canvasService.DrawCarAsync(points1, "blue");
        }

        if (Car2 != null)
        {
            var points2 = CarCanvas.Infrastructure.Algorithms.PointTransformer.TransformPoints(Car2.OriginalPoints, Car2.Center, Car2.Transform);
            await _canvasService.DrawCarAsync(points2, "green");
        }
        
        // Draw existing results markers if any?
        // If we want to persist markers until clear.
        if (ResultCar1 != null) await _canvasService.DrawMarkersAsync(ResultCar1.MarkersToDraw);
        if (ResultCar2 != null) await _canvasService.DrawMarkersAsync(ResultCar2.MarkersToDraw);

        if (ShowDebug)
        {
            if (ResultCar1?.TargetCarAabb.HasValue == true)
            {
                var box = ResultCar1.TargetCarAabb.Value.Inflate(2, _options.CanvasWidth, _options.CanvasHeight);
                await _canvasService.DrawRectAsync(box.MinX, box.MinY, box.MaxX - box.MinX, box.MaxY - box.MinY, "cyan");
            }
            if (ResultCar2?.TargetCarAabb.HasValue == true)
            {
                var box = ResultCar2.TargetCarAabb.Value.Inflate(2, _options.CanvasWidth, _options.CanvasHeight);
                await _canvasService.DrawRectAsync(box.MinX, box.MinY, box.MaxX - box.MinX, box.MaxY - box.MinY, "lime");
            }
        }
    }

    public async Task ToggleDebugAsync(bool isDebug)
    {
        ShowDebug = isDebug;
        await DrawSceneAsync();
        NotifyStateChanged();
    }

    private void NotifyStateChanged() => OnChange?.Invoke();
}
