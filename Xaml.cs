using RxCanvas.Interfaces;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace RxCanvas.Xaml
{
    public class WpfLine : ILine
    {
        public object Native { get; set; }
        public IBounds Bounds { get; set; }

        private SolidColorBrush _strokeBrush;
        private IColor _stroke;
        private Line _line;
        private IPoint _point1;
        private IPoint _point2;

        public WpfLine(ILine line)
        {
            _stroke = line.Stroke;
            _point1 = line.Point1;
            _point2 = line.Point2;

            _strokeBrush = new SolidColorBrush(Color.FromArgb(_stroke.A, _stroke.R, _stroke.G, _stroke.B));
            _strokeBrush.Freeze();

            _line = new Line()
            {
                X1 = _point1.X,
                Y1 = _point1.Y,
                X2 = _point2.X,
                Y2 = _point2.Y,
                Stroke = _strokeBrush,
                StrokeThickness = line.StrokeThickness
            };

            Native = _line;
        }

        public IPoint Point1
        {
            get { return _point1; }
            set
            {
                _point1 = value;
                _line.X1 = _point1.X;
                _line.Y1 = _point1.Y;
            }
        }

        public IPoint Point2
        {
            get { return _point2; }
            set
            {
                _point2 = value;
                _line.X2 = _point2.X;
                _line.Y2 = _point2.Y;
            }
        }

        public IColor Stroke
        {
            get { return _stroke; }
            set
            {
                _stroke = value;
                _strokeBrush = new SolidColorBrush(Color.FromArgb(_stroke.A, _stroke.R, _stroke.G, _stroke.B));
                _strokeBrush.Freeze();
                _line.Stroke = _strokeBrush;
            }
        }

        public double StrokeThickness
        {
            get { return _line.StrokeThickness; }
            set { _line.StrokeThickness = value; }
        }
    }

    public class WpfBezier : IBezier
    {
        public object Native { get; set; }
        public IBounds Bounds { get; set; }

        private SolidColorBrush _fillBrush;
        private SolidColorBrush _strokeBrush;
        private Path _path;
        private PathGeometry _pg;
        private PathFigure _pf;
        private BezierSegment _bs;
        private IColor _fill;
        private IColor _stroke;
        private IPoint _start;
        private IPoint _point1;
        private IPoint _point2;
        private IPoint _point3;

        public WpfBezier(IBezier b)
        {
            _fill = b.Fill;
            _stroke = b.Stroke;
            _start = b.Start;
            _point1 = b.Point1;
            _point2 = b.Point2;
            _point3 = b.Point3;

            _fillBrush = new SolidColorBrush(Color.FromArgb(_fill.A, _fill.R, _fill.G, _fill.B));
            _fillBrush.Freeze();
            _strokeBrush = new SolidColorBrush(Color.FromArgb(_stroke.A, _stroke.R, _stroke.G, _stroke.B));
            _strokeBrush.Freeze();

            _path = new Path();
            _path.Tag = this;
            _path.Fill = _fillBrush;
            _path.Stroke = _strokeBrush;
            _path.StrokeThickness = b.StrokeThickness;
            _pg = new PathGeometry();
            _pf = new PathFigure();
            _pf.StartPoint = new Point(b.Start.X, b.Start.Y);
            _pf.IsFilled = b.IsFilled;
            _pf.IsClosed = b.IsClosed;
            _bs = new BezierSegment();
            _bs.Point1 = new Point(b.Point1.X, b.Point1.Y);
            _bs.Point2 = new Point(b.Point2.X, b.Point2.Y);
            _bs.Point3 = new Point(b.Point3.X, b.Point3.Y);
            _pf.Segments.Add(_bs);
            _pg.Figures.Add(_pf);
            _path.Data = _pg;

            Native = _path;
        }

        public IPoint Start
        {
            get { return _start; }
            set
            {
                _start = value;
                _pf.StartPoint = new Point(_start.X, _start.Y);
            }
        }

        public IPoint Point1
        {
            get { return _point1; }
            set
            {
                _point1 = value;
                _bs.Point1 = new Point(_point1.X, _point1.Y);
            }
        }

        public IPoint Point2
        {
            get { return _point2; }
            set
            {
                _point2 = value;
                _bs.Point2 = new Point(_point2.X, _point2.Y);
            }
        }

        public IPoint Point3
        {
            get { return _point3; }
            set
            {
                _point3 = value;
                _bs.Point3 = new Point(_point3.X, _point3.Y);
            }
        }

        public IColor Fill
        {
            get { return _fill; }
            set
            {
                _fill = value;
                _fillBrush = new SolidColorBrush(Color.FromArgb(_fill.A, _fill.R, _fill.G, _fill.B));
                _fillBrush.Freeze();
                _path.Fill = _fillBrush;
            }
        }

        public IColor Stroke
        {
            get { return _stroke; }
            set
            {
                _stroke = value;
                _strokeBrush = new SolidColorBrush(Color.FromArgb(_stroke.A, _stroke.R, _stroke.G, _stroke.B));
                _strokeBrush.Freeze();
                _path.Stroke = _strokeBrush;
            }
        }

        public double StrokeThickness
        {
            get { return _path.StrokeThickness; }
            set { _path.StrokeThickness = value; }
        }

        public bool IsFilled
        {
            get { return _pf.IsFilled; }
            set { _pf.IsFilled = value; }
        }

        public bool IsClosed
        {
            get { return _pf.IsClosed; }
            set { _pf.IsClosed = value; }
        }
    }

    public class WpfQuadraticBezier : IQuadraticBezier
    {
        public object Native { get; set; }
        public IBounds Bounds { get; set; }

        private SolidColorBrush _fillBrush;
        private SolidColorBrush _strokeBrush;
        private Path _path;
        private PathGeometry _pg;
        private PathFigure _pf;
        private QuadraticBezierSegment _qbs;
        private IColor _fill;
        private IColor _stroke;
        private IPoint _start;
        private IPoint _point1;
        private IPoint _point2;

        public WpfQuadraticBezier(IQuadraticBezier qb)
        {
            _fill = qb.Fill;
            _stroke = qb.Stroke;
            _start = qb.Start;
            _point1 = qb.Point1;
            _point2 = qb.Point2;

            _fillBrush = new SolidColorBrush(Color.FromArgb(_fill.A, _fill.R, _fill.G, _fill.B));
            _fillBrush.Freeze();
            _strokeBrush = new SolidColorBrush(Color.FromArgb(_stroke.A, _stroke.R, _stroke.G, _stroke.B));
            _strokeBrush.Freeze();

            _path = new Path();
            _path.Tag = this;
            _path.Fill = _fillBrush;
            _path.Stroke = _strokeBrush;
            _path.StrokeThickness = qb.StrokeThickness;
            _pg = new PathGeometry();
            _pf = new PathFigure();
            _pf.StartPoint = new Point(qb.Start.X, qb.Start.Y);
            _pf.IsFilled = qb.IsFilled;
            _pf.IsClosed = qb.IsClosed;
            _qbs = new QuadraticBezierSegment();
            _qbs.Point1 = new Point(qb.Point1.X, qb.Point1.Y);
            _qbs.Point2 = new Point(qb.Point2.X, qb.Point2.Y);
            _pf.Segments.Add(_qbs);
            _pg.Figures.Add(_pf);
            _path.Data = _pg;

            Native = _path;
        }

        public IPoint Start
        {
            get { return _start; }
            set
            {
                _start = value;
                _pf.StartPoint = new Point(_start.X, _start.Y);
            }
        }

        public IPoint Point1
        {
            get { return _point1; }
            set
            {
                _point1 = value;
                _qbs.Point1 = new Point(_point1.X, _point1.Y);
            }
        }

        public IPoint Point2
        {
            get { return _point2; }
            set
            {
                _point2 = value;
                _qbs.Point2 = new Point(_point2.X, _point2.Y);
            }
        }

        public IColor Fill
        {
            get { return _fill; }
            set
            {
                _fill = value;
                _fillBrush = new SolidColorBrush(Color.FromArgb(_fill.A, _fill.R, _fill.G, _fill.B));
                _fillBrush.Freeze();
                _path.Fill = _fillBrush;
            }
        }

        public IColor Stroke
        {
            get { return _stroke; }
            set
            {
                _stroke = value;
                _strokeBrush = new SolidColorBrush(Color.FromArgb(_stroke.A, _stroke.R, _stroke.G, _stroke.B));
                _strokeBrush.Freeze();
                _path.Stroke = _strokeBrush;
            }
        }

        public double StrokeThickness
        {
            get { return _path.StrokeThickness; }
            set { _path.StrokeThickness = value; }
        }

        public bool IsFilled
        {
            get { return _pf.IsFilled; }
            set { _pf.IsFilled = value; }
        }

        public bool IsClosed
        {
            get { return _pf.IsClosed; }
            set { _pf.IsClosed = value; }
        }
    }

    public class WpfArc : IArc
    {
        public object Native { get; set; }
        public IBounds Bounds { get; set; }

        private SolidColorBrush _fillBrush;
        private SolidColorBrush _strokeBrush;
        private Path _path;
        private PathGeometry _pg;
        private PathFigure _pf;
        private ArcSegment _as;
        private Point _start;
        private IColor _fill;
        private IColor _stroke;
        private IArc _source;

        public WpfArc(IArc arc)
        {
            _source = arc;
            _fill = arc.Fill;
            _stroke = arc.Stroke;

            _fillBrush = new SolidColorBrush(Color.FromArgb(_fill.A, _fill.R, _fill.G, _fill.B));
            _fillBrush.Freeze();
            _strokeBrush = new SolidColorBrush(Color.FromArgb(_stroke.A, _stroke.R, _stroke.G, _stroke.B));
            _strokeBrush.Freeze();

            _path = new Path();
            _path.Tag = this;
            _path.Fill = _fillBrush;
            _path.Stroke = _strokeBrush;
            _path.StrokeThickness = arc.StrokeThickness;
            _pg = new PathGeometry();
            _pf = new PathFigure();
            _pf.IsFilled = arc.IsFilled;
            _pf.IsClosed = arc.IsClosed;
            _start = new Point();
            _as = new ArcSegment();
            SetArcSegment(_as, arc, out _start);
            _pf.StartPoint = _start;
            _pf.Segments.Add(_as);
            _pg.Figures.Add(_pf);
            _path.Data = _pg;

            Native = _path;
        }

        public const double Deg2Rad = Math.PI / 180;
        public const double πHalf = Math.PI / 2;

        private void SetArcSegment(ArcSegment segment, IArc arc, out Point startPoint)
        {
            // original code
            // https://pdfsharp.codeplex.com/SourceControl/latest#PDFsharp/code/PdfSharp/PdfSharp.Internal/Calc.cs
            // https://pdfsharp.codeplex.com/SourceControl/latest#PDFsharp/code/PdfSharp/PdfSharp.Drawing/GeometryHelper.cs

            double x = Math.Min(arc.Point1.X, arc.Point2.X);
            double y = Math.Min(arc.Point1.Y, arc.Point2.Y);
            double width = Math.Abs(arc.Point2.X - arc.Point1.X);
            double height = Math.Abs(arc.Point2.Y - arc.Point1.Y);
            double startAngle = arc.StartAngle;
            double sweepAngle = arc.SweepAngle;

            // normalize the angles
            double α = startAngle;
            if (α < 0)
            {
                α = α + (1 + Math.Floor((Math.Abs(α) / 360))) * 360;
            }
            else if (α > 360)
            {
                α = α - Math.Floor(α / 360) * 360;
            }

            Debug.Assert(α >= 0 && α <= 360);

            if (Math.Abs(sweepAngle) >= 360)
            {
                sweepAngle = Math.Sign(sweepAngle) * 360;
            }

            double β = startAngle + sweepAngle;
            if (β < 0)
            {
                β = β + (1 + Math.Floor((Math.Abs(β) / 360))) * 360;
            }
            else if (β > 360)
            {
                β = β - Math.Floor(β / 360) * 360;
            }

            if (α == 0 && β < 0)
            {
                α = 360;
            }
            else if (α == 360 && β > 0)
            {
                α = 0;
            }

            // scanling factor
            double δx = width / 2;
            double δy = height / 2;
            // center of ellipse
            double x0 = x + δx;
            double y0 = y + δy;
            double cosα, cosβ, sinα, sinβ;

            if (width == height)
            {
                // circular arc needs no correction.
                α = α * Deg2Rad;
                β = β * Deg2Rad;
            }
            else
            {
                // elliptic arc needs the angles to be adjusted such that the scaling transformation is compensated.
                α = α * Deg2Rad;
                sinα = Math.Sin(α);
                if (Math.Abs(sinα) > 1E-10)
                {
                    if (α < Math.PI)
                    {
                        α = Math.PI / 2 - Math.Atan(δy * Math.Cos(α) / (δx * sinα));
                    }
                    else
                    {
                        α = 3 * Math.PI / 2 - Math.Atan(δy * Math.Cos(α) / (δx * sinα));
                    }
                }
                // α = πHalf - Math.Atan(δy * Math.Cos(α) / (δx * sinα));
                β = β * Deg2Rad;
                sinβ = Math.Sin(β);
                if (Math.Abs(sinβ) > 1E-10)
                {
                    if (β < Math.PI)
                    {
                        β = Math.PI / 2 - Math.Atan(δy * Math.Cos(β) / (δx * sinβ));
                    }
                    else
                    {
                        β = 3 * Math.PI / 2 - Math.Atan(δy * Math.Cos(β) / (δx * sinβ));
                    }
                }
                // β = πHalf - Math.Atan(δy * Math.Cos(β) / (δx * sinβ));
            }

            sinα = Math.Sin(α);
            cosα = Math.Cos(α);
            sinβ = Math.Sin(β);
            cosβ = Math.Cos(β);

            startPoint = new Point(x0 + δx * cosα, y0 + δy * sinα);
            var destPoint = new Point(x0 + δx * cosβ, y0 + δy * sinβ);
            var size = new Size(δx, δy);
            bool isLargeArc = Math.Abs(sweepAngle) >= 180;
            SweepDirection sweepDirection = sweepAngle > 0 ? SweepDirection.Clockwise : SweepDirection.Counterclockwise;
            bool isStroked = true;

            segment.Point = destPoint;
            segment.Size = size;
            segment.RotationAngle = 0.0;
            segment.IsLargeArc = isLargeArc;
            segment.SweepDirection = sweepDirection;
            segment.IsStroked = isStroked;
        }

        public IPoint Point1
        {
            get { return _source.Point1; }
            set
            {
                _source.Point1 = value;
                Update();
            }
        }

        public IPoint Point2
        {
            get { return _source.Point2; }
            set
            {
                _source.Point2 = value;
                Update();
            }
        }

        private void Update()
        {
            SetArcSegment(_as, _source, out _start);
            _pf.StartPoint = _start;
        }

        public double StartAngle
        {
            get { return _source.StartAngle; }
            set 
            {
                _source.StartAngle = value;
                SetArcSegment(_as, _source, out _start);
                _pf.StartPoint = _start;
            }
        }

        public double SweepAngle
        {
            get { return _source.SweepAngle; }
            set 
            {
                _source.SweepAngle = value;
                SetArcSegment(_as, _source, out _start);
                _pf.StartPoint = _start;
            }
        }

        public IColor Stroke
        {
            get { return _stroke; }
            set
            {
                _stroke = value;
                _strokeBrush = new SolidColorBrush(Color.FromArgb(_stroke.A, _stroke.R, _stroke.G, _stroke.B));
                _strokeBrush.Freeze();
                _path.Stroke = _strokeBrush;
            }
        }

        public double StrokeThickness
        {
            get { return _path.StrokeThickness; }
            set { _path.StrokeThickness = value; }
        }

        public IColor Fill
        {
            get { return _fill; }
            set
            {
                _fill = value;
                _fillBrush = new SolidColorBrush(Color.FromArgb(_fill.A, _fill.R, _fill.G, _fill.B));
                _fillBrush.Freeze();
                _path.Fill = _fillBrush;
            }
        }

        public bool IsFilled
        {
            get { return _pf.IsFilled; }
            set { _pf.IsFilled = value; }
        }

        public bool IsClosed
        {
            get { return _pf.IsClosed; }
            set { _pf.IsClosed = value; }
        }
    }

    public class WpfRectangle : IRectangle
    {
        public object Native { get; set; }
        public IBounds Bounds { get; set; }

        private SolidColorBrush _strokeBrush;
        private SolidColorBrush _fillBrush;
        private Rectangle _rectangle;
        private IColor _stroke;
        private IColor _fill;
        private IPoint _point1;
        private IPoint _point2;

        public WpfRectangle(IRectangle rectangle)
        {
            _stroke = rectangle.Stroke;
            _fill = rectangle.Fill;
            _point1 = rectangle.Point1;
            _point2 = rectangle.Point2;

            _strokeBrush = new SolidColorBrush(Color.FromArgb(_stroke.A, _stroke.R, _stroke.G, _stroke.B));
            _strokeBrush.Freeze();
            _fillBrush = new SolidColorBrush(Color.FromArgb(_fill.A, _fill.R, _fill.G, _fill.B));
            _fillBrush.Freeze();

            _rectangle = new Rectangle()
            {
                Stroke = _strokeBrush,
                StrokeThickness = rectangle.StrokeThickness,
                Fill = _fillBrush
            };

            Update();

            Native = _rectangle;
        }

        public IPoint Point1
        {
            get { return _point1; }
            set
            {
                _point1 = value;
                Update();
            }
        }

        public IPoint Point2
        {
            get { return _point2; }
            set
            {
                _point2 = value;
                Update();
            }
        }

        private void Update()
        {
            double x = Math.Min(_point1.X, _point2.X);
            double y = Math.Min(_point1.Y, _point2.Y);
            double width = Math.Abs(_point2.X - _point1.X);
            double height = Math.Abs(_point2.Y - _point1.Y);
            Canvas.SetLeft(_rectangle, x - 1.0);
            Canvas.SetTop(_rectangle, y - 1.0);
            _rectangle.Width = width + 2.0;
            _rectangle.Height = height + 2.0;
        }

        public IColor Stroke
        {
            get { return _stroke; }
            set
            {
                _stroke = value;
                _strokeBrush = new SolidColorBrush(Color.FromArgb(_stroke.A, _stroke.R, _stroke.G, _stroke.B));
                _strokeBrush.Freeze();
                _rectangle.Stroke = _strokeBrush;
            }
        }

        public double StrokeThickness
        {
            get { return _rectangle.StrokeThickness; }
            set { _rectangle.StrokeThickness = value; }
        }

        public IColor Fill
        {
            get { return _fill; }
            set
            {
                _fill = value;
                _fillBrush = new SolidColorBrush(Color.FromArgb(_fill.A, _fill.R, _fill.G, _fill.B));
                _fillBrush.Freeze();
                _rectangle.Fill = _fillBrush;
            }
        }
    }

    public class WpfEllipse : IEllipse
    {
        public object Native { get; set; }
        public IBounds Bounds { get; set; }

        private SolidColorBrush _strokeBrush;
        private SolidColorBrush _fillBrush;
        private Ellipse _ellipse;
        private IColor _stroke;
        private IColor _fill;
        private IPoint _point1;
        private IPoint _point2;

        public WpfEllipse(IEllipse ellipse)
        {
            _stroke = ellipse.Stroke;
            _fill = ellipse.Fill;
            _point1 = ellipse.Point1;
            _point2 = ellipse.Point2;

            _strokeBrush = new SolidColorBrush(Color.FromArgb(_stroke.A, _stroke.R, _stroke.G, _stroke.B));
            _strokeBrush.Freeze();
            _fillBrush = new SolidColorBrush(Color.FromArgb(_fill.A, _fill.R, _fill.G, _fill.B));
            _fillBrush.Freeze();

            _ellipse = new Ellipse()
            {
                Stroke = _strokeBrush,
                StrokeThickness = ellipse.StrokeThickness,
                Fill = _fillBrush
            };

            Update();

            Native = _ellipse;
        }

        public IPoint Point1
        {
            get { return _point1; }
            set
            {
                _point1 = value;
                Update();
            }
        }

        public IPoint Point2
        {
            get { return _point2; }
            set
            {
                _point2 = value;
                Update();
            }
        }

        private void Update()
        {
            double x = Math.Min(_point1.X, _point2.X);
            double y = Math.Min(_point1.Y, _point2.Y);
            double width = Math.Abs(_point2.X - _point1.X);
            double height = Math.Abs(_point2.Y - _point1.Y);
            Canvas.SetLeft(_ellipse, x - 1.0);
            Canvas.SetTop(_ellipse, y - 1.0);
            _ellipse.Width = width + 2.0;
            _ellipse.Height = height + 2.0;
        }

        public IColor Stroke
        {
            get { return _stroke; }
            set
            {
                _stroke = value;
                _strokeBrush = new SolidColorBrush(Color.FromArgb(_stroke.A, _stroke.R, _stroke.G, _stroke.B));
                _strokeBrush.Freeze();
                _ellipse.Stroke = _strokeBrush;
            }
        }

        public double StrokeThickness
        {
            get { return _ellipse.StrokeThickness; }
            set { _ellipse.StrokeThickness = value; }
        }

        public IColor Fill
        {
            get { return _fill; }
            set
            {
                _fill = value;
                _fillBrush = new SolidColorBrush(Color.FromArgb(_fill.A, _fill.R, _fill.G, _fill.B));
                _fillBrush.Freeze();
                _ellipse.Fill = _fillBrush;
            }
        }
    }

    public class WpfText : IText
    {
        public object Native { get; set; }
        public IBounds Bounds { get; set; }

        private SolidColorBrush _foregroundBrush;
        private SolidColorBrush _backgroundBrush;
        private Grid _grid;
        private TextBlock _tb;
        private IColor _foreground;
        private IColor _background;
        private IPoint _point1;
        private IPoint _point2;

        public WpfText(IText text)
        {
            _foreground = text.Foreground;
            _background = text.Backgroud;
            _point1 = text.Point1;
            _point2 = text.Point2;

            _foregroundBrush = new SolidColorBrush(Color.FromArgb(_foreground.A, _foreground.R, _foreground.G, _foreground.B));
            _foregroundBrush.Freeze();
            _backgroundBrush = new SolidColorBrush(Color.FromArgb(_background.A, _background.R, _background.G, _background.B));
            _backgroundBrush.Freeze();

            _grid = new Grid();
            _grid.Background = _backgroundBrush;

            _tb = new TextBlock();
            _tb.HorizontalAlignment = (HorizontalAlignment)text.HorizontalAlignment;
            _tb.VerticalAlignment = (VerticalAlignment)text.VerticalAlignment;
            _tb.Background = _backgroundBrush;
            _tb.Foreground = _foregroundBrush;
            _tb.FontSize = text.Size;
            _tb.FontFamily = new FontFamily("Calibri");
            _tb.Text = text.Text;

            _grid.Children.Add(_tb);

            Update();

            Native = _grid;
        }

        public IPoint Point1
        {
            get { return _point1; }
            set
            {
                _point1 = value;
                Update();
            }
        }

        public IPoint Point2
        {
            get { return _point2; }
            set
            {
                _point2 = value;
                Update();
            }
        }

        private void Update()
        {
            double x = Math.Min(_point1.X, _point2.X);
            double y = Math.Min(_point1.Y, _point2.Y);
            double width = Math.Abs(_point2.X - _point1.X);
            double height = Math.Abs(_point2.Y - _point1.Y);
            Canvas.SetLeft(_grid, x - 1.0);
            Canvas.SetTop(_grid, y - 1.0);
            _grid.Width = width + 2.0;
            _grid.Height = height + 2.0;
        }

        public int HorizontalAlignment
        {
            get { return (int)_tb.HorizontalAlignment; }
            set { _tb.HorizontalAlignment = (HorizontalAlignment)value; }
        }

        public int VerticalAlignment
        {
            get { return (int)_tb.VerticalAlignment; }
            set { _tb.VerticalAlignment = (VerticalAlignment)value; }
        }

        public double Size
        {
            get { return _tb.FontSize; }
            set { _tb.FontSize = value; }
        }

        public string Text
        {
            get { return _tb.Text; }
            set { _tb.Text = value; }
        }

        public IColor Foreground
        {
            get { return _foreground; }
            set
            {
                _foreground = value;
                _foregroundBrush = new SolidColorBrush(Color.FromArgb(_foreground.A, _foreground.R, _foreground.G, _foreground.B));
                _foregroundBrush.Freeze();
                _tb.Foreground = _foregroundBrush;
            }
        }

        public IColor Backgroud
        {
            get { return _background; }
            set
            {
                _background = value;
                _backgroundBrush = new SolidColorBrush(Color.FromArgb(_background.A, _background.R, _background.G, _background.B));
                _backgroundBrush.Freeze();
                _grid.Background = _backgroundBrush;
                _tb.Background = _backgroundBrush;
            }
        }
    }

    public class WpfCanvas : ICanvas
    {
        public object Native { get; set; }
        public IBounds Bounds { get; set; }

        public IObservable<ImmutablePoint> Downs { get; set; }
        public IObservable<ImmutablePoint> Ups { get; set; }
        public IObservable<ImmutablePoint> Moves { get; set; }

        public IHistory History { get; set; }

        private SolidColorBrush _backgroundBrush;
        private IColor _background;
        private double _snapX;
        private double _snapY;
        private bool _enableSnap;
        private Canvas _canvas;

        public double Snap(double val, double snap)
        {
            double r = val % snap;
            return r >= snap / 2.0 ? val + snap - r : val - r;
        }

        public WpfCanvas(ICanvas canvas)
        {
            _background = canvas.Background;
            _snapX = canvas.SnapX;
            _snapY = canvas.SnapY;
            _enableSnap = canvas.EnableSnap;

            History = canvas.History;

            _backgroundBrush = new SolidColorBrush(Color.FromArgb(_background.A, _background.R, _background.G, _background.B));
            _backgroundBrush.Freeze();

            Children = new ObservableCollection<INative>();

            _canvas = new Canvas()
            {
                Width = canvas.Width,
                Height = canvas.Height,
                Background = _backgroundBrush
            };

            Downs = Observable.FromEventPattern<MouseButtonEventArgs>(_canvas, "PreviewMouseLeftButtonDown").Select(e =>
            {
                var p = e.EventArgs.GetPosition(_canvas);
                return new ImmutablePoint(_enableSnap ? Snap(p.X, _snapX) : p.X, _enableSnap ? Snap(p.Y, _snapY) : p.Y);
            });

            Ups = Observable.FromEventPattern<MouseButtonEventArgs>(_canvas, "PreviewMouseLeftButtonUp").Select(e =>
            {
                var p = e.EventArgs.GetPosition(_canvas);
                return new ImmutablePoint(_enableSnap ? Snap(p.X, _snapX) : p.X, _enableSnap ? Snap(p.Y, _snapY) : p.Y);
            });

            Moves = Observable.FromEventPattern<MouseEventArgs>(_canvas, "PreviewMouseMove").Select(e =>
            {
                var p = e.EventArgs.GetPosition(_canvas);
                return new ImmutablePoint(_enableSnap ? Snap(p.X, _snapX) : p.X, _enableSnap ? Snap(p.Y, _snapY) : p.Y);
            });

            Native = _canvas;
        }

        public IList<INative> Children { get; set; }

        public double Width
        {
            get { return _canvas.Width; }
            set { _canvas.Width = value; }
        }

        public double Height
        {
            get { return _canvas.Height; }
            set { _canvas.Height = value; }
        }

        public IColor Background
        {
            get { return _background; }
            set
            {
                _background = value;
                if (_background == null)
                {
                    _backgroundBrush = null;
                }
                else
                {
                    _backgroundBrush = new SolidColorBrush(Color.FromArgb(_background.A, _background.R, _background.G, _background.B));
                    _backgroundBrush.Freeze();
                }
                _canvas.Background = _backgroundBrush;
            }
        }

        public bool EnableSnap
        {
            get { return _enableSnap; }
            set { _enableSnap = value; }
        }

        public double SnapX
        {
            get { return _snapX; }
            set { _snapX = value; }
        }

        public double SnapY
        {
            get { return _snapY; }
            set { _snapY = value; }
        }

        public bool IsCaptured
        {
            get { return Mouse.Captured == _canvas; }
            set { _canvas.CaptureMouse(); }
        }

        public void Capture()
        {
            _canvas.CaptureMouse();
        }

        public void ReleaseCapture()
        {
            _canvas.ReleaseMouseCapture();
        }

        public void Add(INative value)
        {
            _canvas.Children.Add(value.Native as UIElement);
            Children.Add(value);
        }

        public void Remove(INative value)
        {
            _canvas.Children.Remove(value.Native as UIElement);
            Children.Remove(value);
        }

        public void Clear()
        {
            _canvas.Children.Clear();
            Children.Clear();
        }

        public void Render(INative context)
        {
        }
    }

    public class XModelToWpfConverter : IModelToNativeConverter
    {
        public ILine Convert(ILine line)
        {
            return new WpfLine(line);
        }

        public IBezier Convert(IBezier bezier)
        {
            return new WpfBezier(bezier);
        }

        public IQuadraticBezier Convert(IQuadraticBezier quadraticBezier)
        {
            return new WpfQuadraticBezier(quadraticBezier);
        }

        public IArc Convert(IArc arc)
        {
            return new WpfArc(arc);
        }

        public IRectangle Convert(IRectangle rectangle)
        {
            return new WpfRectangle(rectangle);
        }

        public IEllipse Convert(IEllipse ellipse)
        {
            return new WpfEllipse(ellipse);
        }

        public IText Convert(IText text)
        {
            return new WpfText(text);
        }

        public ICanvas Convert(ICanvas canvas)
        {
            return new WpfCanvas(canvas);
        }
    }
}
