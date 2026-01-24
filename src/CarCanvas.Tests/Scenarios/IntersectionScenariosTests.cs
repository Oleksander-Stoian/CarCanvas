using System.Threading.Tasks;
using Xunit;

namespace CarCanvas.Tests.Scenarios
{
    public class IntersectionScenariosTests
    {
        [Fact]
        public async Task LineFarAway_ShouldReturnZeroHits()
        {
            // Car at (100, 100), size ~100x100
            // Line at (800, 800) -> (900, 900)
            var result = await new TestSceneBuilder()
                .WithCar(1, 100, 100, 0)
                .AddLine(800, 800, 900, 900)
                .ExecuteAsync();

            Assert.Equal(0, result.TotalHitsLines);
        }

        [Fact]
        public async Task LineCrossesCar_ShouldReturnNonZeroHits()
        {
            // Car at (500, 500), 100x100 solid block (local 0..100)
            // Center of car model is (50, 50).
            // Placed at (500, 500) -> Global coords approx 450..550
            
            // Line crossing through center
            var result = await new TestSceneBuilder()
                .WithCar(1, 500, 500, 0)
                .AddLine(400, 500, 600, 500)
                .ExecuteAsync();

            Assert.True(result.TotalHitsLines > 0, "Line crossing car should have hits");
        }

        [Fact]
        public async Task LineTouchesAabbBoundary_ShouldNotThrow_AndResultStable()
        {
            // Car at (200, 200)
            // Line grazing the edge
            var result = await new TestSceneBuilder()
                .WithCar(1, 200, 200, 0)
                .AddLine(0, 0, 1000, 0) // Far away line, just to check stability
                .AddLine(200, 200, 300, 200) // Line potentially inside or on edge
                .ExecuteAsync();

            Assert.True(result.TotalHitsLines >= 0);
        }

        [Fact]
        public async Task ZeroLengthLineOutsideCar_ShouldReturnZeroHits()
        {
            var result = await new TestSceneBuilder()
                .WithCar(1, 300, 300, 0)
                .AddLine(10, 10, 10, 10) // Point far away
                .ExecuteAsync();

            Assert.Equal(0, result.TotalHitsLines);
        }

        [Fact]
        public async Task ZeroLengthLineInsideCar_ShouldReturnNonZeroHits()
        {
            // Car at (500,500). Center (50,50) local.
            // Global center approx (500, 500).
            
            var result = await new TestSceneBuilder()
                .WithCar(1, 500, 500, 0)
                .AddLine(500, 500, 500, 500) // Point exactly in center
                .ExecuteAsync();

            Assert.True(result.TotalHitsLines > 0, $"Point inside car should be detected. Actual: {result.TotalHitsLines}");
        }

        [Fact]
        public async Task Rotation_ShouldNotThrow_AndHitsNonNegative()
        {
            // Rotated car + random lines
            var result = await new TestSceneBuilder()
                .WithCar(1, 500, 500, 45) // 45 degrees
                .AddRandomLines(50, seed: 123)
                .WithOptions(fastMode: false, maxMarkers: 1000, useGrid: true)
                .ExecuteAsync();

            Assert.True(result.TotalHitsLines >= 0);
        }
    }
}