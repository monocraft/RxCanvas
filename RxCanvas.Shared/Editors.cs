using RxCanvas.Bounds;
using RxCanvas.Interfaces;
using RxCanvas.Model;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RxCanvas.Editors
{
    public class XSelectionEditor : IEditor, IDisposable
    {
        public enum State 
        { 
            None = 0,
            Hover = 1,
            Selected = 2,
            Move = 4,
            HoverSelected = Hover | Selected,
            HoverMove = Hover | Move,
            SelectedMove = Selected | Move,
            HoverSelectedMove = Hover | Selected | Move,
        }

        public string Name { get; set; }

        private bool _isEnabled;
        public bool IsEnabled 
        {
            get { return _isEnabled; }
            set 
            {
                if (_isEnabled)
                {
                    Reset();
                }
                _isEnabled = value; 
            }
        }

        public string Key { get; set; }
        public string Modifiers { get; set; }

        private ICanvas _canvas;
        private ImmutablePoint _start;
        private INative _selected;
        private INative _hover;
        private State _state = State.None;
        private IDisposable _downs;
        private IDisposable _ups;
        private IDisposable _drag;

        public XSelectionEditor(
            IModelToNativeConverter nativeConverter, 
            ICanvasFactory canvasFactory, 
            ICanvas canvas)
        {
            _canvas = canvas;

            Name = "Selection";
            Key = "H";
            Modifiers = "";

            var dragMoves = from move in _canvas.Moves
                            //where _canvas.IsCaptured
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

                bool render = false;

                if (_selected != null)
                {
                    _selected.Bounds.Hide();
                    _selected = null;
                    _state = _state & ~State.Selected;
                    Debug.Print("_state: {0}", _state);
                    render = true;
                }

                if (_hover != null)
                {
                    _hover.Bounds.Hide();
                    _hover = null;
                    _state = _state & ~State.Hover;
                    Debug.Print("_state: {0}", _state);
                    render = true;
                }

                _selected = HitTest(p.X, p.Y);
                if (_selected != null)
                {
                    _selected.Bounds.Show();
                    _state |= State.Selected;
                    Debug.Print("_state: {0}", _state);
                    _start = p;
                    _canvas.Capture();
                    _state |= State.Move;
                    Debug.Print("_state: {0}", _state);
                    render = true;
                }

                if (render)
                {
                    _canvas.Render(null);
                }
            });

            _ups = _canvas.Ups.Subscribe(p =>
            {
                if (!IsEnabled)
                {
                    return;
                }

                if (_canvas.IsCaptured)
                {
                    _state = _state & ~State.Move;
                    Debug.Print("_state: {0}", _state);
                    _canvas.ReleaseCapture();
                }
            });

            _drag = dragPositions.Subscribe(p =>
            {
                if (!IsEnabled)
                {
                    return;
                }

                if (_canvas.IsCaptured)
                {
                    double dx = _start.X - p.X;
                    double dy = _start.Y - p.Y;
                    _start = p;

                    if (_selected is ILine)
                    {
                        // TODO: Move entire line or line Start or line End.
                        var line = _selected as ILine;

                        line.Point1.X -= dx;
                        line.Point1.Y -= dy;
                        line.Point2.X -= dx;
                        line.Point2.Y -= dy;

                        // TODO: Add Move(double dx, double dy) method to INative interface.
                        line.Point1 = line.Point1;
                        line.Point2 = line.Point2;
                    }
                    else if (_selected is IRectangle)
                    {
                        var rectangle = _selected as IRectangle;
                        rectangle.X -= dx;
                        rectangle.Y -= dy;
                    }
                    else if (_selected is IEllipse)
                    {
                        var ellipse = _selected as IEllipse;
                        ellipse.X -= dx;
                        ellipse.Y -= dy;
                    }
                    else if (_selected is IText)
                    {
                        var text = _selected as IText;
                        text.X -= dx;
                        text.Y -= dy;
                    }

                    // TODO: Add missing elements.
   
                    _selected.Bounds.Update();
                    _canvas.Render(null);
                }

                if (!_canvas.IsCaptured)
                {
                    bool render = false;

                    if (_hover != null 
                        && ((_selected != _hover) || (_selected == null)))
                    {
                        _hover.Bounds.Hide();
                        _hover = null;
                        _state = _state & ~State.Hover;
                        Debug.Print("_state: {0}", _state);
                        render = true;
                    }

                    _hover = HitTest(p.X, p.Y);
                    if (_hover != null)
                    {
                        _hover.Bounds.Show();
                        _state |= State.Hover;
                        Debug.Print("_state: {0}", _state);
                        render = true;
                    }

                    if (render)
                    {
                        _canvas.Render(null);
                    }
                }
            });
        }

        private INative HitTest(double x, double y)
        {
            foreach (var child in _canvas.Children)
            {
                if (child.Bounds != null)
                {
                    var bounds = child.Bounds;
                    if (bounds.Contains(x, y))
                    {
                        return child;
                    }
                }
            }
            return null;
        }

        private void Reset()
        {
            bool render = false;

            if (_hover != null)
            {
                _hover.Bounds.Hide();
                _hover = null;
                render = true;
            }

            if (_selected != null)
            {
                _selected.Bounds.Hide();
                _selected = null;
                render = true;
            }

            _state = State.None;
            Debug.Print("_state: {0}", _state);

            if (render)
            {
                _canvas.Render(null);
            }
        }

        public void Dispose()
        {
            _downs.Dispose();
            _ups.Dispose();
            _drag.Dispose();
        }
    }

    public class XLineEditor : IEditor, IDisposable
    {
        public enum State { None, Start, End }

        public string Name { get; set; }
        public bool IsEnabled { get; set; }
        public string Key { get; set; }
        public string Modifiers { get; set; }

        private ICanvas _canvas;
        private ILine _xline;
        private ILine _nline;
        private State _state = State.None;
        private IDisposable _downs;
        private IDisposable _drag;

        public XLineEditor(
            IModelToNativeConverter nativeConverter, 
            ICanvasFactory canvasFactory, 
            ICanvas canvas)
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
                    _xline.Point2.X = p.X;
                    _xline.Point2.Y = p.Y;
                    _nline.Point2 = _xline.Point2;
                    _nline.Bounds.Hide();
                    _canvas.Render(null);
                    _state = State.None;
                    _canvas.ReleaseCapture();
                }
                else
                {
                    _xline = canvasFactory.CreateLine();
                    _xline.Point1.X = p.X;
                    _xline.Point1.Y = p.Y;
                    _xline.Point2.X = p.X;
                    _xline.Point2.Y = p.Y;
                    _nline = nativeConverter.Convert(_xline);
                    _canvas.Add(_nline);
                    _nline.Bounds = new LineBounds(nativeConverter, canvasFactory, canvas, _nline, 15.0, 0.0);
                    _nline.Bounds.Update();
                    _nline.Bounds.Show();
                    _canvas.Capture();
                    _canvas.Render(null);
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
                    _xline.Point2.X = p.X;
                    _xline.Point2.Y = p.Y;
                    _nline.Point2 = _xline.Point2;
                    _nline.Bounds.Update();
                    _canvas.Render(null);
                }
            });
        }

        public void Dispose()
        {
            _downs.Dispose();
            _drag.Dispose();
        }
    }

    public class XBezierEditor : IEditor, IDisposable
    {
        public enum State { None, Start, Point1, Point2, Point3 }

        public string Name { get; set; }
        public bool IsEnabled { get; set; }
        public string Key { get; set; }
        public string Modifiers { get; set; }

        private ICanvas _canvas;
        private IBezier _xb;
        private IBezier _nb;
        private State _state = State.None;
        private IDisposable _downs;
        private IDisposable _drag;

        public XBezierEditor(
            IModelToNativeConverter nativeConverter, 
            ICanvasFactory canvasFactory, 
            ICanvas canvas)
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
                                _nb.Point3 = _xb.Point3;
                                _xb.Point2.X = p.X;
                                _xb.Point2.Y = p.Y;
                                _nb.Point2 = _xb.Point2;
                                _canvas.Render(null);
                                _state = State.Point1;
                            }
                            break;
                        case State.Point1:
                            {
                                _xb.Point1.X = p.X;
                                _xb.Point1.Y = p.Y;
                                _nb.Point1 = _xb.Point1;
                                _canvas.Render(null);
                                _state = State.Point2;
                            }
                            break;
                        case State.Point2:
                            {
                                _xb.Point2.X = p.X;
                                _xb.Point2.Y = p.Y;
                                _nb.Point2 = _xb.Point2;
                                _canvas.Render(null);
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
                    _nb = nativeConverter.Convert(_xb);
                    _canvas.Add(_nb);
                    _canvas.Render(null);
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
                    _nb.Point3 = _xb.Point3;
                    _xb.Point2.X = p.X;
                    _xb.Point2.Y = p.Y;
                    _nb.Point2 = _xb.Point2;
                    _canvas.Render(null);
                }
                else if (_state == State.Point1)
                {
                    _xb.Point1.X = p.X;
                    _xb.Point1.Y = p.Y;
                    _nb.Point1 = _xb.Point1;
                    _canvas.Render(null);
                }
                else if (_state == State.Point2)
                {
                    _xb.Point2.X = p.X;
                    _xb.Point2.Y = p.Y;
                    _nb.Point2 = _xb.Point2;
                    _canvas.Render(null);
                }
            });
        }

        public void Dispose()
        {
            _downs.Dispose();
            _drag.Dispose();
        }
    }

    public class XQuadraticBezierEditor : IEditor, IDisposable
    {
        public enum State { None, Start, Point1, Point2 }

        public string Name { get; set; }
        public bool IsEnabled { get; set; }
        public string Key { get; set; }
        public string Modifiers { get; set; }

        private ICanvas _canvas;
        private IQuadraticBezier _xqb;
        private IQuadraticBezier _nqb;
        private State _state = State.None;
        private IDisposable _downs;
        private IDisposable _drag;

        public XQuadraticBezierEditor(
            IModelToNativeConverter nativeConverter, 
            ICanvasFactory canvasFactory, 
            ICanvas canvas)
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
                                _nqb.Point2 = _xqb.Point2;
                                _canvas.Render(null);
                                _state = State.Point1;
                            }
                            break;
                        case State.Point1:
                            {
                                _xqb.Point1.X = p.X;
                                _xqb.Point1.Y = p.Y;
                                _nqb.Point1 = _xqb.Point1;
                                _canvas.Render(null);
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
                    _nqb = nativeConverter.Convert(_xqb);
                    _canvas.Add(_nqb);
                    _canvas.Render(null);
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
                    _nqb.Point2 = _xqb.Point2;
                    _canvas.Render(null);
                }
                else if (_state == State.Point1)
                {
                    _xqb.Point1.X = p.X;
                    _xqb.Point1.Y = p.Y;
                    _nqb.Point1 = _xqb.Point1;
                    _canvas.Render(null);
                }
            });
        }

        public void Dispose()
        {
            _downs.Dispose();
            _drag.Dispose();
        }
    }

    public class XArcEditor : IEditor, IDisposable
    {
        public enum State { None, Size, StartAngle, SweepAngle }

        public string Name { get; set; }
        public bool IsEnabled { get; set; }
        public string Key { get; set; }
        public string Modifiers { get; set; }

        private ICanvas _canvas;
        private IArc _xarc;
        private IArc _narc;
        private State _state = State.None;
        private IDisposable _downs;
        private IDisposable _drag;
        private ImmutablePoint _start;

        public XArcEditor(
            IModelToNativeConverter nativeConverter, 
            ICanvasFactory canvasFactory, 
            ICanvas canvas)
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
                    _canvas.Render(null);
                    _state = State.None;
                    _canvas.ReleaseCapture();
                }
                else
                {
                    _start = new ImmutablePoint(p.X, p.Y);
                    _xarc = canvasFactory.CreateArc();
                    _xarc.X = _start.X;
                    _xarc.Y = _start.Y;
                    _narc = nativeConverter.Convert(_xarc);
                    _canvas.Add(_narc);
                    _canvas.Render(null);
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
                    _canvas.Render(null);
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
            _narc.X = _xarc.X;
            _narc.Y = _xarc.Y;
            _narc.Width = _xarc.Width;
            _narc.Height = _xarc.Height;
        }

        public void Dispose()
        {
            _downs.Dispose();
            _drag.Dispose();
        }
    }

    public class XCanvasRectangleEditor : IEditor, IDisposable
    {
        public enum State { None, TopLeft, TopRight, BottomLeft, BottomRight }

        public string Name { get; set; }
        public bool IsEnabled { get; set; }
        public string Key { get; set; }
        public string Modifiers { get; set; }

        private ICanvas _canvas;
        private IRectangle _xrectangle;
        private IRectangle _nrectangle;
        private State _state = State.None;
        private IDisposable _downs;
        private IDisposable _drag;
        private ImmutablePoint _start;

        public XCanvasRectangleEditor(
            IModelToNativeConverter nativeConverter, 
            ICanvasFactory canvasFactory, 
            ICanvas canvas)
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
                    _nrectangle.Bounds.Hide();
                    _canvas.Render(null);
                    _state = State.None;
                    _canvas.ReleaseCapture();
                }
                else
                {
                    _start = new ImmutablePoint(p.X - 1.0, p.Y - 1.0);
                    _xrectangle = canvasFactory.CreateRectangle();
                    _xrectangle.X = _start.X;
                    _xrectangle.Y = _start.Y;
                    _nrectangle = nativeConverter.Convert(_xrectangle);
                    _canvas.Add(_nrectangle);
                    _nrectangle.Bounds = new RectangleBounds(nativeConverter, canvasFactory, canvas, _nrectangle, 5.0);
                    _nrectangle.Bounds.Update();
                    _nrectangle.Bounds.Show();
                    _canvas.Render(null);
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
                    _nrectangle.Bounds.Update();
                    _canvas.Render(null);
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
            _nrectangle.X = _xrectangle.X;
            _nrectangle.Y = _xrectangle.Y;
            _nrectangle.Width = _xrectangle.Width;
            _nrectangle.Height = _xrectangle.Height;
        }

        public void Dispose()
        {
            _downs.Dispose();
            _drag.Dispose();
        }
    }

    public class XCanvasEllipseEditor : IEditor, IDisposable
    {
        public enum State { None, TopLeft, TopRight, BottomLeft, BottomRight }

        public string Name { get; set; }
        public bool IsEnabled { get; set; }
        public string Key { get; set; }
        public string Modifiers { get; set; }

        private ICanvas _canvas;
        private IEllipse _xellipse;
        private IEllipse _nellipse;
        private State _state = State.None;
        private IDisposable _downs;
        private IDisposable _drag;
        private ImmutablePoint _start;

        public XCanvasEllipseEditor(
            IModelToNativeConverter nativeConverter, 
            ICanvasFactory canvasFactory, 
            ICanvas canvas)
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
                    _nellipse.Bounds.Hide();
                    _canvas.Render(null);
                    _state = State.None;
                    _canvas.ReleaseCapture();
                }
                else
                {
                    _start = new ImmutablePoint(p.X - 1.0, p.Y - 1.0);
                    _xellipse = canvasFactory.CreateEllipse();
                    _xellipse.X = _start.X;
                    _xellipse.Y = _start.Y;
                    _nellipse = nativeConverter.Convert(_xellipse);
                    _canvas.Add(_nellipse);
                    _nellipse.Bounds = new EllipseBounds(nativeConverter, canvasFactory, canvas, _nellipse, 5.0);
                    _nellipse.Bounds.Update();
                    _nellipse.Bounds.Show();
                    _canvas.Render(null);
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
                    _nellipse.Bounds.Update();
                    _canvas.Render(null);
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
            _nellipse.X = _xellipse.X;
            _nellipse.Y = _xellipse.Y;
            _nellipse.Width = _xellipse.Width;
            _nellipse.Height = _xellipse.Height;
        }

        public void Dispose()
        {
            _downs.Dispose();
            _drag.Dispose();
        }
    }

    public class XCanvasTextEditor : IEditor, IDisposable
    {
        public enum State { None, TopLeft, TopRight, BottomLeft, BottomRight }

        public string Name { get; set; }
        public bool IsEnabled { get; set; }
        public string Key { get; set; }
        public string Modifiers { get; set; }

        private ICanvas _canvas;
        private IText _xtext;
        private IText _ntext;
        private State _state = State.None;
        private IDisposable _downs;
        private IDisposable _drag;
        private ImmutablePoint _start;

        public XCanvasTextEditor(
            IModelToNativeConverter nativeConverter, 
            ICanvasFactory canvasFactory, 
            ICanvas canvas)
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
                    _ntext.Bounds.Hide();
                    _canvas.Render(null);
                    _state = State.None;
                    _canvas.ReleaseCapture();
                }
                else
                {
                    _start = new ImmutablePoint(p.X - 1.0, p.Y - 1.0);
                    _xtext = canvasFactory.CreateText();
                    _xtext.X = _start.X;
                    _xtext.Y = _start.Y;
                    _ntext = nativeConverter.Convert(_xtext);
                    _canvas.Add(_ntext);
                    _ntext.Bounds = new TextBounds(nativeConverter, canvasFactory, canvas, _ntext, 5.0);
                    _ntext.Bounds.Update();
                    _ntext.Bounds.Show();
                    _canvas.Render(null);
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
                    _ntext.Bounds.Update();
                    _canvas.Render(null);
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
            _ntext.X = _xtext.X;
            _ntext.Y = _xtext.Y;
            _ntext.Width = _xtext.Width;
            _ntext.Height = _xtext.Height;
        }

        public void Dispose()
        {
            _downs.Dispose();
            _drag.Dispose();
        }
    }

    public class XModelFactory : ICanvasFactory
    {
        public IColor CreateColor()
        {
            return new XColor(0x00, 0x00, 0x00, 0x00);
        }

        public IPoint CreatePoint()
        {
            return new XPoint(0.0, 0.0);
        }

        public IPolygon CreatePolygon()
        {
            return new XPolygon();
        }

        public ILine CreateLine()
        {
            return new XLine()
            {
                Point1 = new XPoint(0.0, 0.0),
                Point2 = new XPoint(0.0, 0.0),
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
                IsFilled = false,
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
                IsFilled = false,
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
                IsFilled = false,
                IsClosed = false
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
                Fill = new XColor(0x00, 0xFF, 0xFF, 0xFF)
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
                Fill = new XColor(0x00, 0xFF, 0xFF, 0xFF)
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
