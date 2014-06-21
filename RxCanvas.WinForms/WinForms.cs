using RxCanvas.Core;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace RxCanvas.WinForms
{
    public class WinFormsCanvasPanel : Panel
    {
        public ICanvas Canvas { get; set; }

        public WinFormsCanvasPanel()
        {
            this.SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Draw(e.Graphics, Canvas);
        }

        private void Draw(Graphics g, ICanvas canvas)
        {
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
            g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
            g.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
            g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;

            g.Clear(Color.White);

            if (Canvas == null)
            {
                return;
            }

            foreach (var child in canvas.Children)
            {
                if (child is ILine)
                {
                    var line = child as ILine;
                    Pen pen = new Pen(Color.FromArgb(line.Stroke.A, line.Stroke.R, line.Stroke.G, line.Stroke.B), (float)line.StrokeThickness);
                    g.DrawLine(pen, (float)line.Point1.X, (float)line.Point1.Y, (float)line.Point2.X, (float)line.Point2.Y);
                    pen.Dispose();
                }
                else if (child is IBezier)
                {
                    var bezier = child as IBezier;
                    Pen pen = new Pen(Color.FromArgb(bezier.Stroke.A, bezier.Stroke.R, bezier.Stroke.G, bezier.Stroke.B), (float)bezier.StrokeThickness);
                    g.DrawBezier(pen,
                        (float)bezier.Start.X, (float)bezier.Start.Y,
                        (float)bezier.Point1.X, (float)bezier.Point1.Y,
                        (float)bezier.Point2.X, (float)bezier.Point2.Y,
                        (float)bezier.Point3.X, (float)bezier.Point3.Y);
                    pen.Dispose();
                }
                else if (child is IQuadraticBezier)
                {
                    var quadraticBezier = child as IQuadraticBezier;
                    Pen pen = new Pen(Color.FromArgb(quadraticBezier.Stroke.A, quadraticBezier.Stroke.R, quadraticBezier.Stroke.G, quadraticBezier.Stroke.B), (float)quadraticBezier.StrokeThickness);
                    double x1 = quadraticBezier.Start.X;
                    double y1 = quadraticBezier.Start.Y;
                    double x2 = quadraticBezier.Start.X + (2.0 * (quadraticBezier.Point1.X - quadraticBezier.Start.X)) / 3.0;
                    double y2 = quadraticBezier.Start.Y + (2.0 * (quadraticBezier.Point1.Y - quadraticBezier.Start.Y)) / 3.0;
                    double x3 = x2 + (quadraticBezier.Point2.X - quadraticBezier.Start.X) / 3.0;
                    double y3 = y2 + (quadraticBezier.Point2.Y - quadraticBezier.Start.Y) / 3.0;
                    double x4 = quadraticBezier.Point2.X;
                    double y4 = quadraticBezier.Point2.Y;
                    g.DrawBezier(pen,
                        (float)x1, (float)y1,
                        (float)x2, (float)y2,
                        (float)x3, (float)y3,
                        (float)x4, (float)y4);
                    pen.Dispose();
                }
                else if (child is IArc)
                {
                    var arc = child as IArc;
                    if (arc.Width > 0.0 && arc.Height > 0.0)
                    {
                        Pen pen = new Pen(Color.FromArgb(arc.Stroke.A, arc.Stroke.R, arc.Stroke.G, arc.Stroke.B), (float)arc.StrokeThickness);
                        g.DrawArc(pen, (float)arc.X, (float)arc.Y, (float)arc.Width, (float)arc.Height, (float)arc.StartAngle, (float)arc.SweepAngle);
                        pen.Dispose();
                    }
                }
                else if (child is IRectangle)
                {
                    var rectangle = child as IRectangle;
                    Pen pen = new Pen(Color.FromArgb(rectangle.Stroke.A, rectangle.Stroke.R, rectangle.Stroke.G, rectangle.Stroke.B), (float)rectangle.StrokeThickness);
                    g.DrawRectangle(pen, (float)rectangle.X, (float)rectangle.Y, (float)rectangle.Width, (float)rectangle.Height);
                    pen.Dispose();
                }
                else if (child is IEllipse)
                {
                    var ellipse = child as IEllipse;
                    Pen pen = new Pen(Color.FromArgb(ellipse.Stroke.A, ellipse.Stroke.R, ellipse.Stroke.G, ellipse.Stroke.B), (float)ellipse.StrokeThickness);
                    g.DrawEllipse(pen, (float)ellipse.X, (float)ellipse.Y, (float)ellipse.Width, (float)ellipse.Height);
                    pen.Dispose();
                }
                else if (child is IText)
                {
                    var text = child as IText;
                    Brush brush = new SolidBrush(Color.FromArgb(text.Foreground.A, text.Foreground.R, text.Foreground.G, text.Foreground.B));
                    Font font = new Font("Callibri", (float)text.Size);
                    g.DrawString(text.Text, font, brush,
                        new RectangleF((float)text.X, (float)text.Y, (float)text.Width, (float)text.Height),
                        new StringFormat() { Alignment = (StringAlignment)text.HorizontalAlignment, LineAlignment = (StringAlignment)text.VerticalAlignment });
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

        public IList<INative> Children { get; set; }

        public double Width 
        {
            get { return _control.Width; }
            set { _control.Width = (int)value; }
        }

        public double Height
        {
            get { return _control.Height; }
            set { _control.Height = (int)value; }
        }

        public IColor Background { get; set; }

        public bool EnableSnap { get; set; }
        public double SnapX { get; set; }
        public double SnapY { get; set; }

        public bool IsCaptured { get; set; }

        private WinFormsCanvasPanel _control;

        public double Snap(double val, double snap)
        {
            double r = val % snap;
            return r >= snap / 2.0 ? val + snap - r : val - r;
        }

        public WinFormsCanvas(ICanvas canvas, WinFormsCanvasPanel control)
        {
            Background = canvas.Background;
            SnapX = canvas.SnapX;
            SnapY = canvas.SnapY;
            EnableSnap = canvas.EnableSnap;

            Children = new ObservableCollection<INative>();

            Downs = Observable.FromEventPattern<MouseEventArgs>(control, "MouseDown").Select(e =>
            {
                var p = e.EventArgs.Location;
                return new ImmutablePoint(EnableSnap ? Snap((double)p.X, SnapX) : (double)p.X,
                    EnableSnap ? Snap((double)p.Y, SnapY) : (double)p.Y);
            });

            Ups = Observable.FromEventPattern<MouseEventArgs>(control, "MouseUp").Select(e =>
            {
                var p = e.EventArgs.Location;
                return new ImmutablePoint(EnableSnap ? Snap((double)p.X, SnapX) : (double)p.X,
                    EnableSnap ? Snap((double)p.Y, SnapY) : (double)p.Y);
            });

            Moves = Observable.FromEventPattern<MouseEventArgs>(control, "MouseMove").Select(e =>
            {
                var p = e.EventArgs.Location;
                return new ImmutablePoint(EnableSnap ? Snap((double)p.X, SnapX) : (double)p.X,
                    EnableSnap ? Snap((double)p.Y, SnapY) : (double)p.Y);
            });

            _control = control;
            _control.Canvas = this;

            Native = control;
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
            _control.Invalidate();
        }
    }

    public class XModelToWinFormsConverter : IModelToNativeConverter
    {
        private WinFormsCanvasPanel _control;

        public XModelToWinFormsConverter(WinFormsCanvasPanel control)
        {
            _control = control;
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

        public ICanvas Convert(ICanvas canvas)
        {
            return new WinFormsCanvas(canvas, _control);
        }
    }
}
