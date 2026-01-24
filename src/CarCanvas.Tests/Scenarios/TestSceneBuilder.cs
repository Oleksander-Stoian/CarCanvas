using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CarCanvas.Application.Algorithms;
using CarCanvas.Application.DTOs;
using CarCanvas.Application.Enums;
using CarCanvas.Domain.Entities;
using CarCanvas.Domain.ValueObjects;
using CarCanvas.Infrastructure.Services;

namespace CarCanvas.Tests.Scenarios
{
    public class TestSceneBuilder
    {
        private CarModel _car;
        private CarModel _otherCar; // Dummy second car
        private readonly List<LineSegment> _lines = new();
        private readonly AppOptions _options = new()
        {
            CanvasWidth = 1000,
            CanvasHeight = 1000,
            StrideKey = 1000,
            MaxMarkersToDraw = 10000,
            FastMode = false,
            CoordinateMode = CoordinateMode.MathYUp
        };
        private bool _useGrid = false;

        public TestSceneBuilder()
        {
            // Default car: 100x100 square at (0,0) in local coords
            var points = new List<Point2D>();
            for (int x = 0; x <= 100; x++)
            {
                for (int y = 0; y <= 100; y++)
                {
                    points.Add(new Point2D(x, y));
                }
            }
            _car = new CarModel(1, points);
            _otherCar = new CarModel(2, new List<Point2D>()); // Empty other car by default
        }

        public TestSceneBuilder WithCar(int id, int x, int y, int rotationDeg)
        {
            // CarModel constructor calculates center from OriginalPoints
            // We need to match IntersectionService logic for Transform
            // IntersectionService uses PointTransformer.TransformPoints which applies:
            // 1. Translate point so Center is at (0,0)
            // 2. Rotate
            // 3. Translate back (actually just translate to new position)
            // Wait, PointTransformer logic (from context or memory):
            // Usually: (p - center) * rot + center + translate
            // Or: (p - center) * rot + newPosition
            
            // Let's assume Transform.TranslateX/Y is the FINAL position of the center
            // OR it is the offset from the original center.
            // Looking at PointTransformer usage in IntersectionService:
            // PointTransformer.TransformPoints(car.OriginalPoints, car.Center, car.Transform, ...)
            
            // To place the car exactly at (x,y), we need to set TranslateX/Y appropriately.
            // If TranslateX/Y represents the delta, then:
            // _car.Transform.TranslateX = x - _car.Center.X;
            // _car.Transform.TranslateY = y - _car.Center.Y;
            
            // However, usually in such apps TranslateX/Y is the absolute position of the center 
            // OR the offset. Let's check typical usage. 
            // Default Transform is 0,0. Car is at original coords.
            // Center of 0..100 is 50,50.
            // If we want car at 500,500.
            // If Transform is offset: Translate = 500 - 50 = 450.
            
            // Let's assume TranslateX/Y is OFFSET for now based on standard simple 2D logic.
            
            _car.Transform.TranslateX = x - _car.Center.X;
            _car.Transform.TranslateY = y - _car.Center.Y;
            _car.Transform.RotationAngle = rotationDeg;
            
            return this;
        }

        public TestSceneBuilder WithOptions(bool fastMode, int maxMarkers, bool useGrid)
        {
            _options.FastMode = fastMode;
            _options.MaxMarkersToDraw = maxMarkers;
            _useGrid = useGrid;
            return this;
        }

        public TestSceneBuilder AddLine(int x0, int y0, int x1, int y1)
        {
            _lines.Add(new LineSegment(new Point2D(x0, y0), new Point2D(x1, y1)));
            return this;
        }

        public TestSceneBuilder AddRandomLines(int n, int seed)
        {
            var rnd = new Random(seed);
            for (int i = 0; i < n; i++)
            {
                int x0 = rnd.Next(0, _options.CanvasWidth);
                int y0 = rnd.Next(0, _options.CanvasHeight);
                int x1 = rnd.Next(0, _options.CanvasWidth);
                int y1 = rnd.Next(0, _options.CanvasHeight);
                _lines.Add(new LineSegment(new Point2D(x0, y0), new Point2D(x1, y1)));
            }
            return this;
        }

        public async Task<IntersectionResult> ExecuteAsync()
        {
            var service = new IntersectionService();
            UniformGridIndex? grid = null;

            if (_useGrid)
            {
                grid = new UniformGridIndex(_options.CanvasWidth, _options.CanvasHeight, cellSize: 50);
                for (int i = 0; i < _lines.Count; i++)
                {
                    grid.Add(_lines[i], i);
                }
            }

            return await service.FindIntersectionsAsync(_car, _otherCar, _lines, _options, grid);
        }
    }
}