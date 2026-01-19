using System.Threading.Tasks;
using CarCanvas.Domain.Entities;

namespace CarCanvas.Application.Interfaces;

public interface ICarLoader
{
    Task<IEnumerable<Point2D>> LoadPointsAsync(string source);
}
