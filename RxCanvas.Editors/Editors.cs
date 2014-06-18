using RxCanvas.Core;
using RxCanvas.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RxCanvas.Editors
{
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

        public PortableXLineEditor(IModelToNativeConverter nativeConverter, ICanvasFactory canvasFactory, ICanvas canvas)
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
                    _xline = canvasFactory.CreateLine();
                    _xline.X1 = p.X;
                    _xline.Y1 = p.Y;
                    _xline.X2 = p.X;
                    _xline.Y2 = p.Y;
                    _line = nativeConverter.Convert(_xline);
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

        public PortableXBezierEditor(IModelToNativeConverter nativeConverter, ICanvasFactory canvasFactory, ICanvas canvas)
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
                    _xb = canvasFactory.CreateBezier();
                    _xb.Start.X = p.X;
                    _xb.Start.Y = p.Y;
                    _xb.Point1.X = p.X;
                    _xb.Point1.Y = p.Y;
                    _xb.Point2.X = p.X;
                    _xb.Point2.Y = p.Y;
                    _xb.Point3.X = p.X;
                    _xb.Point3.Y = p.Y;
                    _b = nativeConverter.Convert(_xb);
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

        public PortableXQuadraticBezierEditor(IModelToNativeConverter nativeConverter, ICanvasFactory canvasFactory, ICanvas canvas)
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
                    _xqb = canvasFactory.CreateQuadraticBezier();
                    _xqb.Start.X = p.X;
                    _xqb.Start.Y = p.Y;
                    _xqb.Point1.X = p.X;
                    _xqb.Point1.Y = p.Y;
                    _xqb.Point2.X = p.X;
                    _xqb.Point2.Y = p.Y;
                    _qb = nativeConverter.Convert(_xqb);
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

        public PortableXArcEditor(IModelToNativeConverter nativeConverter, ICanvasFactory canvasFactory, ICanvas canvas)
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
                    _xarc = canvasFactory.CreateArc();
                    _xarc.X = _start.X;
                    _xarc.Y = _start.Y;
                    _arc = nativeConverter.Convert(_xarc);
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
            _arc.X = _xarc.X;
            _arc.Y = _xarc.Y;
            _arc.Width = _xarc.Width;
            _arc.Height = _xarc.Height;
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

        public PortableXCanvasRectangleEditor(IModelToNativeConverter nativeConverter, ICanvasFactory canvasFactory, ICanvas canvas)
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
                    _xrectangle = canvasFactory.CreateRectangle();
                    _xrectangle.X = _start.X;
                    _xrectangle.Y = _start.Y;
                    _rectangle = nativeConverter.Convert(_xrectangle);
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

        public PortableXCanvasEllipseEditor(IModelToNativeConverter nativeConverter, ICanvasFactory canvasFactory, ICanvas canvas)
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
                    _xellipse = canvasFactory.CreateEllipse();
                    _xellipse.X = _start.X;
                    _xellipse.Y = _start.Y;
                    _elllipse = nativeConverter.Convert(_xellipse);
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

    public class PortableXCanvasTextEditor : IEditor, IDisposable
    {
        public enum State { None, TopLeft, TopRight, BottomLeft, BottomRight }

        public string Name { get; set; }
        public bool IsEnabled { get; set; }
        public string Key { get; set; }
        public string Modifiers { get; set; }

        private ICanvas _canvas;
        private IText _xtext;
        private IText _text;
        private State _state = State.None;
        private IDisposable _downs;
        private IDisposable _drag;
        private ImmutablePoint _start;

        public PortableXCanvasTextEditor(IModelToNativeConverter nativeConverter, ICanvasFactory canvasFactory, ICanvas canvas)
        {
            _canvas = canvas;

            Name = "Text";
            Key = "T";
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
                    _xtext = canvasFactory.CreateText();
                    _xtext.X = _start.X;
                    _xtext.Y = _start.Y;
                    _text = nativeConverter.Convert(_xtext);
                    _canvas.Add(_text);
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
            _xtext.X = Math.Min(_start.X, p.X);
            _xtext.Y = Math.Min(_start.Y, p.Y);
            _xtext.Width = width + 1.0;
            _xtext.Height = height + 1.0;
            _text.X = _xtext.X;
            _text.Y = _xtext.Y;
            _text.Width = _xtext.Width;
            _text.Height = _xtext.Height;
        }

        public void Dispose()
        {
            _downs.Dispose();
            _drag.Dispose();
        }
    }

    public class PortableXDefaultsFactory : ICanvasFactory
    {
        public IColor CreateColor()
        {
            return new XColor(0x00, 0x00, 0x00, 0x00);
        }

        public IPoint CreatePoint()
        {
            return new XPoint(0.0, 0.0);
        }

        public ILine CreateLine()
        {
            return new XLine()
            {
                X1 = 0.0,
                Y1 = 0.0,
                X2 = 0.0,
                Y2 = 0.0,
                Stroke = new XColor(0xFF, 0x00, 0x00, 0x00),
                StrokeThickness = 2.0,
            };
        }

        public IBezier CreateBezier()
        {
            return new XBezier()
            {
                Start = new XPoint(0.0, 0.0),
                Point1 = new XPoint(0.0, 0.0),
                Point2 = new XPoint(0.0, 0.0),
                Point3 = new XPoint(0.0, 0.0),
                Fill = new XColor(0x00, 0xFF, 0xFF, 0xFF),
                Stroke = new XColor(0xFF, 0x00, 0x00, 0x00),
                StrokeThickness = 2.0,
                IsClosed = false
            };
        }

        public IQuadraticBezier CreateQuadraticBezier()
        {
            return new XQuadraticBezier()
            {
                Start = new XPoint(0.0, 0.0),
                Point1 = new XPoint(0.0, 0.0),
                Point2 = new XPoint(0.0, 0.0),
                Fill = new XColor(0x00, 0xFF, 0xFF, 0xFF),
                Stroke = new XColor(0xFF, 0x00, 0x00, 0x00),
                StrokeThickness = 2.0,
                IsClosed = false
            };
        }

        public IArc CreateArc()
        {
            return new XArc()
            {
                X = 0.0,
                Y = 0.0,
                Width = 0.0,
                Height = 0.0,
                StartAngle = 180.0,
                SweepAngle = 180.0,
                Stroke = new XColor(0xFF, 0x00, 0x00, 0x00),
                StrokeThickness = 2.0,
                Fill = new XColor(0x00, 0xFF, 0xFF, 0xFF),
                IsFilled = false
            };
        }

        public IRectangle CreateRectangle()
        {
            return new XRectangle()
            {
                X = 0.0,
                Y = 0.0,
                Width = 0.0,
                Height = 0.0,
                Stroke = new XColor(0xFF, 0x00, 0x00, 0x00),
                StrokeThickness = 2.0,
                Fill = new XColor(0x00, 0xFF, 0xFF, 0xFF),
                IsFilled = false
            };
        }

        public IEllipse CreateEllipse()
        {
            return new XEllipse()
            {
                X = 0.0,
                Y = 0.0,
                Width = 0.0,
                Height = 0.0,
                Stroke = new XColor(0xFF, 0x00, 0x00, 0x00),
                StrokeThickness = 2.0,
                Fill = new XColor(0x00, 0xFF, 0xFF, 0xFF),
                IsFilled = false
            };
        }

        public IText CreateText()
        {
            return new XText()
            {
                X = 0.0,
                Y = 0.0,
                Width = 0.0,
                Height = 0.0,
                HorizontalAlignment = 1,
                VerticalAlignment = 1,
                Size = 11.0,
                Text = "Text",
                Foreground = new XColor(0xFF, 0x00, 0x00, 0x00),
                Backgroud = new XColor(0x00, 0xFF, 0xFF, 0xFF),
            };
        }

        public ICanvas CreateCanvas()
        {
            return new XCanvas()
            {
                Width = 600.0,
                Height = 600.0,
                Background = new XColor(0x00, 0xFF, 0xFF, 0xFF),
                SnapX = 15.0,
                SnapY = 15.0,
                EnableSnap = true
            };
        }
    }
}
