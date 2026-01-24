using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CarCanvas.Application.Algorithms;
using CarCanvas.Infrastructure.Services;
using Xunit;
using Xunit.Abstractions;

namespace CarCanvas.Tests.Invariants
{
    public class IntersectionInvariantsTests
    {
        private readonly ITestOutputHelper _output;

        public IntersectionInvariantsTests(ITestOutputHelper output)
        {
            _output = output;
        }

        public static IEnumerable<object[]> GetSeeds()
        {
            var rnd = new Random(42); // Deterministic seeds for the suite
            // 20 random scenes
            for (int i = 0; i < 20; i++)
            {
                yield return new object[] { rnd.Next(), rnd.Next(200, 1000) };
            }
        }

        [Theory]
        [MemberData(nameof(GetSeeds))]
        public async Task Invariant_GridVsBruteForce_ShouldMatch(int seed, int linesCount)
        {
            // Arrange
            var scene = RandomSceneFactory.Generate(seed, linesCount);
            var service = new IntersectionService();

            // Act 1: Brute Force (Grid = null)
            // Ensure FastMode is false to compare full results (otherwise timing diffs might affect partial results?)
            // Actually, if Grid logic is correct, it should find same intersections as Brute Force.
            // If FastMode is ON, both should stop at same count if order is same... 
            // BUT Grid traversal order is likely different from List iteration order.
            // So if FastMode is ON, they might stop at different sets of points.
            // Invariant: "TotalHits" should be same if we run FULL scan.
            scene.Options.FastMode = false;
            
            var resultBrute = await service.FindIntersectionsAsync(scene.Car, scene.OtherCar, scene.Lines, scene.Options, null);

            // Act 2: Grid
            // Create Grid
            var grid = new UniformGridIndex(scene.Options.CanvasWidth, scene.Options.CanvasHeight, 50);
            for (int i = 0; i < scene.Lines.Count; i++)
            {
                grid.Add(scene.Lines[i], i);
            }
            
            var resultGrid = await service.FindIntersectionsAsync(scene.Car, scene.OtherCar, scene.Lines, scene.Options, grid);

            // Assert
            try 
            {
                Assert.Equal(resultBrute.TotalHitsLines, resultGrid.TotalHitsLines);
                // Grid optimization currently applies to Line checks.
                // We also check Cars hit count just in case.
                Assert.Equal(resultBrute.TotalHitsCars, resultGrid.TotalHitsCars);
            }
            catch (Xunit.Sdk.EqualException)
            {
                _output.WriteLine($"Failure Seed: {seed}");
                _output.WriteLine($"Brute Lines: {resultBrute.TotalHitsLines}, Grid Lines: {resultGrid.TotalHitsLines}");
                throw;
            }
        }

        [Theory]
        [MemberData(nameof(GetSeeds))]
        public async Task Invariant_FullVsFast_ShouldSatisfyInequality(int seed, int linesCount)
        {
            // Arrange
            var scene = RandomSceneFactory.Generate(seed, linesCount);
            var service = new IntersectionService();
            
            // Set a low limit to trigger Early Stop
            scene.Options.MaxMarkersToDraw = 50; 

            // Act 1: Full Mode
            scene.Options.FastMode = false;
            var resultFull = await service.FindIntersectionsAsync(scene.Car, scene.OtherCar, scene.Lines, scene.Options, null);

            // Act 2: Fast Mode
            scene.Options.FastMode = true;
            var resultFast = await service.FindIntersectionsAsync(scene.Car, scene.OtherCar, scene.Lines, scene.Options, null);

            // Assert
            // Full >= Fast
            // Even if Fast stops early, its TotalHits should be <= Full TotalHits.
            // Note: If Fast mode stops early, resultFast.TotalHits might be small (e.g. 50).
            // resultFull.TotalHits will be the actual total (e.g. 200).
            
            try
            {
                Assert.True(resultFull.TotalHitsLines >= resultFast.TotalHitsLines, 
                    $"Full ({resultFull.TotalHitsLines}) should be >= Fast ({resultFast.TotalHitsLines})");
            }
            catch (Xunit.Sdk.TrueException)
            {
                _output.WriteLine($"Failure Seed: {seed}");
                throw;
            }
        }
    }
}
