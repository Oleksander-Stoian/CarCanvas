# CarCanvas Intersections

Web application for visualizing and intersecting "cars" (point sets) and lines.

## Requirements

*   .NET 8.0 SDK
*   Docker (optional)

## Project Structure

*   `src/CarCanvas.Domain`: Core entities (Point2D, CarModel).
*   `src/CarCanvas.Application`: Interfaces and DTOs.
*   `src/CarCanvas.Infrastructure`: Implementation of algorithms (Bresenham, PixelSet) and services.
*   `src/CarCanvas.Web`: Blazor WebAssembly UI.

## Running Locally

1.  Navigate to the solution root.
2.  Run the following command:

    ```bash
    dotnet run --project src/CarCanvas.Web/CarCanvas.Web.csproj
    ```

3.  Open browser at `http://localhost:5000` (or the port shown in console).

## Running with Docker

1.  Navigate to the solution root.
2.  Run:

    ```bash
    docker-compose up --build
    ```

3.  Open browser at `http://localhost:8080`.

## Features

*   **Car Rendering**: Loads points from `Logan.txt`.
*   **Transformations**: Translate and Rotate cars.
*   **Lines**: Add manual lines or generate N random lines.
*   **Intersections**: Finds intersections between cars and lines using PixelSet optimization.
*   **Performance**: Measures calculation time.

## Tests

Run unit tests:

```bash
dotnet test
```
