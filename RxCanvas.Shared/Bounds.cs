using RxCanvas.Interfaces;
using RxCanvas.Model;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RxCanvas.Bounds
{
    internal static class Helper
    {
        public const int PointBoundVertexCount = 4;

        public static double Min(double val1, double val2, double val3, double val4)
        {
            return Math.Min(Math.Min(val1, val2), Math.Min(val3, val4));
        }

        public static double Max(double val1, double val2, double val3, double val4)
        {
            return Math.Max(Math.Max(val1, val2), Math.Max(val3, val4));
        }

        public static IPolygon CreateBoundsPolygon(
            IModelToNativeConverter nativeConverter,
            ICanvasFactory canvasFactory,
            int points)
        {
            var polygon = canvasFactory.CreatePolygon();
            polygon.Points = new IPoint[points];
            polygon.Lines = new ILine[points];

            for (int i = 0; i < points; i++)
            {
                polygon.Points[i] = canvasFactory.CreatePoint();

                var _xline = canvasFactory.CreateLine();
                _xline.Stroke = canvasFactory.CreateColor();
                _xline.Stroke.A = 0xFF;
                _xline.Stroke.R = 0x00;
                _xline.Stroke.G = 0xBF;
                _xline.Stroke.B = 0xFF;
                _xline.StrokeThickness = 2.0;
                var _nline = nativeConverter.Convert(_xline);
                polygon.Lines[i] = _nline;
            }

            return polygon;
        }

        public static void UpdatePointBounds(IPoint point, IPoint[] ps, ILine[] ls, double size, double offset)
        {
            Debug.Assert(point != null);

            double x = point.X - (size / 2.0);
            double y = point.Y - (size / 2.0);
            double width = size;
            double height = size;

            Helper.UpdateRectangleBounds(ps, ls, offset, x, y, width, height);
        }

        public static void UpdateRectangleBounds(IPoint[] ps, ILine[] ls, double offset, double x, double y, double width, double height)
        {
            Debug.Assert(ps != null);
            Debug.Assert(ls != null);
            Debug.Assert(ps.Length == PointBoundVertexCount);
            Debug.Assert(ls.Length == PointBoundVertexCount);

            // top-left
            ps[0].X = x - offset;
            ps[0].Y = y - offset;
            // top-right
            ps[1].X = (x + width) + offset;
            ps[1].Y = y - offset;
            // botton-right
            ps[2].X = (x + width) + offset;
            ps[2].Y = (y + height) + offset;
            // bottom-left
            ps[3].X = x - offset;
            ps[3].Y = (y + height) + offset;

            Helper.MoveLine(ls[0], ps[0], ps[1]);
            Helper.MoveLine(ls[1], ps[1], ps[2]);
            Helper.MoveLine(ls[2], ps[2], ps[3]);
            Helper.MoveLine(ls[3], ps[3], ps[0]);
        }

        public static void MoveLine(ILine line, IPoint point1, IPoint point2)
        {
            line.Point1 = point1;
            line.Point2 = point2;
        }
    }

    internal static class MonotoneChain
    {
        // Implementation of Andrew's monotone chain 2D convex hull algorithm.
        // http://en.wikibooks.org/wiki/Algorithm_Implementation/Geometry/Convex_hull/Monotone_chain
        // Asymptotic complexity O(n log n).

        // 2D cross product of OA and OB vectors, i.e. z-component of their 3D cross product.
        // Returns a positive value, if OAB makes a counter-clockwise turn,
        // negative for clockwise turn, and zero if the points are collinear.
        public static double Cross(XPoint p1, XPoint p2, XPoint p3)
        {
            return (p2.X - p1.X) * (p3.Y - p1.Y) - (p2.Y - p1.Y) * (p3.X - p1.X);
        }

        // Returns a list of points on the convex hull in counter-clockwise order.
        // Note: the last point in the returned list is the same as the first one.
        public static void ConvexHull(XPoint[] points, out XPoint[] hull, out int k)
        {
            int n = points.Length;
            int i, t;

            k = 0;
            hull = new XPoint[2 * n];

            // sort points lexicographically
            Array.Sort(points);

            // lower hull
            for (i = 0; i < n; i++)
            {
                while (k >= 2 && Cross(hull[k - 2], hull[k - 1], points[i]) <= 0)
                    k--;

                hull[k++] = points[i];
            }

            // upper hull
            for (i = n - 2, t = k + 1; i >= 0; i--)
            {
                while (k >= t && Cross(hull[k - 2], hull[k - 1], points[i]) <= 0)
                    k--;

                hull[k++] = points[i];
            }
        }
    }

    public class LineBounds : IBounds
    {
        private ILine _line;
        private double _size;
        private double _offset;
        private ICanvas _canvas;
        private IPolygon _polygonLine;
        private IPolygon _polygonPoint1;
        private IPolygon _polygonPoint2;
        private bool _isVisible;

        private enum HitResult { None, Point1, Point2, Line};
        private HitResult _hitResult;

        public LineBounds(
            IModelToNativeConverter nativeConverter,
            ICanvasFactory canvasFactory,
            ICanvas canvas,
            ILine line,
            double size,
            double offset)
        {
            _line = line;
            _size = size;
            _offset = offset;
            _canvas = canvas;

            _hitResult = HitResult.None;

            InitBounds(nativeConverter, canvasFactory);
        }

        private void InitBounds(
            IModelToNativeConverter nativeConverter,
            ICanvasFactory canvasFactory)
        {
            _polygonPoint1 = Helper.CreateBoundsPolygon(nativeConverter, canvasFactory, 4);
            _polygonPoint2 = Helper.CreateBoundsPolygon(nativeConverter, canvasFactory, 4);
            _polygonLine = Helper.CreateBoundsPolygon(nativeConverter, canvasFactory, 4);
        }

        private void UpdatePoint1Bounds()
        {
            var ps = _polygonPoint1.Points;
            var ls = _polygonPoint1.Lines;
            Helper.UpdatePointBounds(_line.Point1, ps, ls, _size, _offset);
        }

        private void UpdatePoint2Bounds()
        {
            var ps = _polygonPoint2.Points;
            var ls = _polygonPoint2.Lines;
            Helper.UpdatePointBounds(_line.Point2, ps, ls, _size, _offset);
        }

        private void UpdateLineBounds()
        {
            var ps = _polygonLine.Points;
            var ls = _polygonLine.Lines;
            var ps1 = _polygonPoint1.Points;
            var ps2 = _polygonPoint2.Points;

            ps[0].X = Helper.Min(ps1[0].X, ps1[1].X, ps1[2].X, ps1[3].X);
            ps[1].X = Helper.Max(ps1[0].X, ps1[1].X, ps1[2].X, ps1[3].X);
            ps[2].X = Helper.Max(ps2[0].X, ps2[1].X, ps2[2].X, ps2[3].X);
            ps[3].X = Helper.Min(ps2[0].X, ps2[1].X, ps2[2].X, ps2[3].X);

            if (((_line.Point2.X > _line.Point1.X) && (_line.Point2.Y < _line.Point1.Y)) ||
                ((_line.Point2.X < _line.Point1.X) && (_line.Point2.Y > _line.Point1.Y)))
            {
                ps[0].Y = Helper.Min(ps1[0].Y, ps1[1].Y, ps1[2].Y, ps1[3].Y);
                ps[1].Y = Helper.Max(ps1[0].Y, ps1[1].Y, ps1[2].Y, ps1[3].Y);
                ps[2].Y = Helper.Max(ps2[0].Y, ps2[1].Y, ps2[2].Y, ps2[3].Y);
                ps[3].Y = Helper.Min(ps2[0].Y, ps2[1].Y, ps2[2].Y, ps2[3].Y);
            }
            else
            {
                ps[0].Y = Helper.Max(ps1[0].Y, ps1[1].Y, ps1[2].Y, ps1[3].Y);
                ps[1].Y = Helper.Min(ps1[0].Y, ps1[1].Y, ps1[2].Y, ps1[3].Y);
                ps[2].Y = Helper.Min(ps2[0].Y, ps2[1].Y, ps2[2].Y, ps2[3].Y);
                ps[3].Y = Helper.Max(ps2[0].Y, ps2[1].Y, ps2[2].Y, ps2[3].Y);
            }

            Helper.MoveLine(ls[0], ps[0], ps[1]);
            Helper.MoveLine(ls[1], ps[1], ps[2]);
            Helper.MoveLine(ls[2], ps[2], ps[3]);
            Helper.MoveLine(ls[3], ps[3], ps[0]);
        }

        public void Update()
        {
            UpdatePoint1Bounds();
            UpdatePoint2Bounds();
            UpdateLineBounds();
        }

        public bool IsVisible()
        {
            return _isVisible;
        }

        public void Show()
        {
            if (!_isVisible)
            {
                foreach (var line in _polygonLine.Lines)
                {
                    _canvas.Add(line);
                }
                foreach (var line in _polygonPoint1.Lines)
                {
                    _canvas.Add(line);
                }
                foreach (var line in _polygonPoint2.Lines)
                {
                    _canvas.Add(line);
                }
                _isVisible = true;
            }
        }

        public void Hide()
        {
            if (_isVisible)
            {
                foreach (var line in _polygonLine.Lines)
                {
                    _canvas.Remove(line);
                }
                foreach (var line in _polygonPoint1.Lines)
                {
                    _canvas.Remove(line);
                }
                foreach (var line in _polygonPoint2.Lines)
                {
                    _canvas.Remove(line);
                }
                _isVisible = false;
            }
        }

        public bool Contains(double x, double y)
        {
            if (_polygonPoint1.Contains(x, y))
            {
                _hitResult = HitResult.Point1;
                return true;
            }
            else if (_polygonPoint2.Contains(x, y))
            {
                _hitResult = HitResult.Point2;
                return true;
            }
            else if (_polygonLine.Contains(x, y))
            {
                _hitResult = HitResult.Line;
                return true;
            }
            _hitResult = HitResult.None;
            return false;
        }

        public void Move(double dx, double dy)
        {
            //Debug.Print("_hitResult: {0}", _hitResult);
            switch(_hitResult)
            {
                case HitResult.Point1:
                    {
                        double x1 = _line.Point1.X - dx;
                        double y1 = _line.Point1.Y - dy;
                        _line.Point1.X = _canvas.EnableSnap ? _canvas.Snap(x1, _canvas.SnapX) : x1;
                        _line.Point1.Y = _canvas.EnableSnap ? _canvas.Snap(y1, _canvas.SnapY) : y1;
                        _line.Point1 = _line.Point1;
                    }
                    break;
                case HitResult.Point2:
                    {
                        double x2 = _line.Point2.X - dx;
                        double y2 = _line.Point2.Y - dy;
                        _line.Point2.X = _canvas.EnableSnap ? _canvas.Snap(x2, _canvas.SnapX) : x2;
                        _line.Point2.Y = _canvas.EnableSnap ? _canvas.Snap(y2, _canvas.SnapY) : y2;
                        _line.Point2 = _line.Point2;
                    }
                    break;
                case HitResult.Line:
                    {
                        double x1 = _line.Point1.X - dx;
                        double y1 = _line.Point1.Y - dy;
                        double x2 = _line.Point2.X - dx;
                        double y2 = _line.Point2.Y - dy;
                        _line.Point1.X = _canvas.EnableSnap ? _canvas.Snap(x1, _canvas.SnapX) : x1;
                        _line.Point1.Y = _canvas.EnableSnap ? _canvas.Snap(y1, _canvas.SnapY) : y1;
                        _line.Point2.X = _canvas.EnableSnap ? _canvas.Snap(x2, _canvas.SnapX) : x2;
                        _line.Point2.Y = _canvas.EnableSnap ? _canvas.Snap(y2, _canvas.SnapY) : y2;
                        _line.Point1 = _line.Point1;
                        _line.Point2 = _line.Point2;
                    }
                    break;
            }
        }
    }

    public class BezierBounds : IBounds
    {
        private IBezier _bezier;
        private double _size;
        private double _offset;
        private ICanvas _canvas;
        private IPolygon _polygonBezier;
        private IPolygon _polygonStart;
        private IPolygon _polygonPoint1;
        private IPolygon _polygonPoint2;
        private IPolygon _polygonPoint3;
        private bool _isVisible;

        private enum HitResult { None, Start, Point1, Point2, Point3, Bezier };
        private HitResult _hitResult;

        public BezierBounds(
            IModelToNativeConverter nativeConverter,
            ICanvasFactory canvasFactory,
            ICanvas canvas,
            IBezier bezier,
            double size,
            double offset)
        {
            _bezier = bezier;
            _size = size;
            _offset = offset;
            _canvas = canvas;

            InitBounds(nativeConverter, canvasFactory);
        }

        private void InitBounds(
            IModelToNativeConverter nativeConverter,
            ICanvasFactory canvasFactory)
        {
            _polygonStart = Helper.CreateBoundsPolygon(nativeConverter, canvasFactory, 4);
            _polygonPoint1 = Helper.CreateBoundsPolygon(nativeConverter, canvasFactory, 4);
            _polygonPoint2 = Helper.CreateBoundsPolygon(nativeConverter, canvasFactory, 4);
            _polygonPoint3 = Helper.CreateBoundsPolygon(nativeConverter, canvasFactory, 4);
            _polygonBezier = Helper.CreateBoundsPolygon(nativeConverter, canvasFactory, 4);
        }

        private void UpdateStartBounds()
        {
            var ps = _polygonStart.Points;
            var ls = _polygonStart.Lines;
            Helper.UpdatePointBounds(_bezier.Start, ps, ls, _size, _offset);
        }

        private void UpdatePoint1Bounds()
        {
            var ps = _polygonPoint1.Points;
            var ls = _polygonPoint1.Lines;
            Helper.UpdatePointBounds(_bezier.Point1, ps, ls, _size, _offset);
        }

        private void UpdatePoint2Bounds()
        {
            var ps = _polygonPoint2.Points;
            var ls = _polygonPoint2.Lines;
            Helper.UpdatePointBounds(_bezier.Point2, ps, ls, _size, _offset);
        }

        private void UpdatePoint3Bounds()
        {
            var ps = _polygonPoint3.Points;
            var ls = _polygonPoint3.Lines;
            Helper.UpdatePointBounds(_bezier.Point3, ps, ls, _size, _offset);
        }

        private int k;
        private XPoint[] convexHull;

        public bool ConvexHullAsPolygonContains(double x, double y)
        {
            bool contains = false;
            for (int i = 0, j = k - 2; i < k - 1; j = i++)
            {
                if (((convexHull[i].Y > y) != (convexHull[j].Y > y))
                    && (x < (convexHull[j].X - convexHull[i].X) * (y - convexHull[i].Y) / (convexHull[j].Y - convexHull[i].Y) + convexHull[i].X))
                {
                    contains = !contains;
                }
            }
            return contains;
        }

        private void UpdateBezierBounds()
        {
            var ps = _polygonBezier.Points.Select(p => p as XPoint).ToArray();
            var ls = _polygonBezier.Lines;

            ps[0].X = _bezier.Start.X;
            ps[0].Y = _bezier.Start.Y;
            ps[1].X = _bezier.Point1.X;
            ps[1].Y = _bezier.Point1.Y;
            ps[2].X = _bezier.Point2.X;
            ps[2].Y = _bezier.Point2.Y;
            ps[3].X = _bezier.Point3.X;
            ps[3].Y = _bezier.Point3.Y;

            MonotoneChain.ConvexHull(ps, out convexHull, out k);
            //Debug.Print("k: {0}", k);

            if (k == 3)
            {
                Helper.MoveLine(ls[0], convexHull[0], convexHull[1]);
                Helper.MoveLine(ls[1], convexHull[1], convexHull[2]);

                // not used
                Helper.MoveLine(ls[2], convexHull[0], convexHull[0]);
                Helper.MoveLine(ls[3], convexHull[0], convexHull[0]);
            }
            else if (k == 4)
            {
                Helper.MoveLine(ls[0], convexHull[0], convexHull[1]);
                Helper.MoveLine(ls[1], convexHull[1], convexHull[2]);
                Helper.MoveLine(ls[2], convexHull[2], convexHull[3]);

                // not used
                Helper.MoveLine(ls[3], convexHull[0], convexHull[0]);
            }
            else if (k == 5)
            {
                Helper.MoveLine(ls[0], convexHull[0], convexHull[1]);
                Helper.MoveLine(ls[1], convexHull[1], convexHull[2]);
                Helper.MoveLine(ls[2], convexHull[2], convexHull[3]);
                Helper.MoveLine(ls[3], convexHull[3], convexHull[4]);
            }
        }

        public void Update()
        {
            UpdateStartBounds();
            UpdatePoint1Bounds();
            UpdatePoint2Bounds();
            UpdatePoint3Bounds();
            UpdateBezierBounds();
        }

        public bool IsVisible()
        {
            return _isVisible;
        }

        public void Show()
        {
            if (!_isVisible)
            {
                foreach (var line in _polygonBezier.Lines)
                {
                    _canvas.Add(line);
                }
                foreach (var line in _polygonStart.Lines)
                {
                    _canvas.Add(line);
                }
                foreach (var line in _polygonPoint1.Lines)
                {
                    _canvas.Add(line);
                }
                foreach (var line in _polygonPoint2.Lines)
                {
                    _canvas.Add(line);
                }
                foreach (var line in _polygonPoint3.Lines)
                {
                    _canvas.Add(line);
                }
                _isVisible = true;
            }
        }

        public void Hide()
        {
            if (_isVisible)
            {
                foreach (var line in _polygonBezier.Lines)
                {
                    _canvas.Remove(line);
                }
                foreach (var line in _polygonStart.Lines)
                {
                    _canvas.Remove(line);
                }
                foreach (var line in _polygonPoint1.Lines)
                {
                    _canvas.Remove(line);
                }
                foreach (var line in _polygonPoint2.Lines)
                {
                    _canvas.Remove(line);
                }
                foreach (var line in _polygonPoint3.Lines)
                {
                    _canvas.Remove(line);
                }
                _isVisible = false;
            }
        }

        public bool Contains(double x, double y)
        {
            if (_polygonStart.Contains(x, y))
            {
                _hitResult = HitResult.Start;
                return true;
            }
            else if (_polygonPoint1.Contains(x, y))
            {
                _hitResult = HitResult.Point1;
                return true;
            }
            else if (_polygonPoint2.Contains(x, y))
            {
                _hitResult = HitResult.Point2;
                return true;
            }
            else if (_polygonPoint3.Contains(x, y))
            {
                _hitResult = HitResult.Point3;
                return true;
            }
            else if (ConvexHullAsPolygonContains(x, y)) //_polygonBezier.Contains(x, y)
            {
                _hitResult = HitResult.Bezier;
                return true;
            }
            _hitResult = HitResult.None;
            return false;
        }

        public void Move(double dx, double dy)
        {
            //Debug.Print("_hitResult: {0}", _hitResult);
            switch (_hitResult)
            {
                case HitResult.Start:
                    {
                        double x = _bezier.Start.X - dx;
                        double y = _bezier.Start.Y - dy;
                        _bezier.Start.X = _canvas.EnableSnap ? _canvas.Snap(x, _canvas.SnapX) : x;
                        _bezier.Start.Y = _canvas.EnableSnap ? _canvas.Snap(y, _canvas.SnapY) : y;
                        _bezier.Start = _bezier.Start;
                    }
                    break;
                case HitResult.Point1:
                    {
                        double x1 = _bezier.Point1.X - dx;
                        double y1 = _bezier.Point1.Y - dy;
                        _bezier.Point1.X = _canvas.EnableSnap ? _canvas.Snap(x1, _canvas.SnapX) : x1;
                        _bezier.Point1.Y = _canvas.EnableSnap ? _canvas.Snap(y1, _canvas.SnapY) : y1;
                        _bezier.Point1 = _bezier.Point1;
                    }
                    break;
                case HitResult.Point2:
                    {
                        double x2 = _bezier.Point2.X - dx;
                        double y2 = _bezier.Point2.Y - dy;
                        _bezier.Point2.X = _canvas.EnableSnap ? _canvas.Snap(x2, _canvas.SnapX) : x2;
                        _bezier.Point2.Y = _canvas.EnableSnap ? _canvas.Snap(y2, _canvas.SnapY) : y2;
                        _bezier.Point2 = _bezier.Point2;
                    }
                    break;
                case HitResult.Point3:
                    {
                        double x3 = _bezier.Point3.X - dx;
                        double y3 = _bezier.Point3.Y - dy;
                        _bezier.Point3.X = _canvas.EnableSnap ? _canvas.Snap(x3, _canvas.SnapX) : x3;
                        _bezier.Point3.Y = _canvas.EnableSnap ? _canvas.Snap(y3, _canvas.SnapY) : y3;
                        _bezier.Point3 = _bezier.Point3;
                    }
                    break;
                case HitResult.Bezier:
                    {
                        double x = _bezier.Start.X - dx;
                        double y = _bezier.Start.Y - dy;
                        double x1 = _bezier.Point1.X - dx;
                        double y1 = _bezier.Point1.Y - dy;
                        double x2 = _bezier.Point2.X - dx;
                        double y2 = _bezier.Point2.Y - dy;
                        double x3 = _bezier.Point3.X - dx;
                        double y3 = _bezier.Point3.Y - dy;
                        _bezier.Start.X = _canvas.EnableSnap ? _canvas.Snap(x, _canvas.SnapX) : x;
                        _bezier.Start.Y = _canvas.EnableSnap ? _canvas.Snap(y, _canvas.SnapY) : y;
                        _bezier.Point1.X = _canvas.EnableSnap ? _canvas.Snap(x1, _canvas.SnapX) : x1;
                        _bezier.Point1.Y = _canvas.EnableSnap ? _canvas.Snap(y1, _canvas.SnapY) : y1;
                        _bezier.Point2.X = _canvas.EnableSnap ? _canvas.Snap(x2, _canvas.SnapX) : x2;
                        _bezier.Point2.Y = _canvas.EnableSnap ? _canvas.Snap(y2, _canvas.SnapY) : y2;
                        _bezier.Point3.X = _canvas.EnableSnap ? _canvas.Snap(x3, _canvas.SnapX) : x3;
                        _bezier.Point3.Y = _canvas.EnableSnap ? _canvas.Snap(y3, _canvas.SnapY) : y3;
                        _bezier.Start = _bezier.Start;
                        _bezier.Point1 = _bezier.Point1;
                        _bezier.Point2 = _bezier.Point2;
                        _bezier.Point3 = _bezier.Point3;
                    }
                    break;
            }
        }
    }

    public class QuadraticBezierBounds : IBounds
    {
        private IQuadraticBezier _quadraticBezier;
        private double _size;
        private double _offset;
        private ICanvas _canvas;
        private IPolygon _polygonQuadraticBezier;
        private IPolygon _polygonStart;
        private IPolygon _polygonPoint1;
        private IPolygon _polygonPoint2;
        private bool _isVisible;

        private enum HitResult { None, Start, Point1, Point2, QuadraticBezier };
        private HitResult _hitResult;

        public QuadraticBezierBounds(
            IModelToNativeConverter nativeConverter,
            ICanvasFactory canvasFactory,
            ICanvas canvas,
            IQuadraticBezier quadraticBezier,
            double size,
            double offset)
        {
            _quadraticBezier = quadraticBezier;
            _size = size;
            _offset = offset;
            _canvas = canvas;

            InitBounds(nativeConverter, canvasFactory);
        }

        private void InitBounds(
            IModelToNativeConverter nativeConverter,
            ICanvasFactory canvasFactory)
        {
            _polygonStart = Helper.CreateBoundsPolygon(nativeConverter, canvasFactory, 4);
            _polygonPoint1 = Helper.CreateBoundsPolygon(nativeConverter, canvasFactory, 4);
            _polygonPoint2 = Helper.CreateBoundsPolygon(nativeConverter, canvasFactory, 4);
            _polygonQuadraticBezier = Helper.CreateBoundsPolygon(nativeConverter, canvasFactory, 3);
        }

        private void UpdateStartBounds()
        {
            var ps = _polygonStart.Points;
            var ls = _polygonStart.Lines;
            Helper.UpdatePointBounds(_quadraticBezier.Start, ps, ls, _size, _offset);
        }

        private void UpdatePoint1Bounds()
        {
            var ps = _polygonPoint1.Points;
            var ls = _polygonPoint1.Lines;
            Helper.UpdatePointBounds(_quadraticBezier.Point1, ps, ls, _size, _offset);
        }

        private void UpdatePoint2Bounds()
        {
            var ps = _polygonPoint2.Points;
            var ls = _polygonPoint2.Lines;
            Helper.UpdatePointBounds(_quadraticBezier.Point2, ps, ls, _size, _offset);
        }

        private int k;
        private XPoint[] convexHull;

        public bool ConvexHullAsPolygonContains(double x, double y)
        {
            bool contains = false;
            for (int i = 0, j = k - 2; i < k - 1; j = i++)
            {
                if (((convexHull[i].Y > y) != (convexHull[j].Y > y))
                    && (x < (convexHull[j].X - convexHull[i].X) * (y - convexHull[i].Y) / (convexHull[j].Y - convexHull[i].Y) + convexHull[i].X))
                {
                    contains = !contains;
                }
            }
            return contains;
        }

        private void UpdateQuadraticBezierBounds()
        {
            var ps = _polygonQuadraticBezier.Points.Select(p => p as XPoint).ToArray();
            var ls = _polygonQuadraticBezier.Lines;

            ps[0].X = _quadraticBezier.Start.X;
            ps[0].Y = _quadraticBezier.Start.Y;
            ps[1].X = _quadraticBezier.Point1.X;
            ps[1].Y = _quadraticBezier.Point1.Y;
            ps[2].X = _quadraticBezier.Point2.X;
            ps[2].Y = _quadraticBezier.Point2.Y;

            MonotoneChain.ConvexHull(ps, out convexHull, out k);
            //Debug.Print("k: {0}", k);

            if (k == 3)
            {
                Helper.MoveLine(ls[0], convexHull[0], convexHull[1]);
                Helper.MoveLine(ls[1], convexHull[1], convexHull[2]);

                // not used
                Helper.MoveLine(ls[2], convexHull[0], convexHull[0]);
            }
            else if (k == 4)
            {
                Helper.MoveLine(ls[0], convexHull[0], convexHull[1]);
                Helper.MoveLine(ls[1], convexHull[1], convexHull[2]);
                Helper.MoveLine(ls[2], convexHull[2], convexHull[3]);
            }
        }

        public void Update()
        {
            UpdateStartBounds();
            UpdatePoint1Bounds();
            UpdatePoint2Bounds();
            UpdateQuadraticBezierBounds();
        }

        public bool IsVisible()
        {
            return _isVisible;
        }

        public void Show()
        {
            if (!_isVisible)
            {
                foreach (var line in _polygonQuadraticBezier.Lines)
                {
                    _canvas.Add(line);
                }
                foreach (var line in _polygonStart.Lines)
                {
                    _canvas.Add(line);
                }
                foreach (var line in _polygonPoint1.Lines)
                {
                    _canvas.Add(line);
                }
                foreach (var line in _polygonPoint2.Lines)
                {
                    _canvas.Add(line);
                }
                _isVisible = true;
            }
        }

        public void Hide()
        {
            if (_isVisible)
            {
                foreach (var line in _polygonQuadraticBezier.Lines)
                {
                    _canvas.Remove(line);
                }
                foreach (var line in _polygonStart.Lines)
                {
                    _canvas.Remove(line);
                }
                foreach (var line in _polygonPoint1.Lines)
                {
                    _canvas.Remove(line);
                }
                foreach (var line in _polygonPoint2.Lines)
                {
                    _canvas.Remove(line);
                }
                _isVisible = false;
            }
        }

        public bool Contains(double x, double y)
        {
            if (_polygonStart.Contains(x, y))
            {
                _hitResult = HitResult.Start;
                return true;
            }
            else if (_polygonPoint1.Contains(x, y))
            {
                _hitResult = HitResult.Point1;
                return true;
            }
            else if (_polygonPoint2.Contains(x, y))
            {
                _hitResult = HitResult.Point2;
                return true;
            }
            else if (ConvexHullAsPolygonContains(x, y)) //_polygonQuadraticBezier.Contains(x, y)
            {
                _hitResult = HitResult.QuadraticBezier;
                return true;
            }
            _hitResult = HitResult.None;
            return false;
        }

        public void Move(double dx, double dy)
        {
            //Debug.Print("_hitResult: {0}", _hitResult);
            switch (_hitResult)
            {
                case HitResult.Start:
                    {
                        double x = _quadraticBezier.Start.X - dx;
                        double y = _quadraticBezier.Start.Y - dy;
                        _quadraticBezier.Start.X = _canvas.EnableSnap ? _canvas.Snap(x, _canvas.SnapX) : x;
                        _quadraticBezier.Start.Y = _canvas.EnableSnap ? _canvas.Snap(y, _canvas.SnapY) : y;
                        _quadraticBezier.Start = _quadraticBezier.Start;
                    }
                    break;
                case HitResult.Point1:
                    {
                        double x1 = _quadraticBezier.Point1.X - dx;
                        double y1 = _quadraticBezier.Point1.Y - dy;
                        _quadraticBezier.Point1.X = _canvas.EnableSnap ? _canvas.Snap(x1, _canvas.SnapX) : x1;
                        _quadraticBezier.Point1.Y = _canvas.EnableSnap ? _canvas.Snap(y1, _canvas.SnapY) : y1;
                        _quadraticBezier.Point1 = _quadraticBezier.Point1;
                    }
                    break;
                case HitResult.Point2:
                    {
                        double x2 = _quadraticBezier.Point2.X - dx;
                        double y2 = _quadraticBezier.Point2.Y - dy;
                        _quadraticBezier.Point2.X = _canvas.EnableSnap ? _canvas.Snap(x2, _canvas.SnapX) : x2;
                        _quadraticBezier.Point2.Y = _canvas.EnableSnap ? _canvas.Snap(y2, _canvas.SnapY) : y2;
                        _quadraticBezier.Point2 = _quadraticBezier.Point2;
                    }
                    break;
                case HitResult.QuadraticBezier:
                    {
                        double x = _quadraticBezier.Start.X - dx;
                        double y = _quadraticBezier.Start.Y - dy;
                        double x1 = _quadraticBezier.Point1.X - dx;
                        double y1 = _quadraticBezier.Point1.Y - dy;
                        double x2 = _quadraticBezier.Point2.X - dx;
                        double y2 = _quadraticBezier.Point2.Y - dy;
                        _quadraticBezier.Start.X = _canvas.EnableSnap ? _canvas.Snap(x, _canvas.SnapX) : x;
                        _quadraticBezier.Start.Y = _canvas.EnableSnap ? _canvas.Snap(y, _canvas.SnapY) : y;
                        _quadraticBezier.Point1.X = _canvas.EnableSnap ? _canvas.Snap(x1, _canvas.SnapX) : x1;
                        _quadraticBezier.Point1.Y = _canvas.EnableSnap ? _canvas.Snap(y1, _canvas.SnapY) : y1;
                        _quadraticBezier.Point2.X = _canvas.EnableSnap ? _canvas.Snap(x2, _canvas.SnapX) : x2;
                        _quadraticBezier.Point2.Y = _canvas.EnableSnap ? _canvas.Snap(y2, _canvas.SnapY) : y2;
                        _quadraticBezier.Start = _quadraticBezier.Start;
                        _quadraticBezier.Point1 = _quadraticBezier.Point1;
                        _quadraticBezier.Point2 = _quadraticBezier.Point2;
                    }
                    break;
            }
        }
    }

    public class ArcBounds : IBounds
    {
        private IArc _arc;
        private double _offset;
        private ICanvas _canvas;
        private IPolygon _polygon;
        private bool _isVisible;

        public ArcBounds(
            IModelToNativeConverter nativeConverter,
            ICanvasFactory canvasFactory,
            ICanvas canvas,
            IArc arc,
            double offset)
        {
            _arc = arc;
            _offset = offset;
            _canvas = canvas;

            _polygon = Helper.CreateBoundsPolygon(nativeConverter, canvasFactory, 4);
        }

        public void Update()
        {
            var ps = _polygon.Points;
            var ls = _polygon.Lines;

            double x = _arc.X;
            double y = _arc.Y;
            double width = _arc.Width;
            double height = _arc.Height;

            Helper.UpdateRectangleBounds(ps, ls, _offset, x, y, width, height);
        }

        public bool IsVisible()
        {
            return _isVisible;
        }

        public void Show()
        {
            if (!_isVisible)
            {
                foreach (var line in _polygon.Lines)
                {
                    _canvas.Add(line);
                }
                _isVisible = true;
            }
        }

        public void Hide()
        {
            if (_isVisible)
            {
                foreach (var line in _polygon.Lines)
                {
                    _canvas.Remove(line);
                }
                _isVisible = false;
            }
        }

        public bool Contains(double x, double y)
        {
            return _polygon.Contains(x, y);
        }

        public void Move(double dx, double dy)
        {
            throw new NotImplementedException();
        }
    }

    public class RectangleBounds : IBounds
    {
        private IRectangle _rectangle;
        private double _size;
        private double _offset;
        private ICanvas _canvas;
        private IPolygon _polygonRectangle;
        private IPolygon _polygonPoint1;
        private IPolygon _polygonPoint2;
        private bool _isVisible;

        private enum HitResult { None, Point1, Point2, Rectangle };
        private HitResult _hitResult;

        public RectangleBounds(
            IModelToNativeConverter nativeConverter,
            ICanvasFactory canvasFactory,
            ICanvas canvas,
            IRectangle rectangle,
            double size,
            double offset)
        {
            _rectangle = rectangle;
            _size = size;
            _offset = offset;
            _canvas = canvas;

            _hitResult = HitResult.None;

            InitBounds(nativeConverter, canvasFactory);
        }

        private void InitBounds(
            IModelToNativeConverter nativeConverter,
            ICanvasFactory canvasFactory)
        {
            _polygonPoint1 = Helper.CreateBoundsPolygon(nativeConverter, canvasFactory, 4);
            _polygonPoint2 = Helper.CreateBoundsPolygon(nativeConverter, canvasFactory, 4);
            _polygonRectangle = Helper.CreateBoundsPolygon(nativeConverter, canvasFactory, 4);
        }

        private void UpdatePoint1Bounds()
        {
            var ps = _polygonPoint1.Points;
            var ls = _polygonPoint1.Lines;
            Helper.UpdatePointBounds(_rectangle.Point1, ps, ls, _size, _offset);
        }

        private void UpdatePoint2Bounds()
        {
            var ps = _polygonPoint2.Points;
            var ls = _polygonPoint2.Lines;
            Helper.UpdatePointBounds(_rectangle.Point2, ps, ls, _size, _offset);
        }

        private void UpdateRectangleBounds()
        {
            var ps = _polygonRectangle.Points;
            var ls = _polygonRectangle.Lines;
            var p1 = _rectangle.Point1;
            var p2 = _rectangle.Point2;

            double x = Math.Min(p1.X, p2.X);
            double y = Math.Min(p1.Y, p2.Y);
            double width = Math.Abs(p2.X - p1.X);
            double height = Math.Abs(p2.Y - p1.Y);

            Helper.UpdateRectangleBounds(ps, ls, _offset, x, y, width, height);
        }

        public void Update()
        {
            UpdatePoint1Bounds();
            UpdatePoint2Bounds();
            UpdateRectangleBounds();
        }

        public bool IsVisible()
        {
            return _isVisible;
        }

        public void Show()
        {
            if (!_isVisible)
            {
                foreach (var line in _polygonRectangle.Lines)
                {
                    _canvas.Add(line);
                }
                foreach (var line in _polygonPoint1.Lines)
                {
                    _canvas.Add(line);
                }
                foreach (var line in _polygonPoint2.Lines)
                {
                    _canvas.Add(line);
                }
                _isVisible = true;
            }
        }

        public void Hide()
        {
            if (_isVisible)
            {
                foreach (var line in _polygonRectangle.Lines)
                {
                    _canvas.Remove(line);
                }
                foreach (var line in _polygonPoint1.Lines)
                {
                    _canvas.Remove(line);
                }
                foreach (var line in _polygonPoint2.Lines)
                {
                    _canvas.Remove(line);
                }
                _isVisible = false;
            }
        }

        public bool Contains(double x, double y)
        {
            if (_polygonPoint1.Contains(x, y))
            {
                _hitResult = HitResult.Point1;
                return true;
            }
            else if (_polygonPoint2.Contains(x, y))
            {
                _hitResult = HitResult.Point2;
                return true;
            }
            else if (_polygonRectangle.Contains(x, y))
            {
                _hitResult = HitResult.Rectangle;
                return true;
            }
            _hitResult = HitResult.None;
            return false;
        }

        public void Move(double dx, double dy)
        {
            //Debug.Print("_hitResult: {0}", _hitResult);
            switch (_hitResult)
            {
                case HitResult.Point1:
                    {
                        double x1 = _rectangle.Point1.X - dx;
                        double y1 = _rectangle.Point1.Y - dy;
                        _rectangle.Point1.X = _canvas.EnableSnap ? _canvas.Snap(x1, _canvas.SnapX) : x1;
                        _rectangle.Point1.Y = _canvas.EnableSnap ? _canvas.Snap(y1, _canvas.SnapY) : y1;
                        _rectangle.Point1 = _rectangle.Point1;
                    }
                    break;
                case HitResult.Point2:
                    {
                        double x2 = _rectangle.Point2.X - dx;
                        double y2 = _rectangle.Point2.Y - dy;
                        _rectangle.Point2.X = _canvas.EnableSnap ? _canvas.Snap(x2, _canvas.SnapX) : x2;
                        _rectangle.Point2.Y = _canvas.EnableSnap ? _canvas.Snap(y2, _canvas.SnapY) : y2;
                        _rectangle.Point2 = _rectangle.Point2;
                    }
                    break;
                case HitResult.Rectangle:
                    {
                        double x1 = _rectangle.Point1.X - dx;
                        double y1 = _rectangle.Point1.Y - dy;
                        double x2 = _rectangle.Point2.X - dx;
                        double y2 = _rectangle.Point2.Y - dy;
                        _rectangle.Point1.X = _canvas.EnableSnap ? _canvas.Snap(x1, _canvas.SnapX) : x1;
                        _rectangle.Point1.Y = _canvas.EnableSnap ? _canvas.Snap(y1, _canvas.SnapY) : y1;
                        _rectangle.Point2.X = _canvas.EnableSnap ? _canvas.Snap(x2, _canvas.SnapX) : x2;
                        _rectangle.Point2.Y = _canvas.EnableSnap ? _canvas.Snap(y2, _canvas.SnapY) : y2;
                        _rectangle.Point1 = _rectangle.Point1;
                        _rectangle.Point2 = _rectangle.Point2;
                    }
                    break;
            }
        }
    }

    public class EllipseBounds : IBounds
    {
        private IEllipse _ellipse;
        private double _size;
        private double _offset;
        private ICanvas _canvas;
        private IPolygon _polygonEllipse;
        private IPolygon _polygonPoint1;
        private IPolygon _polygonPoint2;
        private bool _isVisible;

        private enum HitResult { None, Point1, Point2, Ellipse };
        private HitResult _hitResult;

        public EllipseBounds(
            IModelToNativeConverter nativeConverter,
            ICanvasFactory canvasFactory,
            ICanvas canvas,
            IEllipse ellipse,
            double size,
            double offset)
        {
            _ellipse = ellipse;
            _size = size;
            _offset = offset;
            _canvas = canvas;

            _hitResult = HitResult.None;

            InitBounds(nativeConverter, canvasFactory);
        }

        private void InitBounds(
            IModelToNativeConverter nativeConverter,
            ICanvasFactory canvasFactory)
        {
            _polygonPoint1 = Helper.CreateBoundsPolygon(nativeConverter, canvasFactory, 4);
            _polygonPoint2 = Helper.CreateBoundsPolygon(nativeConverter, canvasFactory, 4);
            _polygonEllipse = Helper.CreateBoundsPolygon(nativeConverter, canvasFactory, 4);
        }

        private void UpdatePoint1Bounds()
        {
            var ps = _polygonPoint1.Points;
            var ls = _polygonPoint1.Lines;
            Helper.UpdatePointBounds(_ellipse.Point1, ps, ls, _size, _offset);
        }

        private void UpdatePoint2Bounds()
        {
            var ps = _polygonPoint2.Points;
            var ls = _polygonPoint2.Lines;
            Helper.UpdatePointBounds(_ellipse.Point2, ps, ls, _size, _offset);
        }

        private void UpdateRectangleBounds()
        {
            var ps = _polygonEllipse.Points;
            var ls = _polygonEllipse.Lines;
            var p1 = _ellipse.Point1;
            var p2 = _ellipse.Point2;

            double x = Math.Min(p1.X, p2.X);
            double y = Math.Min(p1.Y, p2.Y);
            double width = Math.Abs(p2.X - p1.X);
            double height = Math.Abs(p2.Y - p1.Y);

            Helper.UpdateRectangleBounds(ps, ls, _offset, x, y, width, height);
        }

        public void Update()
        {
            UpdatePoint1Bounds();
            UpdatePoint2Bounds();
            UpdateRectangleBounds();
        }

        public bool IsVisible()
        {
            return _isVisible;
        }

        public void Show()
        {
            if (!_isVisible)
            {
                foreach (var line in _polygonEllipse.Lines)
                {
                    _canvas.Add(line);
                }
                foreach (var line in _polygonPoint1.Lines)
                {
                    _canvas.Add(line);
                }
                foreach (var line in _polygonPoint2.Lines)
                {
                    _canvas.Add(line);
                }
                _isVisible = true;
            }
        }

        public void Hide()
        {
            if (_isVisible)
            {
                foreach (var line in _polygonEllipse.Lines)
                {
                    _canvas.Remove(line);
                }
                foreach (var line in _polygonPoint1.Lines)
                {
                    _canvas.Remove(line);
                }
                foreach (var line in _polygonPoint2.Lines)
                {
                    _canvas.Remove(line);
                }
                _isVisible = false;
            }
        }

        public bool Contains(double x, double y)
        {
            if (_polygonPoint1.Contains(x, y))
            {
                _hitResult = HitResult.Point1;
                return true;
            }
            else if (_polygonPoint2.Contains(x, y))
            {
                _hitResult = HitResult.Point2;
                return true;
            }
            else if (_polygonEllipse.Contains(x, y))
            {
                _hitResult = HitResult.Ellipse;
                return true;
            }
            _hitResult = HitResult.None;
            return false;
        }

        public void Move(double dx, double dy)
        {
            //Debug.Print("_hitResult: {0}", _hitResult);
            switch (_hitResult)
            {
                case HitResult.Point1:
                    {
                        double x1 = _ellipse.Point1.X - dx;
                        double y1 = _ellipse.Point1.Y - dy;
                        _ellipse.Point1.X = _canvas.EnableSnap ? _canvas.Snap(x1, _canvas.SnapX) : x1;
                        _ellipse.Point1.Y = _canvas.EnableSnap ? _canvas.Snap(y1, _canvas.SnapY) : y1;
                        _ellipse.Point1 = _ellipse.Point1;
                    }
                    break;
                case HitResult.Point2:
                    {
                        double x2 = _ellipse.Point2.X - dx;
                        double y2 = _ellipse.Point2.Y - dy;
                        _ellipse.Point2.X = _canvas.EnableSnap ? _canvas.Snap(x2, _canvas.SnapX) : x2;
                        _ellipse.Point2.Y = _canvas.EnableSnap ? _canvas.Snap(y2, _canvas.SnapY) : y2;
                        _ellipse.Point2 = _ellipse.Point2;
                    }
                    break;
                case HitResult.Ellipse:
                    {
                        double x1 = _ellipse.Point1.X - dx;
                        double y1 = _ellipse.Point1.Y - dy;
                        double x2 = _ellipse.Point2.X - dx;
                        double y2 = _ellipse.Point2.Y - dy;
                        _ellipse.Point1.X = _canvas.EnableSnap ? _canvas.Snap(x1, _canvas.SnapX) : x1;
                        _ellipse.Point1.Y = _canvas.EnableSnap ? _canvas.Snap(y1, _canvas.SnapY) : y1;
                        _ellipse.Point2.X = _canvas.EnableSnap ? _canvas.Snap(x2, _canvas.SnapX) : x2;
                        _ellipse.Point2.Y = _canvas.EnableSnap ? _canvas.Snap(y2, _canvas.SnapY) : y2;
                        _ellipse.Point1 = _ellipse.Point1;
                        _ellipse.Point2 = _ellipse.Point2;
                    }
                    break;
            }
        }
    }

    public class TextBounds : IBounds
    {
        private IText _text;
        private double _offset;
        private ICanvas _canvas;
        private IPolygon _polygon;
        private bool _isVisible;

        public TextBounds(
            IModelToNativeConverter nativeConverter,
            ICanvasFactory canvasFactory,
            ICanvas canvas,
            IText text,
            double offset)
        {
            _text = text;
            _offset = offset;
            _canvas = canvas;

            _polygon = Helper.CreateBoundsPolygon(nativeConverter, canvasFactory, 4);
        }

        public void Update()
        {
            var ps = _polygon.Points;
            var ls = _polygon.Lines;

            double x = _text.X;
            double y = _text.Y;
            double width = _text.Width;
            double height = _text.Height;

            Helper.UpdateRectangleBounds(ps, ls, _offset, x, y, width, height);
        }

        public bool IsVisible()
        {
            return _isVisible;
        }

        public void Show()
        {
            if (!_isVisible)
            {
                foreach (var line in _polygon.Lines)
                {
                    _canvas.Add(line);
                }
                _isVisible = true;
            }
        }

        public void Hide()
        {
            if (_isVisible)
            {
                foreach (var line in _polygon.Lines)
                {
                    _canvas.Remove(line);
                }
                _isVisible = false;
            }
        }

        public bool Contains(double x, double y)
        {
            return _polygon.Contains(x, y);
        }

        public void Move(double dx, double dy)
        {
            throw new NotImplementedException();
        }
    }

    public class BoundsFactory : IBoundsFactory
    {
        private readonly IModelToNativeConverter _nativeConverter;
        private readonly ICanvasFactory _canvasFactory;

        public BoundsFactory(IModelToNativeConverter nativeConverter, ICanvasFactory canvasFactory)
        {
            _nativeConverter = nativeConverter;
            _canvasFactory = canvasFactory;
        }

        public IBounds Create(ICanvas canvas, IPoint point)
        {
            // TODO: Create PointBounds class.
            throw new NotImplementedException();
        }

        public IBounds Create(ICanvas canvas, ILine line)
        {
            return new LineBounds(_nativeConverter, _canvasFactory, canvas, line, 15.0, 0.0);
        }

        public IBounds Create(ICanvas canvas, IBezier bezier)
        {
            return new BezierBounds(_nativeConverter, _canvasFactory, canvas, bezier, 15.0, 0.0);
        }

        public IBounds Create(ICanvas canvas, IQuadraticBezier quadraticBezier)
        {
            return new QuadraticBezierBounds(_nativeConverter, _canvasFactory, canvas, quadraticBezier, 15.0, 0.0);
        }

        public IBounds Create(ICanvas canvas, IArc arc)
        {
            return new ArcBounds(_nativeConverter, _canvasFactory, canvas, arc, 5.0);
        }

        public IBounds Create(ICanvas canvas, IRectangle rectangle)
        {
            return new RectangleBounds(_nativeConverter, _canvasFactory, canvas, rectangle, 0.0, 7.5);
        }

        public IBounds Create(ICanvas canvas, IEllipse ellipse)
        {
            return new EllipseBounds(_nativeConverter, _canvasFactory, canvas, ellipse, 0.0, 7.5);
        }

        public IBounds Create(ICanvas canvas, IText text)
        {
            return new TextBounds(_nativeConverter, _canvasFactory, canvas, text, 5.0);
        }
    }
}
