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
        public ICanvas Canvas { get; set; }

        public WinFormsCanvasPanel()
        {
            this.SetStyle(
                ControlStyles.UserPaint 
                | ControlStyles.AllPaintingInWmPaint 
                | ControlStyles.OptimizedDoubleBuffer,
                true);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Draw(e.Graphics, Canvas);
        }

        private Color ToColor(IColor color)
        {
            return Color.FromArgb(color.A, color.R, color.G, color.B);
        }
       
        private void Draw(Graphics g, ICanvas canvas)
        {
            g.SmoothingMode = SmoothingMode.HighQuality;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
            g.CompositingQuality = CompositingQuality.HighQuality;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;

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
                    Pen pen = new Pen(
                        ToColor(line.Stroke), 
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
                        ToColor(bezier.Stroke), 
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
                        ToColor(quadraticBezier.Stroke), 
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
                    if (arc.Width > 0.0 && arc.Height > 0.0)
                    {
                        Pen pen = new Pen(
                            ToColor(arc.Stroke), 
                            (float)arc.StrokeThickness);

                        g.DrawArc(
                            pen, 
                            (float)arc.X, 
                            (float)arc.Y, 
                            (float)arc.Width, 
                            (float)arc.Height, 
                            (float)arc.StartAngle, 
                            (float)arc.SweepAngle);

                        pen.Dispose();
                    }
                }
                else if (child is IRectangle)
                {
                    var rectangle = child as IRectangle;
                    Pen pen = new Pen(
                        ToColor(rectangle.Stroke), 
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
                        ToColor(ellipse.Stroke), 
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
                    Brush brush = new SolidBrush(ToColor(text.Foreground));
                    Font font = new Font("Callibri", (float)text.Size);

                    g.DrawString(
                        text.Text, 
                        font, 
                        brush,
                        new RectangleF(
                            (float)text.X, 
                            (float)text.Y, 
                            (float)text.Width, 
                            (float)text.Height),
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
        private readonly WinFormsCanvasPanel _control;

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
