using System.Collections.Generic;
using System.Threading.Tasks;
using CarCanvas.Application.DTOs;
using CarCanvas.Domain.Entities;

namespace CarCanvas.Application.Interfaces;

public interface IIntersectionService
{
    Task<IntersectionResult> FindIntersectionsAsync(
        CarModel targetCar, 
        CarModel otherCar, 
        IEnumerable<LineSegment> lines,
        AppOptions options);
}
