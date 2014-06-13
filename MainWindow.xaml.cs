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

    public interface IBezier : INative
    {
        IPoint Start { get; set; }
        IPoint Point1 { get; set; }
        IPoint Point2 { get; set; }
        IPoint Point3 { get; set; }
        IColor Fill { get; set; }
        IColor Stroke { get; set; }
        double StrokeThickness { get; set; }
        bool IsClosed { get; set; }
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

    public interface IArc : INative
    {
        double X { get; set; }
        double Y { get; set; }
        double Width { get; set; }
        double Height { get; set; }
        double StartAngle { get; set; }
        double SweepAngle { get; set; }
        IColor Stroke { get; set; }
        double StrokeThickness { get; set; }
        IColor Fill { get; set; }
        bool IsFilled { get; set; }
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

        bool EnableSnap { get; set; }
        double SnapX { get; set; }
        double SnapY { get; set; }

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

    public class XBezier : XNative, IBezier
    {
        public IPoint Start { get; set; }
        public IPoint Point1 { get; set; }
        public IPoint Point2 { get; set; }
        public IPoint Point3 { get; set; }
        public IColor Fill { get; set; }
        public IColor Stroke { get; set; }
        public double StrokeThickness { get; set; }
        public bool IsClosed { get; set; }
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

    public class XArc : XNative, IArc
    {
        public double X { get; set; }
        public double Y { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }
        public double StartAngle { get; set; }
        public double SweepAngle { get; set; }
        public IColor Stroke { get; set; }
        public double StrokeThickness { get; set; }
        public IColor Fill { get; set; }
        public bool IsFilled { get; set; }
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

    public class PortableXLineEditor : IEditor, IDisposable
    {
        public enum State { None, Start, End }

        public bool IsEnabled { get; set; }

        private ICanvas _canvas;
        private ILine _xline;
        private ILine _line;
        private State _state = State.None;
        private IDisposable _downs;
        private IDisposable _drag;

        public PortableXLineEditor(ICanvas canvas)
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

    public class PortableXBezierEditor : IEditor, IDisposable
    {
        public enum State { None, Start, Point1, Point2, Point3 }

        public bool IsEnabled { get; set; }

        private ICanvas _canvas;
        private IBezier _xb;
        private IBezier _b;
        private State _state = State.None;
        private IDisposable _downs;
        private IDisposable _drag;

        public PortableXBezierEditor(ICanvas canvas)
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
                                _xb.Point3.X = p.X;
                                _xb.Point3.Y = p.Y;
                                _b.Point3 = _xb.Point3;
                                _xb.Point2.X = p.X;
                                _xb.Point2.Y = p.Y;
                                _b.Point2 = _xb.Point2;
                                _state = State.Point1;
                            }
                            break;
                        case State.Point1:
                            {
                                _xb.Point1.X = p.X;
                                _xb.Point1.Y = p.Y;
                                _b.Point1 = _xb.Point1;
                                _state = State.Point2;
                            }
                            break;
                        case State.Point2:
                            {
                                _xb.Point2.X = p.X;
                                _xb.Point2.Y = p.Y;
                                _b.Point2 = _xb.Point2;
                                _state = State.None;
                                _canvas.ReleaseCapture();
                            }
                            break;
                    }
                }
                else
                {
                    // TODO: Use IoC container to get XBezier as IBezier.
                    _xb = new XBezier()
                    {
                        Start = new XPoint(p.X, p.Y),
                        Point1 = new XPoint(p.X, p.Y),
                        Point2 = new XPoint(p.X, p.Y),
                        Point3 = new XPoint(p.X, p.Y),
                        Fill = new XColor(0x00, 0xFF, 0xFF, 0xFF),
                        Stroke = new XColor(0xFF, 0x00, 0x00, 0x00),
                        StrokeThickness = 2.0,
                        IsClosed = false
                    };
                    // TODO: Use IoC container to get WpfBezier as IBezier.
                    _b = new WpfBezier(_xb);
                    _canvas.Add(_b);
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
                    _xb.Point3.X = p.X;
                    _xb.Point3.Y = p.Y;
                    _b.Point3 = _xb.Point3;
                    _xb.Point2.X = p.X;
                    _xb.Point2.Y = p.Y;
                    _b.Point2 = _xb.Point2;
                }
                else if (_state == State.Point1)
                {
                    _xb.Point1.X = p.X;
                    _xb.Point1.Y = p.Y;
                    _b.Point1 = _xb.Point1;
                }
                else if (_state == State.Point2)
                {
                    _xb.Point2.X = p.X;
                    _xb.Point2.Y = p.Y;
                    _b.Point2 = _xb.Point2;
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

    public class PortableXArcEditor : IEditor, IDisposable
    {
        public enum State { None, Size, StartAngle, SweepAngle }

        public bool IsEnabled { get; set; }

        private ICanvas _canvas;
        private IArc _xarc;
        private IArc _arc;
        private State _state = State.None;
        private IDisposable _downs;
        private IDisposable _drag;
        private ImmutablePoint _start;

        public PortableXArcEditor(ICanvas canvas)
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
                    _start = new ImmutablePoint(p.X, p.Y);
                    // TODO: Use IoC container to get XArc as IArc.
                    _xarc = new XArc()
                    {
                        X = _start.X,
                        Y = _start.Y,
                        Width = 0.0,
                        Height = 0.0,
                        StartAngle = 180.0,
                        SweepAngle = 180.0,
                        Stroke = new XColor(0xFF, 0x00, 0x00, 0x00),
                        StrokeThickness = 2.0,
                        Fill = new XColor(0x00, 0xFF, 0xFF, 0xFF),
                        IsFilled = false
                    };
                    // TODO: Use IoC container to get WpfArc as IArc.
                    _arc = new WpfArc(_xarc);
                    _canvas.Add(_arc);
                    _canvas.Capture();
                    _state = State.Size;
                }
            });

            _drag = dragPositions.Subscribe(p =>
            {
                if (!IsEnabled)
                {
                    return;
                }

                if (_state == State.Size)
                {
                    UpdatePositionAndSize(p);
                }
            });
        }

        private void UpdatePositionAndSize(ImmutablePoint p)
        {
            double width = Math.Abs(p.X - _start.X);
            double height = Math.Abs(p.Y - _start.Y);
            _xarc.X = Math.Min(_start.X, p.X);
            _xarc.Y = Math.Min(_start.Y, p.Y);
            _xarc.Width = width;
            _xarc.Height = height;
            (_arc as WpfArc).Update(_xarc);
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
                    _start = new ImmutablePoint(p.X - 1.0, p.Y - 1.0);
                    // TODO: Use IoC container to get XRectangle as IRectangle.
                    _xrectangle = new XRectangle()
                    {
                        X = _start.X,
                        Y = _start.Y,
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
            _xrectangle.Width = width + 1.0;
            _xrectangle.Height = height + 1.0;
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

    public class PortableXCanvasEllipseEditor : IEditor, IDisposable
    {
        public enum State { None, TopLeft, TopRight, BottomLeft, BottomRight }

        public bool IsEnabled { get; set; }

        private ICanvas _canvas;
        private IEllipse _xellipse;
        private IEllipse _elllipse;
        private State _state = State.None;
        private IDisposable _downs;
        private IDisposable _drag;
        private ImmutablePoint _start;

        public PortableXCanvasEllipseEditor(ICanvas canvas)
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
                    _start = new ImmutablePoint(p.X - 1.0, p.Y - 1.0);
                    // TODO: Use IoC container to get XEllipse as IEllipse.
                    _xellipse = new XEllipse()
                    {
                        X = _start.X,
                        Y = _start.Y,
                        Width = 0.0,
                        Height = 0.0,
                        Stroke = new XColor(0xFF, 0x00, 0x00, 0x00),
                        StrokeThickness = 2.0,
                        Fill = new XColor(0x00, 0xFF, 0xFF, 0xFF),
                        IsFilled = false
                    };
                    // TODO: Use IoC container to get WpfEllipse as IEllipse.
                    _elllipse = new WpfEllipse(_xellipse);
                    _canvas.Add(_elllipse);
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
            _xellipse.X = Math.Min(_start.X, p.X);
            _xellipse.Y = Math.Min(_start.Y, p.Y);
            _xellipse.Width = width + 1.0;
            _xellipse.Height = height + 1.0;
            _elllipse.X = _xellipse.X;
            _elllipse.Y = _xellipse.Y;
            _elllipse.Width = _xellipse.Width;
            _elllipse.Height = _xellipse.Height;
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

    public class WpfBezier : XNative, IBezier
    {
        private SolidColorBrush _fillBrush;
        private SolidColorBrush _strokeBrush;
        private Path _path;
        private PathGeometry _pg;
        private PathFigure _pf;
        private BezierSegment _bs;

        private IPoint _start;
        private IPoint _point1;
        private IPoint _point2;
        private IPoint _point3;

        public WpfBezier(IBezier b)
        {
            _fillBrush = new SolidColorBrush(Color.FromArgb(b.Fill.A, b.Fill.R, b.Fill.G, b.Fill.B));
            _fillBrush.Freeze();
            _strokeBrush = new SolidColorBrush(Color.FromArgb(b.Stroke.A, b.Stroke.R, b.Stroke.G, b.Stroke.B));
            _strokeBrush.Freeze();
            _path = new Path();
            _path.Tag = this;
            _path.Fill = _fillBrush;
            _path.Stroke = _strokeBrush;
            _path.StrokeThickness = b.StrokeThickness;
            _pg = new PathGeometry();
            _pf = new PathFigure();
            _pf.StartPoint = new Point(b.Start.X, b.Start.Y);
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

        private void SetStart(double x, double y)
        {
            _pf.StartPoint = new Point(x, y);
        }

        private void SetPoint1(double x, double y)
        {
            _bs.Point1 = new Point(x, y);
        }

        private void SetPoint2(double x, double y)
        {
            _bs.Point2 = new Point(x, y);
        }

        private void SetPoint3(double x, double y)
        {
            _bs.Point3 = new Point(x, y);
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
            return _bs.Point1.X;
        }

        private double GetPoint1Y()
        {
            return _bs.Point1.Y;
        }

        private double GetPoint2X()
        {
            return _bs.Point2.X;
        }

        private double GetPoint2Y()
        {
            return _bs.Point2.Y;
        }

        private double GetPoint3X()
        {
            return _bs.Point3.X;
        }

        private double GetPoint3Y()
        {
            return _bs.Point3.Y;
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

        public IPoint Point3
        {
            get { return _point3; }
            set
            {
                _point3 = value;
                SetPoint3(_point3.X, _point3.Y);
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

    public class WpfArc : XNative, IArc
    {
        private SolidColorBrush _fillBrush;
        private SolidColorBrush _strokeBrush;
        private Path _path;
        private PathGeometry _pg;
        private PathFigure _pf;
        private ArcSegment _as;
        private Point _start;

        public WpfArc(IArc arc)
        {
            _fillBrush = new SolidColorBrush(Color.FromArgb(arc.Fill.A, arc.Fill.R, arc.Fill.G, arc.Fill.B));
            _fillBrush.Freeze();
            _strokeBrush = new SolidColorBrush(Color.FromArgb(arc.Stroke.A, arc.Stroke.R, arc.Stroke.G, arc.Stroke.B));
            _strokeBrush.Freeze();
            _path = new Path();
            _path.Tag = this;
            _path.Fill = _fillBrush;
            _path.Stroke = _strokeBrush;
            _path.StrokeThickness = arc.StrokeThickness;
            _pg = new PathGeometry();
            _pf = new PathFigure();
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

            double x = arc.X;
            double y = arc.Y;
            double width = arc.Width;
            double height = arc.Height;
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

        public void Update(IArc arc)
        {
            SetArcSegment(_as, arc, out _start);
            _pf.StartPoint = _start;
        }

        public double X
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }

        public double Y
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }

        public double Width
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }

        public double Height
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }

        public double StartAngle
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }

        public double SweepAngle
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

    public class WpfEllipse : XNative, IEllipse
    {
        private SolidColorBrush _strokeBrush;
        private SolidColorBrush _fillBrush;
        private Ellipse _ellipse;

        public WpfEllipse(IEllipse ellipse)
        {
            _strokeBrush = new SolidColorBrush(Color.FromArgb(ellipse.Stroke.A, ellipse.Stroke.R, ellipse.Stroke.G, ellipse.Stroke.B));
            _strokeBrush.Freeze();
            _fillBrush = new SolidColorBrush(Color.FromArgb(ellipse.Fill.A, ellipse.Fill.R, ellipse.Fill.G, ellipse.Fill.B));
            _fillBrush.Freeze();

            _ellipse = new Ellipse()
            {
                Width = ellipse.Width,
                Height = ellipse.Height,
                Stroke = _strokeBrush,
                StrokeThickness = ellipse.StrokeThickness,
                Fill = _fillBrush
            };

            Canvas.SetLeft(_ellipse, ellipse.X);
            Canvas.SetTop(_ellipse, ellipse.Y);

            Native = _ellipse;
        }

        public double X
        {
            get { return Canvas.GetLeft(Native as Ellipse); }
            set
            {
                Canvas.SetLeft(Native as Ellipse, value);
            }
        }

        public double Y
        {
            get { return Canvas.GetTop(Native as Ellipse); }
            set
            {
                Canvas.SetTop(Native as Ellipse, value);
            }
        }

        public double Width
        {
            get { return (Native as Ellipse).Width; }
            set { (Native as Ellipse).Width = value; }
        }

        public double Height
        {
            get { return (Native as Ellipse).Height; }
            set { (Native as Ellipse).Height = value; }
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

        private SolidColorBrush _backgroundBrush;
        private double _snapX = 15.0;
        private double _snapY = 15.0;
        private bool _enableSnap = true;

        public double Snap(double val, double snap)
        {
            double r = val % snap;
            return r >= snap / 2.0 ? val + snap - r : val - r;
        }

        public WpfCanvas(double width, double height, IColor backgroud)
        {
            _backgroundBrush = new SolidColorBrush(Color.FromArgb(backgroud.A, backgroud.R, backgroud.G, backgroud.B));
            _backgroundBrush.Freeze();

            Native = new Canvas()
            {
                Width = width,
                Height = height,
                Background = _backgroundBrush
            };

            Downs = Observable.FromEventPattern<MouseButtonEventArgs>((Native as Canvas), "PreviewMouseLeftButtonDown").Select(e =>
            {
                var p = e.EventArgs.GetPosition((Native as Canvas));
                return new ImmutablePoint(_enableSnap ? Snap(p.X, _snapX) : p.X, _enableSnap ? Snap(p.Y, _snapY) : p.Y);
            });

            Ups = Observable.FromEventPattern<MouseButtonEventArgs>((Native as Canvas), "PreviewMouseLeftButtonUp").Select(e =>
            {
                var p = e.EventArgs.GetPosition((Native as Canvas));
                return new ImmutablePoint(_enableSnap ? Snap(p.X, _snapX) : p.X, _enableSnap ? Snap(p.Y, _snapY) : p.Y);
            });

            Moves = Observable.FromEventPattern<MouseEventArgs>((Native as Canvas), "PreviewMouseMove").Select(e =>
            {
                var p = e.EventArgs.GetPosition((Native as Canvas));
                return new ImmutablePoint(_enableSnap ? Snap(p.X, _snapX) : p.X, _enableSnap ? Snap(p.Y, _snapY) : p.Y);
            });
        }

        public bool EnableSnap
        {
            get { return _enableSnap; }
            set
            {
                _enableSnap = value;
            }
        }

        public double SnapX
        {
            get { return _snapX; }
            set
            {
                _snapX = value;
            }
        }

        public double SnapY
        {
            get { return _snapY; }
            set
            {
                _snapY = value;
            }
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
        IEditor _BezierEditor;
        IEditor _quadraticBezierEditor;
        IEditor _arcEditor;
        IEditor _rectangleEditor;
        IEditor _ellipseEditor;

        private WpfLine CreateGridLine(double x1, double y1, double x2, double y2)
        {
            var xline = new XLine()
            {
                X1 = x1,
                Y1 = y1,
                X2 = x2,
                Y2 = y2,
                Stroke = new XColor(0xFF, 0xE8, 0xE8, 0xE8),
                StrokeThickness = 2.0,
            };
            return new WpfLine(xline);
        }

        private void CreateGrid(double width, double height, double size, double originX, double originY)
        {
            for (double y = size; y < height; y += size)
            {
                _canvas.Add(CreateGridLine(originX, y, width, y));
            }

            for (double x = size; x < width; x += size)
            {
                _canvas.Add(CreateGridLine(x, originY, x, height));
            }
        }

        public MainWindow()
        {
            InitializeComponent();

            _canvas = new WpfCanvas(600.0, 600.0, new XColor(0xFF, 0xFF, 0xFF, 0xFF));
            Layout.Children.Add(_canvas.Native as UIElement);

            CreateGrid(600.0, 600.0, 30.0, 0.0, 0.0);

            _lineEditor = new PortableXLineEditor(_canvas) { IsEnabled = true };
            _BezierEditor = new PortableXBezierEditor(_canvas) { IsEnabled = false };
            _quadraticBezierEditor = new PortableXQuadraticBezierEditor(_canvas) { IsEnabled = false };
            _arcEditor = new PortableXArcEditor(_canvas) { IsEnabled = false };
            _ellipseEditor = new PortableXCanvasEllipseEditor(_canvas) { IsEnabled = false };
            _rectangleEditor = new PortableXCanvasRectangleEditor(_canvas) { IsEnabled = false };

            PreviewKeyDown += (sender, e) =>
            {
                switch (e.Key)
                {
                    // Line
                    case Key.L:
                        _lineEditor.IsEnabled = true;
                        _BezierEditor.IsEnabled = false;
                        _quadraticBezierEditor.IsEnabled = false;
                        _arcEditor.IsEnabled = false;
                        _rectangleEditor.IsEnabled = false;
                        _ellipseEditor.IsEnabled = false;
                        break;
                    // Bezier
                    case Key.B:
                        _lineEditor.IsEnabled = false;
                        _BezierEditor.IsEnabled = true;
                        _quadraticBezierEditor.IsEnabled = false;
                        _arcEditor.IsEnabled = false;
                        _rectangleEditor.IsEnabled = false;
                        _ellipseEditor.IsEnabled = false;
                        break;
                    // QuadraticBezier
                    case Key.Q:
                        _lineEditor.IsEnabled = false;
                        _BezierEditor.IsEnabled = false;
                        _quadraticBezierEditor.IsEnabled = true;
                        _arcEditor.IsEnabled = false;
                        _rectangleEditor.IsEnabled = false;
                        _ellipseEditor.IsEnabled = false;
                        break;
                    // Arc
                    case Key.A:
                        _lineEditor.IsEnabled = false;
                        _BezierEditor.IsEnabled = false;
                        _quadraticBezierEditor.IsEnabled = false;
                        _arcEditor.IsEnabled = true;
                        _rectangleEditor.IsEnabled = false;
                        _ellipseEditor.IsEnabled = false;
                        break;
                    // Rectangle
                    case Key.R:
                        _lineEditor.IsEnabled = false;
                        _BezierEditor.IsEnabled = false;
                        _quadraticBezierEditor.IsEnabled = false;
                        _arcEditor.IsEnabled = false;
                        _rectangleEditor.IsEnabled = true;
                        _ellipseEditor.IsEnabled = false;
                        break;
                    // Ellipse
                    case Key.E:
                        _lineEditor.IsEnabled = false;
                        _BezierEditor.IsEnabled = false;
                        _quadraticBezierEditor.IsEnabled = false;
                        _arcEditor.IsEnabled = false;
                        _rectangleEditor.IsEnabled = false;
                        _ellipseEditor.IsEnabled = true;
                        break;
                    // Toggle EnableSnap
                    case Key.S:
                        _canvas.EnableSnap = _canvas.EnableSnap ? false : true;
                        break;
                }
            };
        }
    }
}
