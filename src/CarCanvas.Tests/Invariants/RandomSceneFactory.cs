using System;
using System.Collections.Generic;
using CarCanvas.Application.DTOs;
using CarCanvas.Application.Enums;
using CarCanvas.Domain.Entities;
using CarCanvas.Domain.ValueObjects;

namespace CarCanvas.Tests.Invariants
{
    public record SceneData(
        CarModel Car, 
        CarModel OtherCar, 
        List<LineSegment> Lines, 
        AppOptions Options
    );

    public class RandomSceneFactory
    {
        public static SceneData Generate(int seed, int linesCount)
        {
            var rnd = new Random(seed);
            var options = new AppOptions
            {
                CanvasWidth = 1000,
                CanvasHeight = 1000,
                StrideKey = 1000,
                MaxMarkersToDraw = 10000,
                FastMode = false, // Default
                CoordinateMode = CoordinateMode.MathYUp
            };

            // Create Car 1
            var car1 = CreateCar(1, rnd, options);
            
            // Create Car 2 (Dummy or active)
            var car2 = CreateCar(2, rnd, options);
            
            var lines = new List<LineSegment>();
            for (int i = 0; i < linesCount; i++)
            {
                int x0 = rnd.Next(0, options.CanvasWidth);
                int y0 = rnd.Next(0, options.CanvasHeight);
                int x1 = rnd.Next(0, options.CanvasWidth);
                int y1 = rnd.Next(0, options.CanvasHeight);
                lines.Add(new LineSegment(new Point2D(x0, y0), new Point2D(x1, y1)));
            }

            return new SceneData(car1, car2, lines, options);
        }

        private static CarModel CreateCar(int id, Random rnd, AppOptions options)
        {
            // Standard 100x100 car points
            var points = new List<Point2D>();
            for (int x = 0; x <= 100; x++)
            {
                for (int y = 0; y <= 100; y++)
                {
                    points.Add(new Point2D(x, y));
                }
            }
            var car = new CarModel(id, points);
            
            // Random Transform
            // Position within canvas (keep somewhat inside to ensure hits)
            int cx = rnd.Next(50, options.CanvasWidth - 50);
            int cy = rnd.Next(50, options.CanvasHeight - 50);
            int rotation = rnd.Next(0, 360);

            // Apply transform logic 
            // TranslateX/Y is the offset from the original center (0,0) in local coords?
            // Actually in TestSceneBuilder we saw:
            // _car.Transform.TranslateX = x - _car.Center.X;
            // The CarModel center is calculated from points.
            // For 0..100, Center is (50, 50).
            // So if we want car at (cx, cy), we need offset (cx - 50, cy - 50).
            
            car.Transform.TranslateX = cx - car.Center.X;
            car.Transform.TranslateY = cy - car.Center.Y;
            car.Transform.RotationAngle = rotation;

            return car;
        }
    }
}
