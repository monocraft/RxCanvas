using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Diagnostics;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Joins;
using System.Reactive.Linq;
using System.Reactive.PlatformServices;
using System.Reactive.Subjects;
using System.Reactive.Threading;

namespace RxCanvas
{
    #region I

    public struct ImmutablePoint
    {
        public double X { get; private set; }
        public double Y { get; private set; }
        public ImmutablePoint(double x, double y)
            : this()
        {
            X = x;
            Y = y;
        }
    }

    public interface IColor
    {
        byte A { get; set; }
        byte R { get; set; }
        byte G { get; set; }
        byte B { get; set; }
    }

    public interface IPoint
    {
        double X { get; set; }
        double Y { get; set; }
    }

    public interface INative
    {
        object Native { get; set; }
    }

    public interface ILine : INative
    {
        double X1 { get; set; }
        double Y1 { get; set; }
        double X2 { get; set; }
        double Y2 { get; set; }
        IColor Stroke { get; set; }
        double StrokeThickness { get; set; }
    }

    public interface IQuadraticBezier : INative
    {
        IPoint Start { get; set; }
        IPoint Point1 { get; set; }
        IPoint Point2 { get; set; }
        IColor Fill { get; set; }
        IColor Stroke { get; set; }
        double StrokeThickness { get; set; }
        bool IsClosed { get; set; }
    }

    public interface IRectangle : INative
    {
        double X { get; set; }
        double Y { get; set; }
        double Width { get; set; }
        double Height { get; set; }
        IColor Stroke { get; set; }
        double StrokeThickness { get; set; }
        IColor Fill { get; set; }
        bool IsFilled { get; set; }
    }

    public interface IEllipse : INative
    {
        double X { get; set; }
        double Y { get; set; }
        double Width { get; set; }
        double Height { get; set; }
        IColor Stroke { get; set; }
        double StrokeThickness { get; set; }
        IColor Fill { get; set; }
        bool IsFilled { get; set; }
    }

    public interface ICanvas : INative
    {
        IObservable<ImmutablePoint> Downs { get; set; }
        IObservable<ImmutablePoint> Ups { get; set; }
        IObservable<ImmutablePoint> Moves { get; set; }

        bool IsCaptured { get; }

        void Capture();
        void ReleaseCapture();

        void Add(INative value);
        void Remove(INative value);
    }

    public interface IEditor
    {
        bool IsEnabled { get; set; }
    }

    #endregion

    #region X

    public class XColor : IColor
    {
        public byte A { get; set; }
        public byte R { get; set; }
        public byte G { get; set; }
        public byte B { get; set; }
        public XColor(byte a, byte r, byte g, byte b)
        {
            A = a;
            R = r;
            G = g;
            B = b;
        }
    }

    public class XPoint : IPoint
    {
        public double X { get; set; }
        public double Y { get; set; }
        public XPoint(double x, double y)
        {
            X = x;
            Y = y;
        }
    }

    public abstract class XNative : INative
    {
        public object Native { get; set; }
    }

    public class XLine : XNative, ILine
    {
        public double X1 { get; set; }
        public double Y1 { get; set; }
        public double X2 { get; set; }
        public double Y2 { get; set; }
        public IColor Stroke { get; set; }
        public double StrokeThickness { get; set; }
    }

    public class XQuadraticBezier : XNative, IQuadraticBezier
    {
        public IPoint Start { get; set; }
        public IPoint Point1 { get; set; }
        public IPoint Point2 { get; set; }
        public IColor Fill { get; set; }
        public IColor Stroke { get; set; }
        public double StrokeThickness { get; set; }
        public bool IsClosed { get; set; }
    }

    public class XRectangle : XNative, IRectangle
    {
        public double X { get; set; }
        public double Y { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }
        public IColor Stroke { get; set; }
        public double StrokeThickness { get; set; }
        public IColor Fill { get; set; }
        public bool IsFilled { get; set; }
    }

    public class XEllipse : XNative, IEllipse
    {
        public double X { get; set; }
        public double Y { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }
        public IColor Stroke { get; set; }
        public double StrokeThickness { get; set; }
        public IColor Fill { get; set; }
        public bool IsFilled { get; set; }
    }

    #endregion

    #region Editors

    public class PortableXCanvasLineEditor : IEditor, IDisposable
    {
        public enum State { None, Start, End, }

