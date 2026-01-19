namespace CarCanvas.Application;

public class AppOptions
{
    public int CanvasWidth { get; set; } = 2000;
    public int CanvasHeight { get; set; } = 400;
    public int MaxRandomLines { get; set; } = 50000;
    public int MaxMarkersToDraw { get; set; } = 2000;
    public int StrideKey { get; set; } = 2000; // Typically matches width
}
