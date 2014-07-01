using Autofac;
using RxCanvas.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RxCanvas.Views
{
    public class MainView
    {
        public IList<IEditor> Editors { get; set; }
        public IList<IFile<ICanvas, Stream>> Files { get; set; }
        public IList<ICreator<ICanvas>> Creators { get; set; }
        public ICanvas BackgroundCanvas { get; set; }
        public ICanvas DrawingCanvas { get; set; }

        private ILifetimeScope _backgroundScope;
        private ILifetimeScope _drawingScope;

        public MainView()
        {
            var bootstrapper = new Bootstrapper();
            var container = bootstrapper.Build();

            // create scopes
            _backgroundScope = container.BeginLifetimeScope();
            _drawingScope = container.BeginLifetimeScope();

            // resolve dependencies
            BackgroundCanvas = _backgroundScope.Resolve<ICanvas>();
            DrawingCanvas = _drawingScope.Resolve<ICanvas>();

            Editors = _drawingScope.Resolve<IList<IEditor>>();
            Files = _drawingScope.Resolve<IList<IFile<ICanvas, Stream>>>();
            Creators = _drawingScope.Resolve<IList<ICreator<ICanvas>>>();

            // default editor
            Editors.Where(e => e.Name == "Line")
                .FirstOrDefault()
                .IsEnabled = true;
        }

        public void Open(string path, int index)
        {
            var xcanvas = Files[index].Open(path);
            AsNative(xcanvas);
        }

        public void Save(string path, int index)
        {
            var xcanvas = ToModel();
            Files[index].Save(path, xcanvas);
        }

        public void Export(string path, int index)
        {
            var xcanvas = ToModel();
            Creators[index].Save(path, xcanvas);
        }

        public void Enable(IEditor editor)
        {
            for (int i = 0; i < Editors.Count; i++)
            {
                Editors[i].IsEnabled = false;
            }

            editor.IsEnabled = true;
        }

        public void ToggleSnap()
        {
            var drawingCanvas = _drawingScope.Resolve<ICanvas>();
            drawingCanvas.EnableSnap = drawingCanvas.EnableSnap ? false : true;
        }

        public void Undo()
        {
            var drawingCanvas = _drawingScope.Resolve<ICanvas>();
            var xcanvas = drawingCanvas.History.Undo(drawingCanvas);
            if (xcanvas != null)
            {
                AsNative(xcanvas);
                Render();
            }
        }

        public void Redo()
        {
            var drawingCanvas = _drawingScope.Resolve<ICanvas>();
            var xcanvas = drawingCanvas.History.Redo(drawingCanvas);
            if (xcanvas != null)
            {
                AsNative(xcanvas);
                Render();
            }
        }

        public void Clear()
        {
            var drawingCanvas = _drawingScope.Resolve<ICanvas>();
            drawingCanvas.History.Snapshot(drawingCanvas);
            drawingCanvas.Clear();
            drawingCanvas.Render(null);
        }

        public void Render()
        {
            var backgroundCanvas = _backgroundScope.Resolve<ICanvas>();
            var drawingCanvas = _drawingScope.Resolve<ICanvas>();
            backgroundCanvas.Render(null);
            drawingCanvas.Render(null);
        }

        public void CreateGrid(
            double width, 
            double height, 
            double size, 
            double originX, 
            double originY)
        {
            var backgroundCanvas = _backgroundScope.Resolve<ICanvas>();
            var nativeConverter = _backgroundScope.Resolve<INativeConverter>();
            var canvasFactory = _backgroundScope.Resolve<ICanvasFactory>();

            double thickness = 2.0;

            var stroke = canvasFactory.CreateColor();
            stroke.A = 0xFF;
            stroke.R = 0xE8;
            stroke.G = 0xE8;
            stroke.B = 0xE8;

            // horizontal
            for (double y = size; y < height; y += size)
            {
                var xline = canvasFactory.CreateLine();
                xline.Point1.X = originX;
                xline.Point1.Y = y;
                xline.Point2.X = width;
                xline.Point2.Y = y;
                xline.Stroke = stroke;
                xline.StrokeThickness = thickness;
                var nline = nativeConverter.Convert(xline);
                backgroundCanvas.Add(nline);
            }

            // vertical lines
            for (double x = size; x < width; x += size)
            {
                var xline = canvasFactory.CreateLine();
                xline.Point1.X = x;
                xline.Point1.Y = originY;
                xline.Point2.X = x;
                xline.Point2.Y = height;
                xline.Stroke = stroke;
                xline.StrokeThickness = thickness;
                var nline = nativeConverter.Convert(xline);
                backgroundCanvas.Add(nline);
            }
        }

        private ICanvas ToModel()
        {
            var drawingCanvas = _drawingScope.Resolve<ICanvas>();
            var modelConverter = _drawingScope.Resolve<IModelConverter>();
            return modelConverter.Convert(drawingCanvas);
        }

        public void AsNative(ICanvas xcanvas)
        {
            var nativeConverter = _drawingScope.Resolve<INativeConverter>();
            var canvasFactory = _drawingScope.Resolve<ICanvasFactory>();
            var drawingCanvas = _drawingScope.Resolve<ICanvas>();
            var boundsFactory = _drawingScope.Resolve<IBoundsFactory>();

            drawingCanvas.Clear();

            AsNative(
                nativeConverter,
                boundsFactory,
                drawingCanvas,
                xcanvas.Children);
        }

        private void AsNative(
            INativeConverter nativeConverter,
            IBoundsFactory boundsFactory,
            ICanvas nativeCanvas,
            IList<INative> xchildren)
        {
            foreach (var child in xchildren)
            {
                if (child is ILine)
                {
                    var native = nativeConverter.Convert(child as ILine);
                    nativeCanvas.Add(native);

                    native.Bounds = boundsFactory.Create(nativeCanvas, native);
                    if (native.Bounds != null)
                    {
                        native.Bounds.Update();
                    }
                }
                else if (child is IBezier)
                {
                    var native = nativeConverter.Convert(child as IBezier);
                    nativeCanvas.Add(native);

                    native.Bounds = boundsFactory.Create(nativeCanvas, native);
                    if (native.Bounds != null)
                    {
                        native.Bounds.Update();
                    }
                }
                else if (child is IQuadraticBezier)
                {
                    var native = nativeConverter.Convert(child as IQuadraticBezier);
                    nativeCanvas.Add(native);

                    native.Bounds = boundsFactory.Create(nativeCanvas, native);
                    if (native.Bounds != null)
                    {
                        native.Bounds.Update();
                    }
                }
                else if (child is IArc)
                {
                    var native = nativeConverter.Convert(child as IArc);
                    nativeCanvas.Add(native);

                    native.Bounds = boundsFactory.Create(nativeCanvas, native);
                    if (native.Bounds != null)
                    {
                        native.Bounds.Update();
                    }
                }
                else if (child is IRectangle)
                {
                    var native = nativeConverter.Convert(child as IRectangle);
                    nativeCanvas.Add(native);

                    native.Bounds = boundsFactory.Create(nativeCanvas, native);
                    if (native.Bounds != null)
                    {
                        native.Bounds.Update();
                    }
                }
                else if (child is IEllipse)
                {
                    var native = nativeConverter.Convert(child as IEllipse);
                    nativeCanvas.Add(native);

                    native.Bounds = boundsFactory.Create(nativeCanvas, native);
                    if (native.Bounds != null)
                    {
                        native.Bounds.Update();
                    }
                }
                else if (child is IText)
                {
                    var native = nativeConverter.Convert(child as IText);
                    nativeCanvas.Add(native);

                    native.Bounds = boundsFactory.Create(nativeCanvas, native);
                    if (native.Bounds != null)
                    {
                        native.Bounds.Update();
                    }
                }
                else if (child is IBlock)
                {
                    var block = child as IBlock;
                    nativeCanvas.Add(block);

                    AsNative(
                        nativeConverter,
                        boundsFactory,
                        nativeCanvas,
                        block.Children);
                }
                else
                {
                    throw new NotSupportedException();
                }
            }
        }
    }
}
