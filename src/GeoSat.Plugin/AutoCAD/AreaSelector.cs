using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;

namespace GeoSat.Plugin.AutoCAD
{
    /// <summary>
    /// Handles the "pick two corners" interaction in the AutoCAD editor.
    /// Returns the two points in drawing coordinates (WCS).
    /// </summary>
    public static class AreaSelector
    {
        public static (Point3d Corner1, Point3d Corner2)? PickTwoCorners(Editor editor)
        {
            var opts1 = new PromptPointOptions("\n[GeoSat] Pick first corner of the area: ");
            var result1 = editor.GetPoint(opts1);
            if (result1.Status != PromptStatus.OK)
                return null;

            var opts2 = new PromptCornerOptions("\n[GeoSat] Pick opposite corner: ", result1.Value)
            {
                UseDashedLine = true,
            };
            var result2 = editor.GetCorner(opts2);
            if (result2.Status != PromptStatus.OK)
                return null;

            return (result1.Value, result2.Value);
        }
    }
}
