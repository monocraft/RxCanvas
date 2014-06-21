using RxCanvas.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RxCanvas.Editors
{
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

            _polygon = canvasFactory.CreatePolygon();
            _polygon.Points = new IPoint[4];
            _polygon.Lines = new ILine[4];

            for (int i = 0; i < 4; i++)
            {
                _polygon.Points[i] = canvasFactory.CreatePoint();

                var _xline = canvasFactory.CreateLine();
                _xline.Stroke = canvasFactory.CreateColor();
                _xline.Stroke.A = 0xFF;
                _xline.Stroke.R = 0x00;
                _xline.Stroke.G = 0xBF;
                _xline.Stroke.B = 0xFF;
                _xline.StrokeThickness = 2.0;
                var _line = nativeConverter.Convert(_xline);
                _polygon.Lines[i] = _line;
            }
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
}
