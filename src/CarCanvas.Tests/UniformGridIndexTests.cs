using Xunit;
using CarCanvas.Application.Algorithms;
using CarCanvas.Domain.Entities;
using CarCanvas.Domain.ValueObjects;
using System.Collections.Generic;
using System.Linq;
using System;

using CarCanvas.Infrastructure.Algorithms;

namespace CarCanvas.Tests
{
    public class UniformGridIndexTests
    {
        private const int CellSize = 10;

        [Fact]
        public void TraverseCells_HorizontalLineOnBoundary_ShouldIncludeBothRows()
        {
            // Line on y=10 (boundary between row 0 and 1)
            // x from 5 to 15.
            var grid = new UniformGridIndex(100, 100, CellSize);
            
            // Note: Our DDA is currently semi-open [min, max) by default for rows/cols?
            // But requirement is "inclusive boundary".
            // y=10 touches row 0 and row 1.
            var cells = grid.TraverseCells(5, 10, 15, 10).ToList();
            
            // Expected:
            // (0, 1), (1, 1) -> derived from DivFloor(10)=1.
            // But also (0, 0), (1, 0) because y=10 touches them.
            
            // Let's verify what we get.
            // If this fails, we know we need to implement the boundary logic.
            Assert.Contains((0, 1), cells);
            Assert.Contains((1, 1), cells);
            
            // These assertions enforce inclusive boundary
            Assert.Contains((0, 0), cells);
            Assert.Contains((1, 0), cells);
        }

        [Fact]
        public void TraverseCells_VerticalLineOnBoundary_ShouldIncludeBothCols()
        {
            // Line on x=10 (boundary between col 0 and 1)
            // y from 5 to 15.
            var grid = new UniformGridIndex(100, 100, CellSize);
            var cells = grid.TraverseCells(10, 5, 10, 15).ToList();

            Assert.Contains((1, 0), cells);
            Assert.Contains((1, 1), cells);
            
            Assert.Contains((0, 0), cells);
            Assert.Contains((0, 1), cells);
        }

        [Fact]
        public void TraverseCells_Diagonal45Degrees_ShouldVisitAllCells()
        {
             var grid = new UniformGridIndex(100, 100, 10);
             // (0,0) to (20,20).
             var cells = grid.TraverseCells(0, 0, 20, 20).ToList();
             
             // Standard path
             Assert.Contains((0, 0), cells);
             Assert.Contains((1, 1), cells);
             Assert.Contains((2, 2), cells);
             
             // Crosses corner (10,10) -> touches (0,1) and (1,0)
             Assert.Contains((0, 1), cells);
             Assert.Contains((1, 0), cells);
             
             // Crosses corner (20,20) -> touches (1,2) and (2,1)
             // (20,20) is the end point.
             // End point behavior: DivFloor(20)=2.
             // So it ends in (2,2).
             // Does it touch (1,2) and (2,1) at the end?
             // Yes.
             Assert.Contains((1, 2), cells);
             Assert.Contains((2, 1), cells);
        }

        [Fact]
        public void TraverseCells_CornerCrossing_Exact()
        {
             // Line from (5,5) to (15,15) with cell size 10.
             // Crosses (10,10).
             var grid = new UniformGridIndex(100, 100, 10);
             var cells = grid.TraverseCells(5, 5, 15, 15).ToList();
             
             Assert.Contains((0, 0), cells);
             Assert.Contains((1, 1), cells);
             // Corner neighbors
             Assert.Contains((0, 1), cells);
             Assert.Contains((1, 0), cells);
        }

        [Fact]
        public void FuzzTest_CompareWithLiangBarsky()
        {
             var grid = new UniformGridIndex(1000, 1000, 10);
             var rnd = new Random(42);
             int failures = 0;
             int numTests = 2000;

             for(int i=0; i<numTests; i++)
             {
                 int x0 = rnd.Next(-100, 200);
                 int y0 = rnd.Next(-100, 200);
                 int x1 = rnd.Next(-100, 200);
                 int y1 = rnd.Next(-100, 200);

                 var line = new LineSegment(new Point2D(x0, y0), new Point2D(x1, y1));
                 
                 var ddaCells = grid.TraverseCells(x0, y0, x1, y1).ToHashSet();

                 var exactCells = new HashSet<(int, int)>();
                 
                 int minX = Math.Min(x0, x1);
                 int maxX = Math.Max(x0, x1);
                 int minY = Math.Min(y0, y1);
                 int maxY = Math.Max(y0, y1);

                 int startCol = UniformGridIndex.DivFloor(minX, 10);
                 int endCol = UniformGridIndex.DivFloor(maxX, 10);
                 int startRow = UniformGridIndex.DivFloor(minY, 10);
                 int endRow = UniformGridIndex.DivFloor(maxY, 10);

                 // Check a slightly larger area to ensure we catch everything LB says is "in"
                 for(int r = startRow - 1; r <= endRow + 1; r++)
                 {
                     for(int c = startCol - 1; c <= endCol + 1; c++)
                     {
                         if (GeometryUtils.GetClippedSegment(line, new Aabb(c * 10, c * 10 + 10, r * 10, r * 10 + 10), out _))
                        {
                            exactCells.Add((c, r));
                        }
                     }
                 }

                 // Check for missing cells
                 var missing = exactCells.Except(ddaCells).ToList();
                 if (missing.Any())
                 {
                     failures++;
                     // Output first failure for debugging
                     if (failures == 1)
                     {
                        Console.WriteLine($"Failure #{failures}: Line ({x0},{y0})->({x1},{y1})");
                        Console.WriteLine("Missing cells: " + string.Join(", ", missing));
                        Console.WriteLine("DDA cells: " + string.Join(", ", ddaCells));
                     }
                 }
             }
             
             Assert.Equal(0, failures);
        }
    }
}
