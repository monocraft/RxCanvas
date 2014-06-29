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
            Point1 = 8,
            Point2 = 16,
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
        private ImmutablePoint _original;
        private ImmutablePoint _start;
        private INative _selected;
        private INative _hover;
        private State _state = State.None;
        private IDisposable _downs;
        private IDisposable _ups;
        private IDisposable _drag;

        public XSelectionEditor(
            INativeConverter nativeConverter, 
            ICanvasFactory canvasFactory, 
            IBoundsFactory boundsFactory,
            ICanvas canvas)
        {
            _canvas = canvas;

            Name = "Selection";
            Key = "H";
            Modifiers = "";

            var drags = Observable.Merge(_canvas.Downs, _canvas.Ups, _canvas.Moves);

            _downs = _canvas.Downs.Where(_ => IsEnabled).Subscribe(p => Down(p));
            _ups = _canvas.Ups.Where(_ => IsEnabled).Subscribe(p => Up(p));
            _drag = drags.Where(_ => IsEnabled).Subscribe(p => Drag(p));
        }

        private bool IsState(State state)
        {
            return (_state & state) == state;
        }

        private void Down(ImmutablePoint p)
        {
            bool render = false;

            if (IsState(State.Selected))
            {
                HideSelected();
                render = true;
            }
            else
            {
                if (IsState(State.Point2))
                {
                    ResetSelection();
                    render = true;
                }
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

            if (!render)
            {
                if (IsState(State.None))
                {
                    InitSelection();
                    _canvas.Capture();
                    render = true;
                }
            }

            if (render)
            {
                _canvas.Render(null);
            }
        }

        private void Up(ImmutablePoint p)
        {
            if (_canvas.IsCaptured)
            {
                if (IsState(State.Move))
                {
                    FinishMove(p);
                    _canvas.ReleaseCapture();
                }

                if (IsState(State.Point1))
                {
                    FinishSelection();
                    _canvas.ReleaseCapture();
                }
            }
        }

        private void Drag(ImmutablePoint p)
        {
            if (_canvas.IsCaptured)
            {
                if (IsState(State.Move))
                {
                    Move(p);
                }

                if (IsState(State.Point2))
                {
                    MoveSelection(p);
                }
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
        }

        private void InitSelection()
        {
            // TODO:
            _state |= State.Point1;
            Debug.Print("_state: {0}", _state);
        }

        private void MoveSelection(ImmutablePoint p)
        {
            // TODO:
        }

        private void FinishSelection()
        {
            // TODO:
            _state = _state & ~State.Point1;
            _state |= State.Point2;
            Debug.Print("_state: {0}", _state);
        }

        private void ResetSelection()
        {
            // TODO:
            _state = _state & ~State.Point2;
            Debug.Print("_state: {0}", _state);
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
            // TODO: Create history snapshot but do not push undo.
            _original = p;
            _start = p;
            _state |= State.Move;
            Debug.Print("_state: {0}", _state);
        }

        private void FinishMove(ImmutablePoint p)
        {
            if (p.X == _original.X && p.Y == _original.Y)
            {
                // TODO: Do not push history undo.
            }
            else
            {
                // TODO: Push history undo.
            }
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
            return _canvas.Children
                .Where(c => c.Bounds != null && c.Bounds.Contains(x, y))
                .FirstOrDefault();
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
        private IDisposable _drags;

        public XLineEditor(
            INativeConverter nativeConverter, 
            ICanvasFactory canvasFactory,
            IBoundsFactory boundsFactory,
            ICanvas canvas)
        {
            _canvas = canvas;

            Name = "Line";
            Key = "L";
            Modifiers = "";

            var moves = _canvas.Moves.Where(_ => _canvas.IsCaptured);
            var drags = Observable.Merge(_canvas.Downs, _canvas.Ups, moves);

            _downs = _canvas.Downs.Where(_ => IsEnabled).Subscribe(p =>
            {
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
                    _canvas.History.Snapshot(_canvas);
                    _canvas.Add(_nline);
                    _nline.Bounds = boundsFactory.Create(_canvas, _nline);
                    _nline.Bounds.Update();
                    _nline.Bounds.Show();
                    _canvas.Capture();
                    _canvas.Render(null);
                    _state = State.End;
                }
            });

            _drags = drags.Where(_ => IsEnabled).Subscribe(p =>
            {
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
            _drags.Dispose();
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
        private IDisposable _drags;

        public XBezierEditor(
            INativeConverter nativeConverter, 
            ICanvasFactory canvasFactory,
            IBoundsFactory boundsFactory,
            ICanvas canvas)
        {
            _canvas = canvas;

            Name = "Bézier";
            Key = "B";
            Modifiers = "";

            var moves = _canvas.Moves.Where(_ => _canvas.IsCaptured);
            var drags = Observable.Merge(_canvas.Downs, _canvas.Ups, moves);

            _downs = _canvas.Downs.Where(_ => IsEnabled).Subscribe(p =>
            {
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
                    _canvas.History.Snapshot(_canvas);
                    _canvas.Add(_nb);
                    _nb.Bounds = boundsFactory.Create(_canvas, _nb);
                    _nb.Bounds.Update();
                    _nb.Bounds.Show();
                    _canvas.Render(null);
                    _canvas.Capture();
                    _state = State.Start;
                }
            });

            _drags = drags.Where(_ => IsEnabled).Subscribe(p =>
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
            _drags.Dispose();
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
        private IDisposable _drags;

        public XQuadraticBezierEditor(
            INativeConverter nativeConverter, 
            ICanvasFactory canvasFactory,
            IBoundsFactory boundsFactory,
            ICanvas canvas)
        {
            _canvas = canvas;

            Name = "Quadratic Bézier";
            Key = "Q";
            Modifiers = "";

            var moves = _canvas.Moves.Where(_ => _canvas.IsCaptured);
            var drags = Observable.Merge(_canvas.Downs, _canvas.Ups, moves);

            _downs = _canvas.Downs.Where(_ => IsEnabled).Subscribe(p =>
            {
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
                    _canvas.History.Snapshot(_canvas);
                    _canvas.Add(_nqb);
                    _nqb.Bounds = boundsFactory.Create(_canvas, _nqb);
                    _nqb.Bounds.Update();
                    _nqb.Bounds.Show();
                    _canvas.Render(null);
                    _canvas.Capture();
                    _state = State.Start;
                }
            });

            _drags = drags.Where(_ => IsEnabled).Subscribe(p =>
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
            _drags.Dispose();
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
        private IDisposable _drags;

        public XArcEditor(
            INativeConverter nativeConverter, 
            ICanvasFactory canvasFactory,
            IBoundsFactory boundsFactory,
            ICanvas canvas)
        {
            _canvas = canvas;

            Name = "Arc";
            Key = "A";
            Modifiers = "";

            var moves = _canvas.Moves.Where(_ => _canvas.IsCaptured);
            var drags = Observable.Merge(_canvas.Downs, _canvas.Ups, moves);

            _downs = _canvas.Downs.Where(_ => IsEnabled).Subscribe(p =>
            {
                if (_canvas.IsCaptured)
                {
                    _xarc.Point2.X = p.X;
                    _xarc.Point2.Y = p.Y;
                    _narc.Point2 = _xarc.Point2;
                    _narc.Bounds.Hide();
                    _canvas.Render(null);
                    _state = State.None;
                    _canvas.ReleaseCapture();
                }
                else
                {
                    _xarc = canvasFactory.CreateArc();
                    _xarc.Point1.X = p.X;
                    _xarc.Point1.Y = p.Y;
                    _xarc.Point2.X = p.X;
                    _xarc.Point2.Y = p.Y;
                    _narc = nativeConverter.Convert(_xarc);
                    _canvas.History.Snapshot(_canvas);
                    _canvas.Add(_narc);
                    _narc.Bounds = boundsFactory.Create(_canvas, _narc);
                    _narc.Bounds.Update();
                    _narc.Bounds.Show();
                    _canvas.Render(null);
                    _canvas.Capture();
                    _state = State.Size;
                }
            });

            _drags = drags.Where(_ => IsEnabled).Subscribe(p =>
            {
                if (_state == State.Size)
                {
                    _xarc.Point2.X = p.X;
                    _xarc.Point2.Y = p.Y;
                    _narc.Point2 = _xarc.Point2;
                    _narc.Bounds.Update();
                    _canvas.Render(null);
                }
            });
        }

        public void Dispose()
        {
            _downs.Dispose();
            _drags.Dispose();
        }
    }

    public class XRectangleEditor : IEditor, IDisposable
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
        private IDisposable _drags;

        public XRectangleEditor(
            INativeConverter nativeConverter, 
            ICanvasFactory canvasFactory,
            IBoundsFactory boundsFactory,
            ICanvas canvas)
        {
            _canvas = canvas;

            Name = "Rectangle";
            Key = "R";
            Modifiers = "";

            var moves = _canvas.Moves.Where(_ => _canvas.IsCaptured);
            var drags = Observable.Merge(_canvas.Downs, _canvas.Ups, moves);

            _downs = _canvas.Downs.Where(_ => IsEnabled).Subscribe(p =>
            {
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
                    _canvas.History.Snapshot(_canvas);
                    _canvas.Add(_nrectangle);
                    _nrectangle.Bounds = boundsFactory.Create(_canvas, _nrectangle);
                    _nrectangle.Bounds.Update();
                    _nrectangle.Bounds.Show();
                    _canvas.Render(null);
                    _canvas.Capture();
                    _state = State.BottomRight;
                }
            });

            _drags = drags.Where(_ => IsEnabled).Subscribe(p =>
            {
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
            _drags.Dispose();
        }
    }

    public class XEllipseEditor : IEditor, IDisposable
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
        private IDisposable _drags;

        public XEllipseEditor(
            INativeConverter nativeConverter,
            ICanvasFactory canvasFactory,
            IBoundsFactory boundsFactory,
            ICanvas canvas)
        {
            _canvas = canvas;

            Name = "Ellipse";
            Key = "E";
            Modifiers = "";

            var moves = _canvas.Moves.Where(_ => _canvas.IsCaptured);
            var drags = Observable.Merge(_canvas.Downs, _canvas.Ups, moves);

            _downs = _canvas.Downs.Where(_ => IsEnabled).Subscribe(p =>
            {
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
                    _canvas.History.Snapshot(_canvas);
                    _canvas.Add(_nellipse);
                    _nellipse.Bounds = boundsFactory.Create(_canvas, _nellipse);
                    _nellipse.Bounds.Update();
                    _nellipse.Bounds.Show();
                    _canvas.Render(null);
                    _canvas.Capture();
                    _state = State.BottomRight;
                }
            });

            _drags = drags.Where(_ => IsEnabled).Subscribe(p =>
            {
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
            _drags.Dispose();
        }
    }

    public class XTextEditor : IEditor, IDisposable
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
        private IDisposable _drags;

        public XTextEditor(
            INativeConverter nativeConverter,
            ICanvasFactory canvasFactory,
            IBoundsFactory boundsFactory,
            ICanvas canvas)
        {
            _canvas = canvas;

            Name = "Text";
            Key = "T";
            Modifiers = "";

            var moves = _canvas.Moves.Where(_ => _canvas.IsCaptured);
            var drags = Observable.Merge(_canvas.Downs, _canvas.Ups, moves);

            _downs = _canvas.Downs.Where(_ => IsEnabled).Where(_ => IsEnabled).Subscribe(p =>
            {
                if (_canvas.IsCaptured)
                {
                    _xtext.Point2.X = p.X;
                    _xtext.Point2.Y = p.Y;
                    _ntext.Point2 = _xtext.Point2;
                    _ntext.Bounds.Hide();
                    _canvas.Render(null);
                    _state = State.None;
                    _canvas.ReleaseCapture();
                }
                else
                {
                    _xtext = canvasFactory.CreateText();
                    _xtext.Point1.X = p.X;
                    _xtext.Point1.Y = p.Y;
                    _xtext.Point2.X = p.X;
                    _xtext.Point2.Y = p.Y;
                    _ntext = nativeConverter.Convert(_xtext);
                    _canvas.History.Snapshot(_canvas);
                    _canvas.Add(_ntext);
                    _ntext.Bounds = boundsFactory.Create(_canvas, _ntext);
                    _ntext.Bounds.Update();
                    _ntext.Bounds.Show();
                    _canvas.Render(null);
                    _canvas.Capture();
                    _state = State.BottomRight;
                }
            });

            _drags = drags.Where(_ => IsEnabled).Subscribe(p =>
            {
                if (_state == State.BottomRight)
                {
                    _xtext.Point2.X = p.X;
                    _xtext.Point2.Y = p.Y;
                    _ntext.Point2 = _xtext.Point2;
                    _ntext.Bounds.Update();
                    _canvas.Render(null);
                }
            });
        }

        public void Dispose()
        {
            _downs.Dispose();
            _drags.Dispose();
        }
    }

    public class XCanvasFactory : ICanvasFactory
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
                Stroke = new XColor(0xFF, 0x00, 0x00, 0x00),
                StrokeThickness = 2.0,
                Fill = new XColor(0x00, 0xFF, 0xFF, 0xFF),
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
                Stroke = new XColor(0xFF, 0x00, 0x00, 0x00),
                StrokeThickness = 2.0,
                Fill = new XColor(0x00, 0xFF, 0xFF, 0xFF),
                IsFilled = false,
                IsClosed = false
            };
        }

        public IArc CreateArc()
        {
            return new XArc()
            {
                Point1 = new XPoint(0.0, 0.0),
                Point2 = new XPoint(0.0, 0.0),
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
                Point1 = new XPoint(0.0, 0.0),
                Point2 = new XPoint(0.0, 0.0),
                HorizontalAlignment = 1,
                VerticalAlignment = 1,
                Size = 11.0,
                Text = "Text",
                Foreground = new XColor(0xFF, 0x00, 0x00, 0x00),
                Backgroud = new XColor(0x00, 0xFF, 0xFF, 0xFF),
            };
        }

        public IBlock CreateBlock()
        {
            return new XBlock();
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
