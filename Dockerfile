FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["src/CarCanvas.Web/CarCanvas.Web.csproj", "src/CarCanvas.Web/"]
COPY ["src/CarCanvas.Application/CarCanvas.Application.csproj", "src/CarCanvas.Application/"]
COPY ["src/CarCanvas.Domain/CarCanvas.Domain.csproj", "src/CarCanvas.Domain/"]
COPY ["src/CarCanvas.Infrastructure/CarCanvas.Infrastructure.csproj", "src/CarCanvas.Infrastructure/"]
RUN dotnet restore "src/CarCanvas.Web/CarCanvas.Web.csproj"
COPY . .
WORKDIR "/src/src/CarCanvas.Web"
RUN dotnet publish "CarCanvas.Web.csproj" -c Release -o /app/publish

FROM nginx:alpine AS final
WORKDIR /usr/share/nginx/html
COPY --from=build /app/publish/wwwroot .
COPY nginx.conf /etc/nginx/nginx.conf
