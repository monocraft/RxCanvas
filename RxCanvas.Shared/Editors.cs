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
        [Flags]
        public enum State 
        { 
            None = 0,
            Hover = 1,
            Selected = 2,
            Move = 4,
            HoverSelected = Hover | Selected,
            HoverMove = Hover | Move,
            SelectedMove = Selected | Move
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

        private bool IsState(State state)
        {
            return (_state & state) == state;
        }

        public XSelectionEditor(
            IModelToNativeConverter nativeConverter, 
            ICanvasFactory canvasFactory, 
            IBoundsFactory boundsFactory,
            ICanvas canvas)
        {
            _canvas = canvas;

            Name = "Selection";
            Key = "H";
            Modifiers = "";

            var dragMoves = from move in _canvas.Moves
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

                if (IsState(State.Selected))
                {
                    HideSelected();
                    render = true;
                }

                if (IsState(State.Hover))
                {
                    HideHover();
                    render = true;
                }

                _selected = HitTest(p.X, p.Y);
                if (_selected != null)
                {
                    ShowSelected();
                    InitMove(p);
                    _canvas.Capture();
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
                    if (IsState(State.Move))
                    {
                        FinishMove();
                        _canvas.ReleaseCapture();
                    }
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
                    Move(p);
                }
                else
                {
                    bool render = false;
                    var result = HitTest(p.X, p.Y);

                    if (IsState(State.Hover))
                    {
                        if (IsState(State.Selected))
                        {
                            if (_hover != _selected && _hover != result)
                            {
                                HideHover();
                                render = true;
                            }
                            else
                            {
                                return;
                            }
                        }
                        else
                        {
                            if (result != _hover)
                            {
                                HideHover();
                                render = true;
                            }
                            else
                            {
                                return;
                            }
                        }
                    }

                    if (result != null)
                    {
                        if (IsState(State.Selected))
                        {
                            if (result != _selected)
                            {
                                _hover = result;
                                ShowHover();
                                render = true;
                            }
                        }
                        else
                        {
                            _hover = result;
                            ShowHover();
                            render = true;
                        }
                    }

                    if (render)
                    {
                        _canvas.Render(null);
                    }
                }
            });
        }

        private void ShowHover()
        {
            _hover.Bounds.Show();
            _state |= State.Hover;
            Debug.Print("_state: {0}", _state);
        }

        private void HideHover()
        {
            _hover.Bounds.Hide();
            _hover = null;
            _state = _state & ~State.Hover;
            Debug.Print("_state: {0}", _state);
        }

        private void ShowSelected()
        {
            _selected.Bounds.Show();
            _state |= State.Selected;
            Debug.Print("_state: {0}", _state);
        }

        private void HideSelected()
        {
            _selected.Bounds.Hide();
            _selected = null;
            _state = _state & ~State.Selected;
            Debug.Print("_state: {0}", _state);
        }

        private void InitMove(ImmutablePoint p)
        {
            _start = p;
            _state |= State.Move;
            Debug.Print("_state: {0}", _state);
        }

        private void FinishMove()
        {
            _state = _state & ~State.Move;
            Debug.Print("_state: {0}", _state);
        }

        private void Move(ImmutablePoint p)
        {
            double dx = _start.X - p.X;
            double dy = _start.Y - p.Y;
            _start = p;
            _selected.Bounds.Move(dx, dy);
            _selected.Bounds.Update();
            _canvas.Render(null);
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
            IBoundsFactory boundsFactory,
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
                    _nline.Bounds = boundsFactory.Create(_canvas, _nline);
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
            IBoundsFactory boundsFactory,
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
                                _nb.Bounds.Update();
                                _canvas.Render(null);
                                _state = State.Point1;
                            }
                            break;
                        case State.Point1:
                            {
                                _xb.Point1.X = p.X;
                                _xb.Point1.Y = p.Y;
                                _nb.Point1 = _xb.Point1;
                                _nb.Bounds.Update();
                                _canvas.Render(null);
                                _state = State.Point2;
                            }
                            break;
                        case State.Point2:
                            {
                                _xb.Point2.X = p.X;
                                _xb.Point2.Y = p.Y;
                                _nb.Point2 = _xb.Point2;
                                _nb.Bounds.Hide();
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
                    _nb.Bounds = boundsFactory.Create(_canvas, _nb);
                    _nb.Bounds.Update();
                    _nb.Bounds.Show();
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
                            _nb.Bounds.Update();
                            _canvas.Render(null);
                        }
                        break;
                    case State.Point1:
                        {
                            _xb.Point1.X = p.X;
                            _xb.Point1.Y = p.Y;
                            _nb.Point1 = _xb.Point1;
                            _nb.Bounds.Update();
                            _canvas.Render(null);
                        }
                        break;
                    case State.Point2:
                        {
                            _xb.Point2.X = p.X;
                            _xb.Point2.Y = p.Y;
                            _nb.Point2 = _xb.Point2;
                            _nb.Bounds.Update();
                            _canvas.Render(null);
                        }
                        break;
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
            IBoundsFactory boundsFactory,
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
                                _nqb.Bounds.Update();
                                _canvas.Render(null);
                                _state = State.Point1;
                            }
                            break;
                        case State.Point1:
                            {
                                _xqb.Point1.X = p.X;
                                _xqb.Point1.Y = p.Y;
                                _nqb.Point1 = _xqb.Point1;
                                _nqb.Bounds.Hide();
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
                    _nqb.Bounds = boundsFactory.Create(_canvas, _nqb);
                    _nqb.Bounds.Update();
                    _nqb.Bounds.Show();
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

                switch (_state)
                {
                    case State.Start:
                        {
                            _xqb.Point2.X = p.X;
                            _xqb.Point2.Y = p.Y;
                            _nqb.Point2 = _xqb.Point2;
                            _nqb.Bounds.Update();
                            _canvas.Render(null);
                        }
                        break;
                    case State.Point1:
                        {
                            _xqb.Point1.X = p.X;
                            _xqb.Point1.Y = p.Y;
                            _nqb.Point1 = _xqb.Point1;
                            _nqb.Bounds.Update();
                            _canvas.Render(null);
                        }
                        break;
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
            IBoundsFactory boundsFactory,
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
                    _narc.Bounds.Hide();
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
                    _narc.Bounds = boundsFactory.Create(_canvas, _narc);
                    _narc.Bounds.Update();
                    _narc.Bounds.Show();
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
                    _narc.Bounds.Update();
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

        public XCanvasRectangleEditor(
            IModelToNativeConverter nativeConverter, 
            ICanvasFactory canvasFactory,
            IBoundsFactory boundsFactory,
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
                    _xrectangle.Point2.X = p.X;
                    _xrectangle.Point2.Y = p.Y;
                    _nrectangle.Point2 = _xrectangle.Point2;
                    _nrectangle.Bounds.Hide();
                    _canvas.Render(null);
                    _state = State.None;
                    _canvas.ReleaseCapture();
                }
                else
                {
                    _xrectangle = canvasFactory.CreateRectangle();
                    _xrectangle.Point1.X = p.X;
                    _xrectangle.Point1.Y = p.Y;
                    _xrectangle.Point2.X = p.X;
                    _xrectangle.Point2.Y = p.Y;
                    _nrectangle = nativeConverter.Convert(_xrectangle);
                    _canvas.Add(_nrectangle);
                    _nrectangle.Bounds = boundsFactory.Create(_canvas, _nrectangle);
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
                    _xrectangle.Point2.X = p.X;
                    _xrectangle.Point2.Y = p.Y;
                    _nrectangle.Point2 = _xrectangle.Point2;
                    _nrectangle.Bounds.Update();
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

        public XCanvasEllipseEditor(
            IModelToNativeConverter nativeConverter,
            ICanvasFactory canvasFactory,
            IBoundsFactory boundsFactory,
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
                    _xellipse.Point2.X = p.X;
                    _xellipse.Point2.Y = p.Y;
                    _nellipse.Point2 = _xellipse.Point2;
                    _nellipse.Bounds.Hide();
                    _canvas.Render(null);
                    _state = State.None;
                    _canvas.ReleaseCapture();
                }
                else
                {
                    _xellipse = canvasFactory.CreateEllipse();
                    _xellipse.Point1.X = p.X;
                    _xellipse.Point1.Y = p.Y;
                    _xellipse.Point2.X = p.X;
                    _xellipse.Point2.Y = p.Y;
                    _nellipse = nativeConverter.Convert(_xellipse);
                    _canvas.Add(_nellipse);
                    _nellipse.Bounds = boundsFactory.Create(_canvas, _nellipse);
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
                    _xellipse.Point2.X = p.X;
                    _xellipse.Point2.Y = p.Y;
                    _nellipse.Point2 = _xellipse.Point2;
                    _nellipse.Bounds.Update();
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
            IBoundsFactory boundsFactory,
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
                    _ntext.Bounds = boundsFactory.Create(_canvas, _ntext);
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
            double x = Math.Min(_start.X, p.X);
            double y = Math.Min(_start.Y, p.Y);
            _xtext.X = _start.X <= p.X ? x : x - 1.0;
            _xtext.Y = _start.Y <= p.Y ? y : y - 1.0;
            _xtext.Width = _start.X <= p.X ? width + 1.0 : width + 3.0;
            _xtext.Height = _start.Y <= p.Y ? height + 1.0 : height + 3.0;
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
                Point1 = new XPoint(0.0, 0.0),
                Point2 = new XPoint(0.0, 0.0),
                Stroke = new XColor(0xFF, 0x00, 0x00, 0x00),
                StrokeThickness = 2.0,
                Fill = new XColor(0x00, 0xFF, 0xFF, 0xFF)
            };
        }

        public IEllipse CreateEllipse()
        {
            return new XEllipse()
            {
                Point1 = new XPoint(0.0, 0.0),
                Point2 = new XPoint(0.0, 0.0),
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
