﻿using RxCanvas.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RxCanvas.Bounds
{
    internal static class Helper
    {
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
            polygon.Points = new IPoint[4];
            polygon.Lines = new ILine[4];

            for (int i = 0; i < 4; i++)
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
            var size = _size;
            var offset = _offset;

            double x = _line.Point1.X - (_size / 2.0);
            double y = _line.Point1.Y - (_size / 2.0);
            double width = _size;
            double height = _size;

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

            Move(ls[0], ps[0], ps[1]);
            Move(ls[1], ps[1], ps[2]);
            Move(ls[2], ps[2], ps[3]);
            Move(ls[3], ps[3], ps[0]);
        }

        private void UpdatePoint2Bounds()
        {
            var ps = _polygonPoint2.Points;
            var ls = _polygonPoint2.Lines;
            var size = _size;
            var offset = _offset;

            double x = _line.Point2.X - (_size / 2.0);
            double y = _line.Point2.Y - (_size / 2.0);
            double width = _size;
            double height = _size;

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

            Move(ls[0], ps[0], ps[1]);
            Move(ls[1], ps[1], ps[2]);
            Move(ls[2], ps[2], ps[3]);
            Move(ls[3], ps[3], ps[0]);
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

            Move(ls[0], ps[0], ps[1]);
            Move(ls[1], ps[1], ps[2]);
            Move(ls[2], ps[2], ps[3]);
            Move(ls[3], ps[3], ps[0]);
        }

        public void Update()
        {
            UpdatePoint1Bounds();
            UpdatePoint2Bounds();
            UpdateLineBounds();
        }

        private void Move(ILine line, IPoint point1, IPoint point2)
        {
            line.Point1 = point1;
            line.Point2 = point2;
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
            var offset = _offset;

            double x = _rectangle.X;
            double y = _rectangle.Y;
            double width = _rectangle.Width;
            double height = _rectangle.Height;

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

            Move(ls[0], ps[0], ps[1]);
            Move(ls[1], ps[1], ps[2]);
            Move(ls[2], ps[2], ps[3]);
            Move(ls[3], ps[3], ps[0]);
        }

        private void Move(ILine line, IPoint point1, IPoint point2)
        {
            line.Point1 = point1;
            line.Point2 = point2;
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
            var offset = _offset;

            double x = _ellipse.X;
            double y = _ellipse.Y;
            double width = _ellipse.Width;
            double height = _ellipse.Height;

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

            Move(ls[0], ps[0], ps[1]);
            Move(ls[1], ps[1], ps[2]);
            Move(ls[2], ps[2], ps[3]);
            Move(ls[3], ps[3], ps[0]);
        }

        private void Move(ILine line, IPoint point1, IPoint point2)
        {
            line.Point1 = point1;
            line.Point2 = point2;
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
            var offset = _offset;

            double x = _text.X;
            double y = _text.Y;
            double width = _text.Width;
            double height = _text.Height;

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

            Move(ls[0], ps[0], ps[1]);
            Move(ls[1], ps[1], ps[2]);
            Move(ls[2], ps[2], ps[3]);
            Move(ls[3], ps[3], ps[0]);
        }

        private void Move(ILine line, IPoint point1, IPoint point2)
        {
            line.Point1 = point1;
            line.Point2 = point2;
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
            // TODO: Create BezierBounds class.
            throw new NotImplementedException();
        }

        public IBounds Create(ICanvas canvas, IQuadraticBezier quadraticBezier)
        {
            // TODO: Create QuadraticBezierBounds class.
            throw new NotImplementedException();
        }

        public IBounds Create(ICanvas canvas, IArc arc)
        {
            // TODO: Create ArcBounds class.
            throw new NotImplementedException();
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
