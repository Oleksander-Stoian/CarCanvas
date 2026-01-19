using System.Collections.Generic;
using System.Linq;
using CarCanvas.Domain.Entities;
using CarCanvas.Domain.ValueObjects;
using CarCanvas.Infrastructure.Algorithms;
using Xunit;

namespace CarCanvas.Tests.Algorithms;

public class PointTransformerTests
{
    [Fact]
    public void TransformPoints_Translation_OffsetsPoints()
    {
        var points = new List<Point2D> { new Point2D(0, 0) };
        var center = new Point2D(0, 0);
        var transform = new Transform { TranslateX = 10, TranslateY = 5, RotationAngle = 0 };

        var result = PointTransformer.TransformPoints(points, center, transform).ToList();

        Assert.Single(result);
        Assert.Equal(10, result[0].X);
        Assert.Equal(5, result[0].Y);
    }

    [Fact]
    public void TransformPoints_Rotation90Degrees_RotatesAroundCenter()
    {
        // Center (0,0), Point (10,0) -> Rotate 90 -> Point (0, 10)
        var points = new List<Point2D> { new Point2D(10, 0) };
        var center = new Point2D(0, 0);
        var transform = new Transform { TranslateX = 0, TranslateY = 0, RotationAngle = 90 };

        var result = PointTransformer.TransformPoints(points, center, transform).ToList();

        Assert.Single(result);
        // Allowing for small rounding errors, but ints are exact here if logic is right
        // 10 * cos(90) - 0 * sin(90) = 0
        // 10 * sin(90) + 0 * cos(90) = 10
        Assert.Equal(0, result[0].X);
        Assert.Equal(10, result[0].Y);
    }
}