        public bool IsEnabled { get; set; }

        private ICanvas _canvas;
        private ILine _xline;
        private ILine _line;
        private State _state = State.None;
        private IDisposable _downs;
        private IDisposable _drag;

        public PortableXCanvasLineEditor(ICanvas canvas)
        {
            _canvas = canvas;

            var dragMoves = from move in _canvas.Moves
                            where _canvas.IsCaptured
                            select move;

            var allPositions = Observable.Merge(_canvas.Downs, _canvas.Ups, dragMoves);

            var dragPositions = from move in allPositions
                                select move;

            _downs = _canvas.Downs.Subscribe(p =>
            {
                if (!IsEnabled)
                {
                    return;
                }

                if (_canvas.IsCaptured)
                {
                    _xline.X2 = p.X;
                    _xline.Y2 = p.Y;
                    _line.X2 = _xline.X2;
                    _line.Y2 = _xline.Y2;
                    _state = State.None;
                    _canvas.ReleaseCapture();
                }
                else
                {
                    // TODO: Use IoC container to get XLine as ILine.
                    _xline = new XLine()
                    {
                        X1 = p.X,
                        Y1 = p.Y,
                        X2 = p.X,
                        Y2 = p.Y,
                        Stroke = new XColor(0xFF, 0x00, 0x00, 0x00),
                        StrokeThickness = 2.0,
                    };
                    // TODO: Use IoC container to get WpfLine as ILine.
                    _line = new WpfLine(_xline);
                    _canvas.Add(_line);
                    _canvas.Capture();
                    _state = State.End;
                }
            });

            _drag = dragPositions.Subscribe(p =>
            {
                if (!IsEnabled)
                {
                    return;
                }

                if (_state == State.End)
                {
                    _xline.X2 = p.X;
                    _xline.Y2 = p.Y;
                    _line.X2 = _xline.X2;
                    _line.Y2 = _xline.Y2;
                }
            });
        }

        public void Dispose()
        {
            _downs.Dispose();
            _drag.Dispose();
        }
    }

    public class PortableXQuadraticBezierEditor : IEditor, IDisposable
    {
        public enum State { None, Start, Point1, Point2 }

        public bool IsEnabled { get; set; }

        private ICanvas _canvas;
        private IQuadraticBezier _xqb;
        private IQuadraticBezier _qb;
        private State _state = State.None;
        private IDisposable _downs;
        private IDisposable _drag;

        public PortableXQuadraticBezierEditor(ICanvas canvas)
        {
            _canvas = canvas;

            var dragMoves = from move in _canvas.Moves
                            where _canvas.IsCaptured
                            select move;

            var allPositions = Observable.Merge(_canvas.Downs, _canvas.Ups, dragMoves);

            var dragPositions = from move in allPositions
                                select move;

            _downs = _canvas.Downs.Subscribe(p =>
            {
                if (!IsEnabled)
                {
                    return;
                }

                if (_canvas.IsCaptured)
                {
                    switch (_state)
                    {
                        case State.Start:
                            {
                                _xqb.Point2.X = p.X;
                                _xqb.Point2.Y = p.Y;
                                _qb.Point2 = _xqb.Point2;
                                _state = State.Point1;
                            }
                            break;
                        case State.Point1:
                            {
                                _xqb.Point1.X = p.X;
                                _xqb.Point1.Y = p.Y;
                                _qb.Point1 = _xqb.Point1;
                                _state = State.None;
                                _canvas.ReleaseCapture();
                            }
                            break;
                    }
                }
                else
                {
                    // TODO: Use IoC container to get XQuadraticBezier as IQuadraticBezier.
                    _xqb = new XQuadraticBezier()
                    {
                        Start = new XPoint(p.X, p.Y),
                        Point1 = new XPoint(p.X, p.Y),
                        Point2 = new XPoint(p.X, p.Y),
                        Fill = new XColor(0x00, 0xFF, 0xFF, 0xFF),
                        Stroke = new XColor(0xFF, 0x00, 0x00, 0x00),
                        StrokeThickness = 2.0,
                        IsClosed = false
                    };
                    // TODO: Use IoC container to get WpfQuadraticBezier as IQuadraticBezier.
                    _qb = new WpfQuadraticBezier(_xqb);
                    _canvas.Add(_qb);
                    _canvas.Capture();
                    _state = State.Start;
                }
            });

            _drag = dragPositions.Subscribe(p =>
            {
                if (!IsEnabled)
                {
                    return;
                }

                if (_state == State.Start)
                {
                    _xqb.Point2.X = p.X;
                    _xqb.Point2.Y = p.Y;
                    _qb.Point2 = _xqb.Point2;
                }
                else if (_state == State.Point1)
                {
                    _xqb.Point1.X = p.X;
                    _xqb.Point1.Y = p.Y;
                    _qb.Point1 = _xqb.Point1;
                }
            });
        }

