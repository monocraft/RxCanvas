﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RxCanvas.Core
{
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

        bool IsCaptured { get; set; }

        void Capture();
        void ReleaseCapture();

        void Add(INative value);
        void Remove(INative value);
        void Clear();
    }

    public interface IEditor
    {
        string Name { get; set; }
        bool IsEnabled { get; set; }
        string Key { get; set; }
        string Modifiers { get; set; }
    }

    public interface ICanvasFactory
    {
        IColor CreateColor();
        IPoint CreatePoint();
        ILine CreateLine();
        IBezier CreateBezier();
        IQuadraticBezier CreateQuadraticBezier();
        IArc CreateArc();
        IRectangle CreateRectangle();
        IEllipse CreateEllipse();
        ICanvas CreateCanvas();
    }

    public interface ICoreToModelConverter
    {
        ILine Convert(ILine line);
        IBezier Convert(IBezier bezier);
        IQuadraticBezier Convert(IQuadraticBezier quadraticBezier);
        IArc Convert(IArc arc);
        IRectangle Convert(IRectangle rectangle);
        IEllipse Convert(IEllipse ellipse);
        ICanvas Convert(ICanvas canvas);
    }

    public interface IModelToNativeConverter
    {
        ILine Convert(ILine line);
        IBezier Convert(IBezier bezier);
        IQuadraticBezier Convert(IQuadraticBezier quadraticBezier);
        IArc Convert(IArc arc);
        IRectangle Convert(IRectangle rectangle);
        IEllipse Convert(IEllipse ellipse);
        ICanvas Convert(ICanvas canvas);
    }

    public interface ICanvasSerializer
    {
        void Serialize(string path, ICanvas canvas);
        ICanvas Deserialize(string path);
    }
}