using System;
using GeoSat.Core.WorldFile;
using Xunit;

namespace GeoSat.Core.Tests
{
    public class WorldFileGeneratorTests
    {
        [Fact]
        public void Generate_ProducesCorrectNumberOfLines()
        {
            var content = WorldFileGenerator.Generate(
                topLeftX: 0, topLeftY: 100,
                bottomRightX: 100, bottomRightY: 0,
                imageWidthPx: 1000, imageHeightPx: 1000);

            var lines = content.Split(new[] { Environment.NewLine }, StringSplitOptions.None);
            Assert.Equal(6, lines.Length);
        }

        [Fact]
        public void Generate_PixelScaleIsCorrect()
        {
            var content = WorldFileGenerator.Generate(
                topLeftX: 0, topLeftY: 1000,
                bottomRightX: 500, bottomRightY: 0,
                imageWidthPx: 500, imageHeightPx: 1000);

            var lines = content.Split(new[] { Environment.NewLine }, StringSplitOptions.None);

            // X scale = 500 / 500 = 1.0
            Assert.Equal("1.0000000000", lines[0]);

            // Y scale = (0 - 1000) / 1000 = -1.0
            Assert.Equal("-1.0000000000", lines[3]);
        }

        [Fact]
        public void Generate_RotationIsZero()
        {
            var content = WorldFileGenerator.Generate(0, 100, 100, 0, 100, 100);
            var lines = content.Split(new[] { Environment.NewLine }, StringSplitOptions.None);

            Assert.Equal("0.0000000000", lines[1]);
            Assert.Equal("0.0000000000", lines[2]);
        }

        [Theory]
        [InlineData("jpg", ".jgw")]
        [InlineData("jpeg", ".jgw")]
        [InlineData("tif", ".tfw")]
        [InlineData("tiff", ".tfw")]
        [InlineData("png", ".pgw")]
        [InlineData("bmp", ".bpw")]
        public void GetWorldFileExtension_ReturnsCorrectExtension(string input, string expected)
        {
            Assert.Equal(expected, WorldFileGenerator.GetWorldFileExtension(input));
        }
    }
}
