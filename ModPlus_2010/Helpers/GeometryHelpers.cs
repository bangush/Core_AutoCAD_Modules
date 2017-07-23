﻿using Autodesk.AutoCAD.Geometry;

namespace ModPlus.Helpers
{
    /// <summary>Вспомогательные методы построения геометрии</summary>
    public static class GeometryHelpers
    {
        /// <summary>3Д точка по направлению. Направление берется как единычный вектор из точки pt2 к точке pt1, перемножается на указанную
        /// длину и откладывается от указанной точки</summary>
        /// <param name="pt1">Первая точка для получения единичного вектора</param>
        /// <param name="pt2">Вторая точка для получения единичного вектора</param>
        /// <param name="ptFrom">Точка от которой откладывается расстояние</param>
        /// <param name="lenght">Расстояние на котором нужно получить точку</param>
        /// <returns>3Д точка</returns>
        public static Point3d Point3dAtDirection(Point3d pt1, Point3d pt2, Point3d ptFrom, double lenght)
        {
            Point3d pt3 = ptFrom + (pt2 - pt1).GetNormal() * lenght;
            return pt3;
        }
        /// <summary>2Д точка по направлению. Направление берется как единычный вектор из точки pt2 к точке pt1, перемножается на указанную
        /// длину и откладывается от указанной точки</summary>
        /// <param name="pt1">Первая точка для получения единичного вектора</param>
        /// <param name="pt2">Вторая точка для получения единичного вектора</param>
        /// <param name="ptFrom">Точка от которой откладывается расстояние</param>
        /// <param name="lenght">Расстояние на котором нужно получить точку</param>
        /// <returns>2Д точка</returns>
        public static Point2d Point2dAtDirection(Point3d pt1, Point3d pt2, Point3d ptFrom, double lenght)
        {
            Point3d pt3 = ptFrom + (pt2 - pt1).GetNormal() * lenght;

            return ConvertPoint3dToPoint2d(pt3);
        }
        public static Point2d GetPointToExtendLine(Point3d p1, Point3d p2, double distance)
        {
            return (p1 + (p2 - p1).DivideBy((p2 - p1).Length) * distance).Convert2d(new Plane());
        }
        public static Point2d GetPerpendicularPoint2d(Point3d p1, Point3d p2, double distance)
        {
            return (p2 + (p2 - p1).GetPerpendicularVector() * distance).Convert2d(new Plane());
        }
        public static Point3d GetPerpendicularPoint3d(Point3d p1, Point3d p2, double distance)
        {
            return (p2 + (p2 - p1).GetPerpendicularVector() * distance);
        }
        /// <summary>Конвертирование 2Д точки в 3Д точку</summary>
        /// <param name="point2D">Исходная 2Д точка</param>
        /// <returns>Результирующая 3Д точка</returns>
        public static Point3d ConvertPoint2DToPoint3D(Point2d point2D)
        {
            return new Point3d(point2D.X, point2D.Y, 0.0);
        }
        /// <summary>Конвертирование 3Д точки в 2Д точку</summary>
        /// <param name="point3d">Исходная 3Д точка</param>
        /// <returns>Результирующая 2Д точка</returns>
        public static Point2d ConvertPoint3dToPoint2d(Point3d point3d)
        {
            return new Point2d(point3d.X, point3d.Y);
        }
    }
}
