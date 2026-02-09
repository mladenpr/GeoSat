using System.IO;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using GeoSat.Core;

namespace GeoSat.Plugin.AutoCAD
{
    /// <summary>
    /// Inserts a georeferenced raster image into the AutoCAD drawing.
    /// Uses the RasterImageDef / RasterImage API to create a proper image reference
    /// positioned at the correct location and scale.
    /// </summary>
    public static class RasterInserter
    {
        public static void Insert(Document doc, GeoSatResult result)
        {
            var db = doc.Database;

            using (var tr = db.TransactionManager.StartTransaction())
            {
                // 1. Create or find the image dictionary
                var imageDictId = RasterImageDef.GetImageDictionary(db);
                if (imageDictId.IsNull)
                    imageDictId = RasterImageDef.CreateImageDictionary(db);

                var imageDict = (DBDictionary)tr.GetObject(imageDictId, OpenMode.ForWrite);

                // 2. Create the image definition (points to the file on disk)
                var imageName = Path.GetFileNameWithoutExtension(result.ImagePath);
                RasterImageDef imageDef;
                ObjectId imageDefId;

                if (imageDict.Contains(imageName))
                {
                    imageDefId = imageDict.GetAt(imageName);
                    imageDef = (RasterImageDef)tr.GetObject(imageDefId, OpenMode.ForWrite);
                }
                else
                {
                    imageDef = new RasterImageDef
                    {
                        SourceFileName = result.ImagePath,
                    };
                    imageDef.Load();
                    imageDefId = imageDict.SetAt(imageName, imageDef);
                    tr.AddNewlyCreatedDBObject(imageDef, true);
                }

                // 3. Create the raster image entity
                var rasterImage = new RasterImage
                {
                    ImageDefId = imageDefId,
                };

                // 4. Set the orientation: position, scale, and direction vectors
                //    The image's coordinate system is defined by an origin + U vector + V vector.
                //    U = horizontal direction (width), V = vertical direction (height).
                var origin = new Point3d(result.InsertionPointX, result.InsertionPointY - result.ImageHeightDrawingUnits, 0);
                var uVector = new Vector3d(result.ImageWidthDrawingUnits, 0, 0);
                var vVector = new Vector3d(0, result.ImageHeightDrawingUnits, 0);

                rasterImage.Orientation = new CoordinateSystem3d(origin, uVector, vVector);
                rasterImage.ShowImage = true;

                // 5. Add to model space
                var modelSpace = (BlockTableRecord)tr.GetObject(
                    SymbolUtilityServices.GetBlockModelSpaceId(db), OpenMode.ForWrite);

                modelSpace.AppendEntity(rasterImage);
                tr.AddNewlyCreatedDBObject(rasterImage, true);

                // 6. Link the image entity to its definition
                rasterImage.AssociateRasterDef(imageDef);

                // 7. Send image to back so it doesn't cover geometry
                var drawOrder = (DrawOrderTable)tr.GetObject(
                    modelSpace.DrawOrderTableId, OpenMode.ForWrite);
                var ids = new ObjectIdCollection(new ObjectId[] { rasterImage.ObjectId });
                drawOrder.MoveToBottom(ids);

                tr.Commit();
            }
        }
    }
}
