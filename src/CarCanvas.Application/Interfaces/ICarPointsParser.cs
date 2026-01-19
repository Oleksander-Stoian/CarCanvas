using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using CarCanvas.Domain.Entities;

namespace CarCanvas.Application.Interfaces;

public interface ICarPointsParser
{
    /// <summary>
    /// Parses points from a stream.
    /// </summary>
    /// <param name="fileStream">Input stream (text content).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Read-only list of unique Point2D.</returns>
    /// <exception cref="System.FormatException">Thrown when format is invalid.</exception>
    /// <exception cref="System.IO.InvalidDataException">Thrown when constraints (count, etc) are violated.</exception>
    Task<IReadOnlyList<Point2D>> ParseAsync(Stream fileStream, CancellationToken ct = default);
}
