# CLAUDE.md — GeoSat

## Project Overview

GeoSat is a C# AutoCAD 2022 plugin that fetches Copernicus Sentinel-2 satellite imagery via WMTS and inserts georeferenced rasters into CAD drawings.

## Build

```bash
dotnet build GeoSat.sln -c Debug
```

Requires: .NET SDK (8.0+) with .NET Framework 4.8 targeting pack (Windows).

## Test

```bash
dotnet test GeoSat.sln
```

Test framework: xUnit 2.9.2. Tests are in `tests/GeoSat.Core.Tests/`.

## Solution Structure

```
GeoSat.sln
  src/GeoSat.Core/          — Core library (netstandard2.0). Tile fetching, CRS transforms, image stitching. No AutoCAD dependency.
  src/GeoSat.Plugin/         — AutoCAD plugin (net48). Commands: GEOSAT, GEOSATSET.
  tests/GeoSat.Core.Tests/   — Unit tests (net48, xUnit).
```

## Key Dependencies

- **ProjNet 2.0.0** — Coordinate reference system transforms. Uses `MathTransform` (not the old `IMathTransform` from GeoAPI).
- **SixLabors.ImageSharp 2.1.9** — Tile stitching (v2.x for netstandard2.0 compat; v3.x needs .NET 6+).
- **AutoCAD.NET 24.1** — AutoCAD 2022 API (compile-only, excluded from output).

## Architecture Notes

- `GeoSatEngine` is the main orchestrator: CRS transform -> tile calc -> fetch -> stitch -> world file.
- All imagery APIs expect WGS84 (lat/lon). `CrsTransformer` handles conversion from drawing CRS.
- WMTS tiles use standard slippy-map (OSM) indexing. `TileCalculator` does the math.
- OAuth2 tokens for Copernicus Data Space are managed in `WmtsTileFetcher`.
- Tile cache lives on disk via `DiskTileCache` to avoid redundant API calls.
- Platform: x64 only. All projects build x64.

## Conventions

- Nullable reference types enabled across all projects.
- Language version: `latest`.
- API credentials stored in `settings.json` (gitignored — never commit).
- World files (.jgw) are generated alongside JPEG imagery for georeferencing.
