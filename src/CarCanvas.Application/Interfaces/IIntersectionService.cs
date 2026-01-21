using System.Collections.Generic;
using System.Threading.Tasks;
using CarCanvas.Application.Algorithms;
using CarCanvas.Application.DTOs;
using CarCanvas.Domain.Entities;

namespace CarCanvas.Application.Interfaces;

public interface IIntersectionService
{
    void InvalidateCache();

    Task<IntersectionResult> FindIntersectionsAsync(
        CarModel targetCar, 
        CarModel otherCar, 
        IList<LineSegment> lines,
        AppOptions options,
        UniformGridIndex? gridIndex = null);
}
