﻿using RxCanvas.Binary;
using RxCanvas.Interfaces;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace RxCanvas.WinForms
{
    public class WinFormsCanvasPanel : Panel
    {
        public IList<ICanvas> Layers { get; set; }

        public WinFormsCanvasPanel()
        {
            this.SetStyle(
                ControlStyles.UserPaint 
                | ControlStyles.AllPaintingInWmPaint 
                | ControlStyles.OptimizedDoubleBuffer
                | ControlStyles.SupportsTransparentBackColor,
                true);
            
            this.BackColor = Color.Transparent;

            this.Layers = new List<ICanvas>();
        }

        protected override CreateParams CreateParams
        {
            get
            {
                var cp = base.CreateParams;
                cp.ExStyle |= 0x00000020; // WS_EX_TRANSPARENT
                return cp;
            }
        }

        protected override void OnPaintBackground(PaintEventArgs e) 
        { 
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Draw(e.Graphics, Layers);
        }

        private Color ToNativeColor(IColor color)
        {
            return Color.FromArgb(color.A, color.R, color.G, color.B);
        }

        private void Draw(Graphics g, IList<ICanvas> layers)
        {
            g.SmoothingMode = SmoothingMode.HighQuality;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
            g.CompositingQuality = CompositingQuality.HighQuality;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;

            g.Clear(ToNativeColor(layers.FirstOrDefault().Background));

            for (int i = 0; i < layers.Count; i++)
            {
                DrawLayer(g, layers[i]);
            }
        }

        private void DrawLayer(Graphics g, ICanvas layer)
        {
            foreach (var child in layer.Children)
            {
                if (child is ILine)
                {
                    var line = child as ILine;
                    Pen pen = new Pen(
                        ToNativeColor(line.Stroke),
                        (float)line.StrokeThickness);

                    g.DrawLine(
                        pen,
                        (float)line.Point1.X,
                        (float)line.Point1.Y,
                        (float)line.Point2.X,
                        (float)line.Point2.Y);

                    pen.Dispose();
                }
                else if (child is IBezier)
                {
                    var bezier = child as IBezier;
                    Pen pen = new Pen(
                        ToNativeColor(bezier.Stroke),
                        (float)bezier.StrokeThickness);

                    g.DrawBezier(
                        pen,
                        (float)bezier.Start.X,
                        (float)bezier.Start.Y,
                        (float)bezier.Point1.X,
                        (float)bezier.Point1.Y,
                        (float)bezier.Point2.X,
                        (float)bezier.Point2.Y,
                        (float)bezier.Point3.X,
                        (float)bezier.Point3.Y);

                    pen.Dispose();
                }
                else if (child is IQuadraticBezier)
                {
                    var quadraticBezier = child as IQuadraticBezier;
                    Pen pen = new Pen(
                        ToNativeColor(quadraticBezier.Stroke),
                        (float)quadraticBezier.StrokeThickness);

                    double x1 = quadraticBezier.Start.X;
                    double y1 = quadraticBezier.Start.Y;
                    double x2 = quadraticBezier.Start.X + (2.0 * (quadraticBezier.Point1.X - quadraticBezier.Start.X)) / 3.0;
                    double y2 = quadraticBezier.Start.Y + (2.0 * (quadraticBezier.Point1.Y - quadraticBezier.Start.Y)) / 3.0;
                    double x3 = x2 + (quadraticBezier.Point2.X - quadraticBezier.Start.X) / 3.0;
                    double y3 = y2 + (quadraticBezier.Point2.Y - quadraticBezier.Start.Y) / 3.0;
                    double x4 = quadraticBezier.Point2.X;
                    double y4 = quadraticBezier.Point2.Y;

                    g.DrawBezier(
                        pen,
                        (float)x1,
                        (float)y1,
                        (float)x2,
                        (float)y2,
                        (float)x3,
                        (float)y3,
                        (float)x4,
                        (float)y4);

                    pen.Dispose();
                }
                else if (child is IArc)
                {
                    var arc = child as IArc;

                    double x = Math.Min(arc.Point1.X, arc.Point2.X);
                    double y = Math.Min(arc.Point1.Y, arc.Point2.Y);
                    double width = Math.Abs(arc.Point2.X - arc.Point1.X);
                    double height = Math.Abs(arc.Point2.Y - arc.Point1.Y);

                    if (width > 0.0 && height > 0.0)
                    {
                        Pen pen = new Pen(
                            ToNativeColor(arc.Stroke),
                            (float)arc.StrokeThickness);

                        g.DrawArc(
                            pen,
                            (float)x,
                            (float)y,
                            (float)width,
                            (float)height,
                            (float)arc.StartAngle,
                            (float)arc.SweepAngle);

                        pen.Dispose();
                    }
                }
                else if (child is IRectangle)
                {
                    var rectangle = child as IRectangle;
                    Pen pen = new Pen(
                        ToNativeColor(rectangle.Stroke),
                        (float)rectangle.StrokeThickness);

                    double x = Math.Min(rectangle.Point1.X, rectangle.Point2.X);
                    double y = Math.Min(rectangle.Point1.Y, rectangle.Point2.Y);
                    double width = Math.Abs(rectangle.Point2.X - rectangle.Point1.X);
                    double height = Math.Abs(rectangle.Point2.Y - rectangle.Point1.Y);

                    g.DrawRectangle(
                        pen,
                        (float)(x),
                        (float)(y),
                        (float)(width),
                        (float)(height));

                    pen.Dispose();
                }
                else if (child is IEllipse)
                {
                    var ellipse = child as IEllipse;
                    Pen pen = new Pen(
                        ToNativeColor(ellipse.Stroke),
                        (float)ellipse.StrokeThickness);

                    double x = Math.Min(ellipse.Point1.X, ellipse.Point2.X);
                    double y = Math.Min(ellipse.Point1.Y, ellipse.Point2.Y);
                    double width = Math.Abs(ellipse.Point2.X - ellipse.Point1.X);
                    double height = Math.Abs(ellipse.Point2.Y - ellipse.Point1.Y);

                    g.DrawEllipse(
                        pen,
                        (float)(x),
                        (float)(y),
                        (float)(width),
                        (float)(height));

                    pen.Dispose();
                }
                else if (child is IText)
                {
                    var text = child as IText;
                    Brush brush = new SolidBrush(ToNativeColor(text.Foreground));
                    Font font = new Font("Callibri", (float)text.Size);

                    double x = Math.Min(text.Point1.X, text.Point2.X);
                    double y = Math.Min(text.Point1.Y, text.Point2.Y);
                    double width = Math.Abs(text.Point2.X - text.Point1.X);
                    double height = Math.Abs(text.Point2.Y - text.Point1.Y);

                    g.DrawString(
                        text.Text,
                        font,
                        brush,
                        new RectangleF(
                            (float)(x),
                            (float)(y),
                            (float)(width),
                            (float)(height)),
                        new StringFormat()
                        {
                            Alignment = (StringAlignment)text.HorizontalAlignment,
                            LineAlignment = (StringAlignment)text.VerticalAlignment
                        });

                    brush.Dispose();
                    font.Dispose();
                }
            }
        }
    }

    public class WinFormsCanvas : ICanvas
    {
        public object Native { get; set; }
        public IBounds Bounds { get; set; }

        public IObservable<ImmutablePoint> Downs { get; set; }
        public IObservable<ImmutablePoint> Ups { get; set; }
        public IObservable<ImmutablePoint> Moves { get; set; }

        public IHistory History { get; set; }

        public IList<INative> Children { get; set; }

        public double Width 
        {
            get { return _panel.Width; }
            set { _panel.Width = (int)value; }
        }

        public double Height
        {
            get { return _panel.Height; }
            set { _panel.Height = (int)value; }
        }

        public IColor Background { get; set; }

        public bool EnableSnap { get; set; }
        public double SnapX { get; set; }
        public double SnapY { get; set; }

        public bool IsCaptured { get; set; }

        private WinFormsCanvasPanel _panel;

        public double Snap(double val, double snap)
        {
            double r = val % snap;
            return r >= snap / 2.0 ? val + snap - r : val - r;
        }

        public WinFormsCanvas(ICanvas canvas, WinFormsCanvasPanel panel)
        {
            Background = canvas.Background;
            SnapX = canvas.SnapX;
            SnapY = canvas.SnapY;
            EnableSnap = canvas.EnableSnap;

            History = canvas.History;

            Children = new ObservableCollection<INative>();

            _panel = panel;
            _panel.Layers.Add(this);

            Downs = Observable.FromEventPattern<MouseEventArgs>(_panel, "MouseDown").Select(e =>
            {
                var p = e.EventArgs.Location;
                return new ImmutablePoint(EnableSnap ? Snap((double)p.X, SnapX) : (double)p.X,
                    EnableSnap ? Snap((double)p.Y, SnapY) : (double)p.Y);
            });

            Ups = Observable.FromEventPattern<MouseEventArgs>(_panel, "MouseUp").Select(e =>
            {
                var p = e.EventArgs.Location;
                return new ImmutablePoint(EnableSnap ? Snap((double)p.X, SnapX) : (double)p.X,
                    EnableSnap ? Snap((double)p.Y, SnapY) : (double)p.Y);
            });

            Moves = Observable.FromEventPattern<MouseEventArgs>(_panel, "MouseMove").Select(e =>
            {
                var p = e.EventArgs.Location;
                return new ImmutablePoint(EnableSnap ? Snap((double)p.X, SnapX) : (double)p.X,
                    EnableSnap ? Snap((double)p.Y, SnapY) : (double)p.Y);
            });

            Native = _panel;
        }

        public void Capture()
        {
            IsCaptured = true;
        }

        public void ReleaseCapture()
        {
            IsCaptured = false;
        }

        public void Add(INative value)
        {
            Children.Add(value);
        }

        public void Remove(INative value)
        {
            Children.Remove(value);
        }

        public void Clear()
        {
            Children.Clear();
        }

        public void Render(INative context)
        {
            _panel.Invalidate();
        }
    }

    public class WinFormsConverter : INativeConverter
    {
        private readonly WinFormsCanvasPanel _panel;

        public WinFormsConverter(WinFormsCanvasPanel panel)
        {
            _panel = panel;
        }

        public ILine Convert(ILine line)
        {
            return line;
        }

        public IBezier Convert(IBezier bezier)
        {
            return bezier;
        }

        public IQuadraticBezier Convert(IQuadraticBezier quadraticBezier)
        {
            return quadraticBezier;
        }

        public IArc Convert(IArc arc)
        {
            return arc;
        }

        public IRectangle Convert(IRectangle rectangle)
        {
            return rectangle;
        }

        public IEllipse Convert(IEllipse ellipse)
        {
            return ellipse;
        }

        public IText Convert(IText text)
        {
            return text;
        }

        public IBlock Convert(IBlock block)
        {
            return block;
        }

        public ICanvas Convert(ICanvas canvas)
        {
            return new WinFormsCanvas(canvas, _panel);
        }
    }
}