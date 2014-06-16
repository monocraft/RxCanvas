﻿using System;
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

    public class CoreToModelConverter : ICoreToModelConverter
    {
        public IColor Convert(IColor color)
        {
            return new XColor(color.A, color.R, color.G, color.B);
        }

        public IPoint Convert(IPoint point)
        {
            return new XPoint(point.X, point.Y);
        }

        public ILine Convert(ILine line)
        {
            return new XLine()
            {
                X1 = line.X1,
                Y1 = line.Y1,
                X2 = line.X2,
                Y2 = line.Y2,
                Stroke = Convert(line.Stroke),
                StrokeThickness = line.StrokeThickness
            };
        }

        public IBezier Convert(IBezier bezier)
        {
            return new XBezier()
            {
                Start = Convert(bezier.Start),
                Point1 = Convert(bezier.Point1),
                Point2 = Convert(bezier.Point2),
                Point3 = Convert(bezier.Point3),
                Fill = Convert(bezier.Fill),
                Stroke = Convert(bezier.Stroke),
                StrokeThickness = bezier.StrokeThickness,
                IsClosed = bezier.IsClosed
            };
        }

        public IQuadraticBezier Convert(IQuadraticBezier quadraticBezier)
        {
            return new XQuadraticBezier()
            {
                Start = Convert(quadraticBezier.Start),
                Point1 = Convert(quadraticBezier.Point1),
                Point2 = Convert(quadraticBezier.Point2),
                Fill = Convert(quadraticBezier.Fill),
                Stroke = Convert(quadraticBezier.Stroke),
                StrokeThickness = quadraticBezier.StrokeThickness,
                IsClosed = quadraticBezier.IsClosed
            };
        }

        public IArc Convert(IArc arc)
        {
            return new XArc()
            {
                X = arc.X,
                Y = arc.Y,
                Width = arc.Width,
                Height = arc.Height,
                StartAngle = arc.StartAngle,
                SweepAngle = arc.SweepAngle,
                Stroke = Convert(arc.Stroke),
                StrokeThickness = arc.StrokeThickness,
                Fill = Convert(arc.Fill),
                IsFilled = arc.IsFilled
            };
        }

        public IRectangle Convert(IRectangle rectangle)
        {
            return new XRectangle()
            {
                X = rectangle.X,
                Y = rectangle.Y,
                Width = rectangle.Width,
                Height = rectangle.Height,
                Stroke = Convert(rectangle.Stroke),
                StrokeThickness = rectangle.StrokeThickness,
                Fill = Convert(rectangle.Fill),
                IsFilled = rectangle.IsFilled
            };
        }

        public IEllipse Convert(IEllipse ellipse)
        {
            return new XEllipse()
            {
                X = ellipse.X,
                Y = ellipse.Y,
                Width = ellipse.Width,
                Height = ellipse.Height,
                Stroke = Convert(ellipse.Stroke),
                StrokeThickness = ellipse.StrokeThickness,
                Fill = Convert(ellipse.Fill),
                IsFilled = ellipse.IsFilled
            };
        }

        public INative Convert(INative native)
        {
            if (native is ILine)
            {
                return Convert(native as ILine);
            }
            else if (native is IBezier)
            {
                return Convert(native as IBezier);
            }
            else if (native is IQuadraticBezier)
            {
                return Convert(native as IQuadraticBezier);
            }
            else if (native is IArc)
            {
                return Convert(native as IArc);
            }
            else if (native is IRectangle)
            {
                return Convert(native as IRectangle);
            }
            else if (native is IEllipse)
            {
                return Convert(native as IEllipse);
            }
            else
            {
                throw new NotSupportedException();
            }
        }

        public ICanvas Convert(ICanvas canvas)
        {
            var xcanvas = new XCanvas()
            {
                Width = canvas.Width,
                Height = canvas.Height,
                Background = Convert(canvas.Background),
                SnapX = canvas.SnapX,
                SnapY = canvas.SnapY,
                EnableSnap = canvas.EnableSnap
            };
            var children = new List<INative>();
            foreach (var child in canvas.Children)
            {
                children.Add(Convert(child));
            }
            xcanvas.Children = children;
            return xcanvas;
        }
    }
}