        public void Dispose()
        {
            _downs.Dispose();
            _drag.Dispose();
        }
    }

    public class PortableXCanvasRectangleEditor : IEditor, IDisposable
    {
        public enum State { None, TopLeft, TopRight, BottomLeft, BottomRight }

        public bool IsEnabled { get; set; }

        private ICanvas _canvas;
        private IRectangle _xrectangle;
        private IRectangle _rectangle;
        private State _state = State.None;
        private IDisposable _downs;
        private IDisposable _drag;
        private ImmutablePoint _start;

        public PortableXCanvasRectangleEditor(ICanvas canvas)
        {
            _canvas = canvas;

            var dragMoves = from move in _canvas.Moves
                            where _canvas.IsCaptured
                            select move;

            var allPositions = Observable.Merge(_canvas.Downs, _canvas.Ups, dragMoves);

            var dragPositions = from move in allPositions
                                select move;

            _downs = _canvas.Downs.Subscribe(p =>
            {
                if (!IsEnabled)
                {
                    return;
                }

                if (_canvas.IsCaptured)
                {
                    UpdatePositionAndSize(p);
                    _state = State.None;
                    _canvas.ReleaseCapture();
                }
                else
                {
                    _start = p;
                    // TODO: Use IoC container to get XRectangle as IRectangle.
                    _xrectangle = new XRectangle()
                    {
                        X = p.X,
                        Y = p.Y,
                        Width = 0.0,
                        Height = 0.0,
                        Stroke = new XColor(0xFF, 0x00, 0x00, 0x00),
                        StrokeThickness = 2.0,
                        Fill = new XColor(0x00, 0xFF, 0xFF, 0xFF),
                        IsFilled = false
                    };
                    // TODO: Use IoC container to get WpfRectangle as IRectangle.
                    _rectangle = new WpfRectangle(_xrectangle);
                    _canvas.Add(_rectangle);
                    _canvas.Capture();
                    _state = State.BottomRight;
                }
            });

            _drag = dragPositions.Subscribe(p =>
            {
                if (!IsEnabled)
                {
                    return;
                }

                if (_state == State.BottomRight)
                {
                    UpdatePositionAndSize(p);
                }
            });
        }

        private void UpdatePositionAndSize(ImmutablePoint p)
        {
            double width = Math.Abs(p.X - _start.X);
            double height = Math.Abs(p.Y - _start.Y);
            _xrectangle.X = Math.Min(_start.X, p.X);
            _xrectangle.Y = Math.Min(_start.Y, p.Y);
            _xrectangle.Width = width;
            _xrectangle.Height = height;
            _rectangle.X = _xrectangle.X;
            _rectangle.Y = _xrectangle.Y;
            _rectangle.Width = _xrectangle.Width;
            _rectangle.Height = _xrectangle.Height;
        }

        public void Dispose()
        {
            _downs.Dispose();
            _drag.Dispose();
        }
    }

    #endregion

    #region WPF

    public class WpfLine : XNative, ILine
    {
        private SolidColorBrush _strokeBrush;
        private Line _line;

        public WpfLine(ILine line)
        {
            _strokeBrush = new SolidColorBrush(Color.FromArgb(line.Stroke.A, line.Stroke.R, line.Stroke.G, line.Stroke.B));
            _strokeBrush.Freeze();

            _line = new Line()
            {
                X1 = line.X1,
                Y1 = line.Y1,
                X2 = line.X2,
                Y2 = line.Y2,
                Stroke = _strokeBrush,
                StrokeThickness = line.StrokeThickness
            };

            Native = _line;
        }

        public double X1
        {
            get { return (Native as Line).X1; }
            set
            {
                (Native as Line).X1 = value;
            }
        }

