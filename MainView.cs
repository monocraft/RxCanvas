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
        private ILifetimeScope _backgroundScope;
        private ILifetimeScope _drawingScope;

        public ICollection<IEditor> Editors { get; set; }
        public IList<IFile<ICanvas, Stream>> Files { get; set; }
        public IList<ICreator<ICanvas>> Creators { get; set; }
        public ICanvas BackgroundCanvas { get; set; }
        public ICanvas DrawingCanvas { get; set; }

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

            Editors = _drawingScope.Resolve<ICollection<IEditor>>();
            Files = _drawingScope.Resolve<IList<IFile<ICanvas, Stream>>>();
            Creators = _drawingScope.Resolve<IList<ICreator<ICanvas>>>();

            // set default editor
            Editors.Where(e => e.Name == "Line").FirstOrDefault().IsEnabled = true;
        }

        public void Open(string path, int index)
        {
            var xcanvas = Files[index].Open(path);
            Open(xcanvas);
        }

        public void Save(string path, int index)
        {
            var xcanvas = ConvertToModel();
            Files[index].Save(path, xcanvas);
            //SaveBlockDemo(path, index);
        }

        private void SaveBlockDemo(string path, int index)
        {
            var canvasFactory = _drawingScope.Resolve<ICanvasFactory>();
            var xcanvas = canvasFactory.CreateCanvas();

            var xblock = canvasFactory.CreateBlock();
            var xline = canvasFactory.CreateLine();
            xline.Point1.X = 150.0;
            xline.Point1.Y = 150.0;
            xline.Point2.X = 300.0;
            xline.Point2.Y = 150.0;
            xblock.Children.Add(xline);
            xcanvas.Add(xblock);

            Files[index].Save(path, xcanvas);
        }

        public void Export(string path, int index)
        {
            var canvas = ConvertToModel();
            var creator = Creators[index];
            creator.Save(path, canvas);
        }

        private void Open(ICanvas xcanvas)
        {
            var nativeConverter = _drawingScope.Resolve<INativeConverter>();
            var canvasFactory = _drawingScope.Resolve<ICanvasFactory>();
            var drawingCanvas = _drawingScope.Resolve<ICanvas>();
            var boundsFactory = _drawingScope.Resolve<IBoundsFactory>();

            drawingCanvas.Clear();

            Add(nativeConverter, drawingCanvas, boundsFactory, xcanvas.Children);
        }

        private void Add(
            INativeConverter nativeConverter,
            ICanvas drawingCanvas,
            IBoundsFactory boundsFactory,
            IList<INative> children)
        {
            foreach (var child in children)
            {
                if (child is ILine)
                {
                    var native = nativeConverter.Convert(child as ILine);
                    drawingCanvas.Add(native);

                    native.Bounds = boundsFactory.Create(drawingCanvas, native);
                    if (native.Bounds != null)
                    {
                        native.Bounds.Update();
                    }
                }
                else if (child is IBezier)
                {
                    var native = nativeConverter.Convert(child as IBezier);
                    drawingCanvas.Add(native);

                    native.Bounds = boundsFactory.Create(drawingCanvas, native);
                    if (native.Bounds != null)
                    {
                        native.Bounds.Update();
                    }
                }
                else if (child is IQuadraticBezier)
                {
                    var native = nativeConverter.Convert(child as IQuadraticBezier);
                    drawingCanvas.Add(native);

                    native.Bounds = boundsFactory.Create(drawingCanvas, native);
                    if (native.Bounds != null)
                    {
                        native.Bounds.Update();
                    }
                }
                else if (child is IArc)
                {
                    var native = nativeConverter.Convert(child as IArc);
                    drawingCanvas.Add(native);

                    native.Bounds = boundsFactory.Create(drawingCanvas, native);
                    if (native.Bounds != null)
                    {
                        native.Bounds.Update();
                    }
                }
                else if (child is IRectangle)
                {
                    var native = nativeConverter.Convert(child as IRectangle);
                    drawingCanvas.Add(native);

                    native.Bounds = boundsFactory.Create(drawingCanvas, native);
                    if (native.Bounds != null)
                    {
                        native.Bounds.Update();
                    }
                }
                else if (child is IEllipse)
                {
                    var native = nativeConverter.Convert(child as IEllipse);
                    drawingCanvas.Add(native);

                    native.Bounds = boundsFactory.Create(drawingCanvas, native);
                    if (native.Bounds != null)
                    {
                        native.Bounds.Update();
                    }
                }
                else if (child is IText)
                {
                    var native = nativeConverter.Convert(child as IText);
                    drawingCanvas.Add(native);

                    native.Bounds = boundsFactory.Create(drawingCanvas, native);
                    if (native.Bounds != null)
                    {
                        native.Bounds.Update();
                    }
                }
                else if (child is IBlock)
                {
                    var block = child as IBlock;
                    drawingCanvas.Add(block);

                    Add(nativeConverter, drawingCanvas, boundsFactory, block.Children);
                }
                else
                {
                    throw new NotSupportedException();
                }
            }
        }

        private ICanvas ConvertToModel()
        {
            var drawingCanvas = _drawingScope.Resolve<ICanvas>();
            var modelConverter = _drawingScope.Resolve<IModelConverter>();
            var canvas = modelConverter.Convert(drawingCanvas);
            return canvas;
        }

        public void CreateGrid()
        {
            var backgroundCanvas = _backgroundScope.Resolve<ICanvas>();
            var nativeConverter = _backgroundScope.Resolve<INativeConverter>();
            var canvasFactory = _backgroundScope.Resolve<ICanvasFactory>();
            CreateGrid(nativeConverter, canvasFactory, backgroundCanvas, 600.0, 600.0, 30.0, 0.0, 0.0);
        }

        private INative CreateGridLine(
            INativeConverter nativeConverter,
            ICanvasFactory canvasFactory,
            IColor stroke,
            double thickness,
            double x1, double y1,
            double x2, double y2)
        {
            var xline = canvasFactory.CreateLine();
            xline.Point1.X = x1;
            xline.Point1.Y = y1;
            xline.Point2.X = x2;
            xline.Point2.Y = y2;
            xline.Stroke = stroke;
            xline.StrokeThickness = thickness;
            return nativeConverter.Convert(xline);
        }

        private void CreateGrid(
            INativeConverter nativeConverter,
            ICanvasFactory canvasFactory,
            ICanvas canvas,
            double width, double height,
            double size,
            double originX, double originY)
        {
            double thickness = 2.0;
            var stroke = canvasFactory.CreateColor();
            stroke.A = 0xFF;
            stroke.R = 0xE8;
            stroke.G = 0xE8;
            stroke.B = 0xE8;

            for (double y = size; y < height; y += size)
            {
                canvas.Add(CreateGridLine(nativeConverter, canvasFactory, stroke, thickness, originX, y, width, y));
            }

            for (double x = size; x < width; x += size)
            {
                canvas.Add(CreateGridLine(nativeConverter, canvasFactory, stroke, thickness, x, originY, x, height));
            }
        }

        public void EnableEditor(IEditor _editor)
        {
            foreach (var editor in Editors)
            {
                editor.IsEnabled = false;
            };
            _editor.IsEnabled = true;
        }

        public void ToggleSnap()
        {
            var drawingCanvas = _drawingScope.Resolve<ICanvas>();
            drawingCanvas.EnableSnap = drawingCanvas.EnableSnap ? false : true;
        }

        public void Clear()
        {
            var drawingCanvas = _drawingScope.Resolve<ICanvas>();
            drawingCanvas.History.Snapshot(drawingCanvas);
            drawingCanvas.Clear();
            drawingCanvas.Render(null);
        }

        public void Undo()
        {
            var drawingCanvas = _drawingScope.Resolve<ICanvas>();
            var xcanvas = drawingCanvas.History.Undo(drawingCanvas);
            if (xcanvas != null)
            {
                Open(xcanvas);
                Render();
            }
        }

        public void Redo()
        {
            var drawingCanvas = _drawingScope.Resolve<ICanvas>();
            var xcanvas = drawingCanvas.History.Redo(drawingCanvas);
            if (xcanvas != null)
            {
                Open(xcanvas);
                Render();
            }
        }

        public void Render()
        {
            var backgroundCanvas = _backgroundScope.Resolve<ICanvas>();
            var drawingCanvas = _drawingScope.Resolve<ICanvas>();
            backgroundCanvas.Render(null);
            drawingCanvas.Render(null);
        }
    }
}
