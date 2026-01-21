using CarCanvas.Application.Enums;

namespace CarCanvas.Application.DTOs;

public class AppOptions
{
    public int CanvasWidth { get; set; } = 2000;
    public int CanvasHeight { get; set; } = 400;
    public int MaxRandomLines { get; set; } = 50000;
    public int MaxMarkersToDraw { get; set; } = 5000; // Max markers to draw per frame (setings)
    public int StrideKey { get; set; } = 2000; // Typically matches width
    public CoordinateMode CoordinateMode { get; set; } = CoordinateMode.MathYUp;
}
