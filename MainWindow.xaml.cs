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
using System.Reflection;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Joins;
using System.Reactive.Linq;
using System.Reactive.PlatformServices;
using System.Reactive.Subjects;
using System.Reactive.Threading;
using Autofac;
using Autofac.Builder;
using Autofac.Core;
using Autofac.Features;
using Autofac.Util;
using System.Collections.ObjectModel;

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

        IList<INative> Children { get; set; }

        double Width { get; set; }
        double Height { get; set; }
        IColor Background { get; set; }

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
        string Name { get; set; }
        bool IsEnabled { get; set; }
        string Key { get; set; }
        string Modifiers { get; set; }
    }

    public interface INativeFactory
    {
        ILine CreateLine(ILine line);
        IBezier CreateBezier(IBezier bezier);
        IQuadraticBezier CreateQuadraticBezier(IQuadraticBezier quadraticBezier);
        IArc CreateArc(IArc arc);
        IRectangle CreateRectangle(IRectangle rectangle);
        IEllipse CreateEllipse(IEllipse ellipse);
        ICanvas CreateCanvas(ICanvas canvas);
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

    public class XLine : ILine
    {
        public object Native { get; set; }
        public double X1 { get; set; }
        public double Y1 { get; set; }
        public double X2 { get; set; }
        public double Y2 { get; set; }
        public IColor Stroke { get; set; }
        public double StrokeThickness { get; set; }
    }

    public class XBezier : IBezier
    {
        public object Native { get; set; }
        public IPoint Start { get; set; }
        public IPoint Point1 { get; set; }
        public IPoint Point2 { get; set; }
        public IPoint Point3 { get; set; }
        public IColor Fill { get; set; }
        public IColor Stroke { get; set; }
        public double StrokeThickness { get; set; }
        public bool IsClosed { get; set; }
    }

    public class XQuadraticBezier : IQuadraticBezier
    {
        public object Native { get; set; }
        public IPoint Start { get; set; }
        public IPoint Point1 { get; set; }
        public IPoint Point2 { get; set; }
        public IColor Fill { get; set; }
        public IColor Stroke { get; set; }
        public double StrokeThickness { get; set; }
        public bool IsClosed { get; set; }
    }

    public class XArc : IArc
    {
        public object Native { get; set; }
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

    public class XRectangle : IRectangle
    {
        public object Native { get; set; }
        public double X { get; set; }
        public double Y { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }
        public IColor Stroke { get; set; }
        public double StrokeThickness { get; set; }
        public IColor Fill { get; set; }
        public bool IsFilled { get; set; }
    }

    public class XEllipse : IEllipse
    {
        public object Native { get; set; }
        public double X { get; set; }
        public double Y { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }
        public IColor Stroke { get; set; }
        public double StrokeThickness { get; set; }
        public IColor Fill { get; set; }
        public bool IsFilled { get; set; }
    }

    public class XCanvas : ICanvas
    {
        public object Native { get; set; }

        public IObservable<ImmutablePoint> Downs { get; set; }
        public IObservable<ImmutablePoint> Ups { get; set; }
        public IObservable<ImmutablePoint> Moves { get; set; }

        public IList<INative> Children { get; set; }

        public double Width { get; set; }
        public double Height { get; set; }
        public IColor Background { get; set; }

        public bool EnableSnap { get; set; }
        public double SnapX { get; set; }
        public double SnapY { get; set; }

        public bool IsCaptured 
        {
            get { throw new NotImplementedException(); }
        }

        public void Capture()
        {
            throw new NotImplementedException();
        }

        public void ReleaseCapture()
        {
            throw new NotImplementedException();
        }

        public void Add(INative value)
        {
            throw new NotImplementedException();
        }

        public void Remove(INative value)
        {
            throw new NotImplementedException();
        }
    }

    #endregion

    #region Editors

    public class PortableXLineEditor : IEditor, IDisposable
    {
        public enum State { None, Start, End }

        public string Name { get; set; }
        public bool IsEnabled { get; set; }
        public string Key { get; set; }
        public string Modifiers { get; set; }

        private ICanvas _canvas;
        private ILine _xline;
        private ILine _line;
        private State _state = State.None;
        private IDisposable _downs;
        private IDisposable _drag;

        public PortableXLineEditor(INativeFactory factory, ICanvas canvas)
        {
            _canvas = canvas;

            Name = "Line";
            Key = "L";
            Modifiers = "";

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
                    _line = factory.CreateLine(_xline);
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

        public string Name { get; set; }
        public bool IsEnabled { get; set; }
        public string Key { get; set; }
        public string Modifiers { get; set; }

        private ICanvas _canvas;
        private IBezier _xb;
        private IBezier _b;
        private State _state = State.None;
        private IDisposable _downs;
        private IDisposable _drag;

        public PortableXBezierEditor(INativeFactory factory, ICanvas canvas)
        {
            _canvas = canvas;

            Name = "Bézier";
            Key = "B";
            Modifiers = "";

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
                    _b = factory.CreateBezier(_xb);
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

        public string Name { get; set; }
        public bool IsEnabled { get; set; }
        public string Key { get; set; }
        public string Modifiers { get; set; }

        private ICanvas _canvas;
        private IQuadraticBezier _xqb;
        private IQuadraticBezier _qb;
        private State _state = State.None;
        private IDisposable _downs;
        private IDisposable _drag;

        public PortableXQuadraticBezierEditor(INativeFactory factory, ICanvas canvas)
        {
            _canvas = canvas;

            Name = "Quadratic Bézier";
            Key = "Q";
            Modifiers = "";

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
                    _qb = factory.CreateQuadraticBezier(_xqb);
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

        public string Name { get; set; }
        public bool IsEnabled { get; set; }
        public string Key { get; set; }
        public string Modifiers { get; set; }

        private ICanvas _canvas;
        private IArc _xarc;
        private IArc _arc;
        private State _state = State.None;
        private IDisposable _downs;
        private IDisposable _drag;
        private ImmutablePoint _start;

        public PortableXArcEditor(INativeFactory factory, ICanvas canvas)
        {
            _canvas = canvas;

            Name = "Arc";
            Key = "A";
            Modifiers = "";

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
                    _arc = factory.CreateArc(_xarc);
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

        public string Name { get; set; }
        public bool IsEnabled { get; set; }
        public string Key { get; set; }
        public string Modifiers { get; set; }

        private ICanvas _canvas;
        private IRectangle _xrectangle;
        private IRectangle _rectangle;
        private State _state = State.None;
        private IDisposable _downs;
        private IDisposable _drag;
        private ImmutablePoint _start;

        public PortableXCanvasRectangleEditor(INativeFactory factory, ICanvas canvas)
        {
            _canvas = canvas;

            Name = "Rectangle";
            Key = "R";
            Modifiers = "";

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
                    _rectangle = factory.CreateRectangle(_xrectangle);
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

        public string Name { get; set; }
        public bool IsEnabled { get; set; }
        public string Key { get; set; }
        public string Modifiers { get; set; }

        private ICanvas _canvas;
        private IEllipse _xellipse;
        private IEllipse _elllipse;
        private State _state = State.None;
        private IDisposable _downs;
        private IDisposable _drag;
        private ImmutablePoint _start;

        public PortableXCanvasEllipseEditor(INativeFactory factory, ICanvas canvas)
        {
            _canvas = canvas;

            Name = "Ellipse";
            Key = "E";
            Modifiers = "";

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
                    _elllipse = factory.CreateEllipse(_xellipse);
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

    public class WpfLine : ILine
    {
        public object Native { get; set; }

        private SolidColorBrush _strokeBrush;
        private IColor _stroke;
        private Line _line;

        public WpfLine(ILine line)
        {
            _stroke = line.Stroke;

            _strokeBrush = new SolidColorBrush(Color.FromArgb(_stroke.A, _stroke.R, _stroke.G, _stroke.B));
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
            get { return _line.X1; }
            set { _line.X1 = value; }
        }

        public double Y1
        {
            get { return _line.Y1; }
            set { _line.Y1 = value; }
        }

        public double X2
        {
            get { return _line.X2; }
            set { _line.X2 = value; }
        }

        public double Y2
        {
            get { return _line.Y2; }
            set { _line.Y2 = value; }
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

        public bool IsClosed
        {
            get { return _pf.IsClosed; }
            set { _pf.IsClosed = value; }
        }
    }

    public class WpfQuadraticBezier : IQuadraticBezier
    {
        public object Native { get; set; }

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

        public bool IsClosed
        {
            get { return _pf.IsClosed; }
            set { _pf.IsClosed = value; }
        }
    }

    public class WpfArc : IArc
    {
        public object Native { get; set; }

        private SolidColorBrush _fillBrush;
        private SolidColorBrush _strokeBrush;
        private Path _path;
        private PathGeometry _pg;
        private PathFigure _pf;
        private ArcSegment _as;
        private Point _start;
        private IColor _fill;
        private IColor _stroke;

        public WpfArc(IArc arc)
        {
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

        private SolidColorBrush _strokeBrush;
        private SolidColorBrush _fillBrush;
        private Rectangle _rectangle;
        private IColor _stroke;
        private IColor _fill;

        public WpfRectangle(IRectangle rectangle)
        {
            _stroke = rectangle.Stroke;
            _fill = rectangle.Fill;

            _strokeBrush = new SolidColorBrush(Color.FromArgb(_stroke.A, _stroke.R, _stroke.G, _stroke.B));
            _strokeBrush.Freeze();
            _fillBrush = new SolidColorBrush(Color.FromArgb(_fill.A, _fill.R, _fill.G, _fill.B));
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
            get { return Canvas.GetLeft(_rectangle); }
            set { Canvas.SetLeft(_rectangle, value); }
        }

        public double Y
        {
            get { return Canvas.GetTop(_rectangle); }
            set { Canvas.SetTop(_rectangle, value); }
        }

        public double Width
        {
            get { return _rectangle.Width; }
            set { _rectangle.Width = value; }
        }

        public double Height
        {
            get { return _rectangle.Height; }
            set { _rectangle.Height = value; }
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

        public bool IsFilled
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }
    }

    public class WpfEllipse : IEllipse
    {
        public object Native { get; set; }

        private SolidColorBrush _strokeBrush;
        private SolidColorBrush _fillBrush;
        private Ellipse _ellipse;

        private IColor _stroke;
        private IColor _fill;

        public WpfEllipse(IEllipse ellipse)
        {
            _stroke = ellipse.Stroke;
            _fill = ellipse.Fill;

            _strokeBrush = new SolidColorBrush(Color.FromArgb(_stroke.A, _stroke.R, _stroke.G, _stroke.B));
            _strokeBrush.Freeze();
            _fillBrush = new SolidColorBrush(Color.FromArgb(_fill.A, _fill.R, _fill.G, _fill.B));
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
            get { return Canvas.GetLeft(_ellipse); }
            set { Canvas.SetLeft(_ellipse, value); }
        }

        public double Y
        {
            get { return Canvas.GetTop(_ellipse); }
            set { Canvas.SetTop(_ellipse, value); }
        }

        public double Width
        {
            get { return _ellipse.Width; }
            set { _ellipse.Width = value; }
        }

        public double Height
        {
            get { return _ellipse.Height; }
            set { _ellipse.Height = value; }
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

        public bool IsFilled
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }
    }

    public class WpfCanvas : ICanvas
    {
        public object Native { get; set; }

        public IObservable<ImmutablePoint> Downs { get; set; }
        public IObservable<ImmutablePoint> Ups { get; set; }
        public IObservable<ImmutablePoint> Moves { get; set; }

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
                _backgroundBrush = new SolidColorBrush(Color.FromArgb(_background.A, _background.R, _background.G, _background.B));
                _backgroundBrush.Freeze();
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
    }

    public class WpfNativeFactory : INativeFactory
    {
        public ILine CreateLine(ILine line)
        {
            return new WpfLine(line);
        }

        public IBezier CreateBezier(IBezier bezier)
        {
            return new WpfBezier(bezier);
        }

        public IQuadraticBezier CreateQuadraticBezier(IQuadraticBezier quadraticBezier)
        {
            return new WpfQuadraticBezier(quadraticBezier);
        }

        public IArc CreateArc(IArc arc)
        {
            return new WpfArc(arc);
        }

        public IRectangle CreateRectangle(IRectangle rectangle)
        {
            return new WpfRectangle(rectangle);
        }

        public IEllipse CreateEllipse(IEllipse ellipse)
        {
            return new WpfEllipse(ellipse);
        }

        public ICanvas CreateCanvas(ICanvas canvas)
        {
            return new WpfCanvas(canvas);
        }
    }

    #endregion

    public partial class MainWindow : Window
    {
        private IContainer _container;
        private ILifetimeScope _backgroundScope;
        private ILifetimeScope _drawingScope;
        private ICollection<IEditor> _editors;
        private IDictionary<Tuple<Key, ModifierKeys>, Action> _shortcuts;
        private ICanvas _backgroundCanvas;
        private ICanvas _drawingCanvas;

        private INative CreateGridLine(IColor stroke, double thickness, double x1, double y1, double x2, double y2)
        {
            var xline = new XLine()
            {
                X1 = x1,
                Y1 = y1,
                X2 = x2,
                Y2 = y2,
                Stroke = stroke,
                StrokeThickness = thickness,
            };
            return new WpfLine(xline);
        }

        private void CreateGrid(ICanvas canvas, double width, double height, double size, double originX, double originY)
        {
            IColor stroke = new XColor(0xFF, 0xE8, 0xE8, 0xE8);
            double thickness = 2.0;

            for (double y = size; y < height; y += size)
            {
                canvas.Add(CreateGridLine(stroke, thickness, originX, y, width, y));
            }

            for (double x = size; x < width; x += size)
            {
                canvas.Add(CreateGridLine(stroke, thickness, x, originY, x, height));
            }
        }

        public MainWindow()
        {
            InitializeComponent();

            // register components
            var builder = new ContainerBuilder();

            builder.RegisterAssemblyTypes(Assembly.GetExecutingAssembly())
                .Where(t => t.Name.EndsWith("Editor"))
                .AsImplementedInterfaces()
                .InstancePerLifetimeScope();

            builder.Register<INativeFactory>(f => new WpfNativeFactory()).InstancePerLifetimeScope();

            builder.Register<ICanvas>(c => 
                {
                    var xcanvas = new XCanvas() 
                    { 
                        Width = 600.0, 
                        Height = 600.0, 
                        Background = new XColor(0x00, 0xFF, 0xFF, 0xFF), 
                        SnapX = 15.0, 
                        SnapY = 15.0, 
                        EnableSnap = true 
                    };
                    return new WpfCanvas(xcanvas);
                }).InstancePerLifetimeScope();

            // resolve dependencies
            _container = builder.Build();
            _backgroundScope = _container.BeginLifetimeScope();
            _drawingScope = _container.BeginLifetimeScope();

            _backgroundCanvas = _backgroundScope.Resolve<ICanvas>();
            _drawingCanvas = _drawingScope.Resolve<ICanvas>();
            _editors = _drawingScope.Resolve<ICollection<IEditor>>();

            // initialize editors
            _editors.Where(e => e.Name == "Line").FirstOrDefault().IsEnabled = true;

            // initialize shortcuts
            _shortcuts = new Dictionary<Tuple<Key, ModifierKeys>, Action>();

            // initialize key converters
            var keyConverter = new KeyConverter();
            var modifiersKeyConverter = new ModifierKeysConverter();
            
            // add editor shortcuts
            foreach (var editor in _editors)
            {
                var _editor = editor;
                _shortcuts.Add(
                    new Tuple<Key, ModifierKeys>((Key)keyConverter.ConvertFromString(editor.Key), 
                                                 (ModifierKeys)modifiersKeyConverter.ConvertFromString(editor.Modifiers)),
                    () =>
                    {
                        foreach(var e in _editors)
                        {
                            e.IsEnabled = false;
                        };
                        _editor.IsEnabled = true;
                    });
            }

            // add snap shortcut
            _shortcuts.Add(
                new Tuple<Key, ModifierKeys>((Key)keyConverter.ConvertFromString("S"),
                                             (ModifierKeys)modifiersKeyConverter.ConvertFromString("")),
                () =>
                {
                    var canvas =_drawingScope.Resolve<ICanvas>();
                    canvas.EnableSnap = canvas.EnableSnap ? false : true;
                });

            // add canvas to root layout
            Layout.Children.Add(_backgroundCanvas.Native as UIElement);
            Layout.Children.Add(_drawingCanvas.Native as UIElement);

            // add grid to canvas
            CreateGrid(_backgroundCanvas, 600.0, 600.0, 30.0, 0.0, 0.0);

            // handle user input
            PreviewKeyDown += (sender, e) =>
            {
                Action action;
                bool result = _shortcuts.TryGetValue(new Tuple<Key, ModifierKeys>(e.Key, Keyboard.Modifiers), out action);
                if(result == true && action != null)
                {
                    action();
                }
            };

            DataContext = _drawingCanvas;
        }
    }
}
