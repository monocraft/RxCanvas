using RxCanvas.Interfaces;
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
            polygon.Points = new IPoint[PointBoundVertexCount];
            polygon.Lines = new ILine[PointBoundVertexCount];

            for (int i = 0; i < PointBoundVertexCount; i++)
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
            var offset = _offset;

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
            return _polygonLine.Contains(x, y)
                || _polygonPoint1.Contains(x, y)
                || _polygonPoint2.Contains(x, y);
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

        private void UpdateBezierBounds()
        {
            // TODO:
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
            return _polygonBezier.Contains(x, y)
                || _polygonStart.Contains(x, y)
                || _polygonPoint1.Contains(x, y)
                || _polygonPoint2.Contains(x, y)
                || _polygonPoint3.Contains(x, y);
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
            _polygonQuadraticBezier = Helper.CreateBoundsPolygon(nativeConverter, canvasFactory, 4);
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

        private void UpdateBezierBounds()
        {
            // TODO:
        }

        public void Update()
        {
            UpdateStartBounds();
            UpdatePoint1Bounds();
            UpdatePoint2Bounds();
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
            return _polygonQuadraticBezier.Contains(x, y)
                || _polygonStart.Contains(x, y)
                || _polygonPoint1.Contains(x, y)
                || _polygonPoint2.Contains(x, y);
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
    }

    public class RectangleBounds : IBounds
    {
        private IRectangle _rectangle;
        private double _offset;
        private ICanvas _canvas;
        private IPolygon _polygon;
        private bool _isVisible;

        public RectangleBounds(
            IModelToNativeConverter nativeConverter,
            ICanvasFactory canvasFactory,
            ICanvas canvas,
            IRectangle rectangle,
            double offset)
        {
            _rectangle = rectangle;
            _offset = offset;
            _canvas = canvas;

            _polygon = Helper.CreateBoundsPolygon(nativeConverter, canvasFactory, 4);
        }

        public void Update()
        {
            var ps = _polygon.Points;
            var ls = _polygon.Lines;

            double x = _rectangle.X;
            double y = _rectangle.Y;
            double width = _rectangle.Width;
            double height = _rectangle.Height;

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
    }

    public class EllipseBounds : IBounds
    {
        private IEllipse _ellipse;
        private double _offset;
        private ICanvas _canvas;
        private IPolygon _polygon;
        private bool _isVisible;

        public EllipseBounds(
            IModelToNativeConverter nativeConverter,
            ICanvasFactory canvasFactory,
            ICanvas canvas,
            IEllipse ellipse,
            double offset)
        {
            _ellipse = ellipse;
            _offset = offset;
            _canvas = canvas;

            _polygon = Helper.CreateBoundsPolygon(nativeConverter, canvasFactory, 4);
        }

        public void Update()
        {
            var ps = _polygon.Points;
            var ls = _polygon.Lines;

            double x = _ellipse.X;
            double y = _ellipse.Y;
            double width = _ellipse.Width;
            double height = _ellipse.Height;

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
            return new RectangleBounds(_nativeConverter, _canvasFactory, canvas, rectangle, 5.0);
        }

        public IBounds Create(ICanvas canvas, IEllipse ellipse)
        {
            return new EllipseBounds(_nativeConverter, _canvasFactory, canvas, ellipse, 5.0);
        }

        public IBounds Create(ICanvas canvas, IText text)
        {
            return new TextBounds(_nativeConverter, _canvasFactory, canvas, text, 5.0);
        }
    }
}