        public double Y1
        {
            get { return (Native as Line).Y1; }
            set { (Native as Line).Y1 = value; }
        }

        public double X2
        {
            get { return (Native as Line).X2; }
            set { (Native as Line).X2 = value; }
        }

        public double Y2
        {
            get { return (Native as Line).Y2; }
            set { (Native as Line).Y2 = value; }
        }

        public IColor Stroke
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }

        public double StrokeThickness
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }
    }

    public class WpfQuadraticBezier : XNative, IQuadraticBezier
    {
        private SolidColorBrush _fillBrush;
        private SolidColorBrush _strokeBrush;
        private Path _path;
        private PathGeometry _pg;
        private PathFigure _pf;
        private QuadraticBezierSegment _qbs;

        private IPoint _start;
        private IPoint _point1;
        private IPoint _point2;

        public WpfQuadraticBezier(IQuadraticBezier qb)
        {
            _fillBrush = new SolidColorBrush(Color.FromArgb(qb.Fill.A, qb.Fill.R, qb.Fill.G, qb.Fill.B));
            _fillBrush.Freeze();
            _strokeBrush = new SolidColorBrush(Color.FromArgb(qb.Stroke.A, qb.Stroke.R, qb.Stroke.G, qb.Stroke.B));
            _strokeBrush.Freeze();
            _path = new Path();
            _path.Tag = this;
            _path.Fill = _fillBrush;
            _path.Stroke = _strokeBrush;
            _path.StrokeThickness = qb.StrokeThickness;
            _pg = new PathGeometry();
            _pf = new PathFigure();
            _pf.StartPoint = new Point(qb.Start.X, qb.Start.Y);
            _pf.IsClosed = qb.IsClosed;
            _qbs = new QuadraticBezierSegment();
            _qbs.Point1 = new Point(qb.Point1.X, qb.Point1.Y);
            _qbs.Point2 = new Point(qb.Point2.X, qb.Point2.Y);
            _pf.Segments.Add(_qbs);
            _pg.Figures.Add(_pf);
            _path.Data = _pg;
            Native = _path;
        }

        private void SetStart(double x, double y)
        {
            _pf.StartPoint = new Point(x, y);
        }

        private void SetPoint1(double x, double y)
        {
            _qbs.Point1 = new Point(x, y);
        }

        private void SetPoint2(double x, double y)
        {
            _qbs.Point2 = new Point(x, y);
        }

        private double GetStartX()
        {
            return _pf.StartPoint.X;
        }

        private double GetStartY()
        {
            return _pf.StartPoint.Y;
        }

        private double GetPoint1X()
        {
            return _qbs.Point1.X;
        }

        private double GetPoint1Y()
        {
            return _qbs.Point1.Y;
        }

        private double GetPoint2X()
        {
            return _qbs.Point2.X;
        }

        private double GetPoint2Y()
        {
            return _qbs.Point2.Y;
        }

        public IPoint Start
        {
            get { return _start; }
            set
            {
                _start = value;
                SetStart(_start.X, _start.Y);
            }
        }

        public IPoint Point1
        {
            get { return _point1; }
            set
            {
                _point1 = value;
                SetPoint1(_point1.X, _point1.Y);
            }
        }

        public IPoint Point2
        {
            get { return _point2; }
            set
            {
                _point2 = value;
                SetPoint2(_point2.X, _point2.Y);
            }
        }

        public IColor Fill
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }

        public IColor Stroke
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }

        public double StrokeThickness
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }

        public bool IsClosed
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }
    }

    public class WpfRectangle : XNative, IRectangle
    {
        private SolidColorBrush _strokeBrush;
        private SolidColorBrush _fillBrush;
        private Rectangle _rectangle;

        public WpfRectangle(IRectangle rectangle)
        {
            _strokeBrush = new SolidColorBrush(Color.FromArgb(rectangle.Stroke.A, rectangle.Stroke.R, rectangle.Stroke.G, rectangle.Stroke.B));
            _strokeBrush.Freeze();
            _fillBrush = new SolidColorBrush(Color.FromArgb(rectangle.Fill.A, rectangle.Fill.R, rectangle.Fill.G, rectangle.Fill.B));
            _fillBrush.Freeze();

            _rectangle = new Rectangle()
            {
                Width = rectangle.Width,
                Height = rectangle.Height,
                Stroke = _strokeBrush,
                StrokeThickness = rectangle.StrokeThickness,
                Fill = _fillBrush
            };

            Canvas.SetLeft(_rectangle, rectangle.X);
            Canvas.SetTop(_rectangle, rectangle.Y);

            Native = _rectangle;
        }

        public double X
        {
            get { return Canvas.GetLeft(Native as Rectangle); }
            set
            {
                Canvas.SetLeft(Native as Rectangle, value);
            }
        }

        public double Y
        {
            get { return Canvas.GetTop(Native as Rectangle); }
            set
            {
                Canvas.SetTop(Native as Rectangle, value);
            }
        }

        public double Width
        {
            get { return (Native as Rectangle).Width; }
            set { (Native as Rectangle).Width = value; }
        }

        public double Height
        {
            get { return (Native as Rectangle).Height; }
            set { (Native as Rectangle).Height = value; }
        }

        public IColor Stroke
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }

        public double StrokeThickness
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }

        public IColor Fill
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }

        public bool IsFilled
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }
    }

    public class WpfCanvas : XNative, ICanvas
    {
        public IObservable<ImmutablePoint> Downs { get; set; }
        public IObservable<ImmutablePoint> Ups { get; set; }
        public IObservable<ImmutablePoint> Moves { get; set; }

        public WpfCanvas()
        {
            Native = new Canvas()
            {
                Background = Brushes.Transparent
            };

            Downs = Observable.FromEventPattern<MouseButtonEventArgs>((Native as Canvas), "PreviewMouseLeftButtonDown").Select(e =>
            {
                var p = e.EventArgs.GetPosition((Native as Canvas));
                return new ImmutablePoint(p.X, p.Y);
            });

            Ups = Observable.FromEventPattern<MouseButtonEventArgs>((Native as Canvas), "PreviewMouseLeftButtonUp").Select(e =>
            {
                var p = e.EventArgs.GetPosition((Native as Canvas));
                return new ImmutablePoint(p.X, p.Y);
            });

            Moves = Observable.FromEventPattern<MouseEventArgs>((Native as Canvas), "PreviewMouseMove").Select(e =>
            {
                var p = e.EventArgs.GetPosition((Native as Canvas));
                return new ImmutablePoint(p.X, p.Y);
            });
        }

        public bool IsCaptured
        {
            get
            {
                return Mouse.Captured == (Native as Canvas);
            }
        }

        public void Capture()
        {
            (Native as Canvas).CaptureMouse();
        }

        public void ReleaseCapture()
        {
            (Native as Canvas).ReleaseMouseCapture();
        }

        public void Add(INative value)
        {
            (Native as Canvas).Children.Add(value.Native as UIElement);
        }

        public void Remove(INative value)
        {
            (Native as Canvas).Children.Remove(value.Native as UIElement);
        }
    }

    #endregion

    public partial class MainWindow : Window
    {
        ICanvas _canvas;
        IEditor _lineEditor;
        IEditor _quadraticBezierEditor;
        IEditor _rectangleEditor;

        public MainWindow()
        {
            InitializeComponent();

            _canvas = new WpfCanvas();
            Layout.Children.Add(_canvas.Native as UIElement);

            _lineEditor = new PortableXCanvasLineEditor(_canvas)
            {
                IsEnabled = false
            };

            _quadraticBezierEditor = new PortableXQuadraticBezierEditor(_canvas)
            {
                IsEnabled = true
            };

            _rectangleEditor = new PortableXCanvasRectangleEditor(_canvas)
            {
                IsEnabled = false
            };

            PreviewKeyDown += (sender, e) =>
            {
                switch (e.Key)
                {
                    case Key.L:
                        _lineEditor.IsEnabled = true;
                        _quadraticBezierEditor.IsEnabled = false;
                        _rectangleEditor.IsEnabled = false;
                        break;
                    case Key.R:
                        _lineEditor.IsEnabled = false;
                        _quadraticBezierEditor.IsEnabled = false;
                        _rectangleEditor.IsEnabled = true;
                        break;
                    case Key.Q:
                        _lineEditor.IsEnabled = false;
                        _quadraticBezierEditor.IsEnabled = true;
                        _rectangleEditor.IsEnabled = false;
                        break;
                }
            };


        }
    }
}
