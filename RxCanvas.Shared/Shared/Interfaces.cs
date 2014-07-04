using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RxCanvas.Interfaces
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

    public interface IPolygon
    {
        IPoint[] Points { get; set; }
        ILine[] Lines { get; set; }
        bool Contains(IPoint point);
        bool Contains(double x, double y);
    }

    public interface IBounds
    {
        void Update();
        bool IsVisible();
        void Show();
        void Hide();
        bool Contains(double x, double y);
        void Move(double dx, double dy);
    }

    public interface INative
    {
        object Native { get; set; }
        IBounds Bounds { get; set; }
    }

    public interface ILine : INative
    {
        IPoint Point1 { get; set; }
        IPoint Point2 { get; set; }
        IColor Stroke { get; set; }
        double StrokeThickness { get; set; }
    }

    public interface IBezier : INative
    {
        IPoint Start { get; set; }
        IPoint Point1 { get; set; }
        IPoint Point2 { get; set; }
        IPoint Point3 { get; set; }
        IColor Stroke { get; set; }
        double StrokeThickness { get; set; }
        IColor Fill { get; set; }
        bool IsFilled { get; set; }
        bool IsClosed { get; set; }
    }

    public interface IQuadraticBezier : INative
    {
        IPoint Start { get; set; }
        IPoint Point1 { get; set; }
        IPoint Point2 { get; set; }
        IColor Stroke { get; set; }
        double StrokeThickness { get; set; }
        IColor Fill { get; set; }
        bool IsFilled { get; set; }
        bool IsClosed { get; set; }
    }

    public interface IArc : INative
    {
        IPoint Point1 { get; set; }
        IPoint Point2 { get; set; }
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
        IPoint Point1 { get; set; }
        IPoint Point2 { get; set; }
        IColor Stroke { get; set; }
        double StrokeThickness { get; set; }
        IColor Fill { get; set; }
    }

    public interface IEllipse : INative
    {
        IPoint Point1 { get; set; }
        IPoint Point2 { get; set; }
        IColor Stroke { get; set; }
        double StrokeThickness { get; set; }
        IColor Fill { get; set; }
    }

    public interface IText : INative
    {
        IPoint Point1 { get; set; }
        IPoint Point2 { get; set; }
        int HorizontalAlignment { get; set; }
        int VerticalAlignment { get; set; }
        double Size { get; set; }
        string Text { get; set; }
        IColor Foreground { get; set; }
        IColor Backgroud { get; set; }
    }

    public interface IBlock : INative
    {
        IList<INative> Children { get; set; }
    }

    public interface ICanvas : INative
    {
        IObservable<ImmutablePoint> Downs { get; set; }
        IObservable<ImmutablePoint> Ups { get; set; }
        IObservable<ImmutablePoint> Moves { get; set; }
        IHistory History { get; set; }
        double Width { get; set; }
        double Height { get; set; }
        IColor Background { get; set; }
        bool EnableSnap { get; set; }
        double SnapX { get; set; }
        double SnapY { get; set; }
        double Snap(double val, double snap);
        IList<INative> Children { get; set; }
        bool IsCaptured { get; set; }
        void Capture();
        void ReleaseCapture();
        void Add(INative value);
        void Remove(INative value);
        void Clear();
        void Render(INative context);
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
        IPolygon CreatePolygon();
        ILine CreateLine();
        IBezier CreateBezier();
        IQuadraticBezier CreateQuadraticBezier();
        IArc CreateArc();
        IRectangle CreateRectangle();
        IEllipse CreateEllipse();
        IText CreateText();
        IBlock CreateBlock();
        ICanvas CreateCanvas();
    }

    public interface IConverter
    {
        ILine Convert(ILine line);
        IBezier Convert(IBezier bezier);
        IQuadraticBezier Convert(IQuadraticBezier quadraticBezier);
        IArc Convert(IArc arc);
        IRectangle Convert(IRectangle rectangle);
        IEllipse Convert(IEllipse ellipse);
        IText Convert(IText text);
        IBlock Convert(IBlock block);
        ICanvas Convert(ICanvas canvas);
    }

    public interface IModelConverter : IConverter
    { 
    }

    public interface INativeConverter : IConverter
    { 
    }

    public interface IBoundsFactory
    {
        IBounds Create(ICanvas canvas, IPoint point);
        IBounds Create(ICanvas canvas, ILine line);
        IBounds Create(ICanvas canvas, IBezier bezier);
        IBounds Create(ICanvas canvas, IQuadraticBezier quadraticBezier);
        IBounds Create(ICanvas canvas, IArc arc);
        IBounds Create(ICanvas canvas, IRectangle rectangle);
        IBounds Create(ICanvas canvas, IEllipse ellipse);
        IBounds Create(ICanvas canvas, IText text);
    }

    public interface ICreator
    {
        string Name { get; set; }
        string Extension { get; set; }
        void Save(string path, ICanvas item);
        void Save(string path, IEnumerable<ICanvas> items);
    }

    public interface IFile
    {
        string Name { get; set; }
        string Extension { get; set; }
        ICanvas Open(string path);
        void Save(string path, ICanvas value);
        ICanvas Read(Stream stream);
        void Write(Stream stream, ICanvas value);
    }

    public interface IHistory
    {
        void Snapshot(ICanvas canvas);
        ICanvas Undo(ICanvas canvas);
        ICanvas Redo(ICanvas canvas);
        void Clear();
    }
}
