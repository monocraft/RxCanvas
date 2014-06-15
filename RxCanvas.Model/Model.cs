using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RxCanvas.Core;

namespace RxCanvas.Model
{
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

        public bool IsCaptured { get; set; }

        public XCanvas()
        {
            Children = new ObservableCollection<INative>();
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
    }
}
