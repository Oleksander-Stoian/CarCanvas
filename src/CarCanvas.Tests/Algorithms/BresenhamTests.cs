using System.Linq;
using CarCanvas.Domain.Entities;
using CarCanvas.Infrastructure.Algorithms;
using Xunit;

namespace CarCanvas.Tests.Algorithms;

public class BresenhamTests
{
    [Fact]
    public void GetPointsOnLine_HorizontalLine_ReturnsCorrectPoints()
    {
        var start = new Point2D(0, 0);
        var end = new Point2D(5, 0);
        var points = Bresenham.GetPointsOnLine(start, end).ToList();

        Assert.Equal(6, points.Count);
        Assert.Contains(new Point2D(0, 0), points);
        Assert.Contains(new Point2D(5, 0), points);
        Assert.Contains(new Point2D(2, 0), points);
    }

    [Fact]
    public void GetPointsOnLine_DiagonalLine_ReturnsCorrectPoints()
    {
        var start = new Point2D(0, 0);
        var end = new Point2D(3, 3);
        var points = Bresenham.GetPointsOnLine(start, end).ToList();

        Assert.Equal(4, points.Count);
        Assert.Contains(new Point2D(0, 0), points);
        Assert.Contains(new Point2D(1, 1), points);
        Assert.Contains(new Point2D(2, 2), points);
        Assert.Contains(new Point2D(3, 3), points);
    }
}
