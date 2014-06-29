using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Reactive.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Autofac;
using RxCanvas.Interfaces;
using System.IO;

namespace RxCanvas.WinForms
{
    public partial class Form1 : Form
    {
        private MainView _mainView;
        private IDictionary<Tuple<Keys, Keys>, Action> _shortcuts;

        public Form1()
        {
            InitializeComponent();
            this.SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.DoubleBuffer, true);

            _mainView = new MainView();
            _mainView.Build(this.canvasPanel1);

            InitlializeShortucts();
            Initialize();
        }

        private void InitlializeShortucts()
        {
            // shortcuts dictionary
            _shortcuts = new Dictionary<Tuple<Keys, Keys>, Action>();

            // key converters
            var keyConverter = new KeysConverter();
            var modifiersKeyConverter = new KeysConverter();

            // open shortcut
            _shortcuts.Add(
                new Tuple<Keys, Keys>((Keys)keyConverter.ConvertFromString("O"),
                                      (Keys)modifiersKeyConverter.ConvertFromString("Control")),
                () => Open());

            // save shortcut
            _shortcuts.Add(
                new Tuple<Keys, Keys>((Keys)keyConverter.ConvertFromString("S"),
                                      (Keys)modifiersKeyConverter.ConvertFromString("Control")),
                () => Save());

            // export shortcut
            _shortcuts.Add(
                new Tuple<Keys, Keys>((Keys)keyConverter.ConvertFromString("E"),
                                      (Keys)modifiersKeyConverter.ConvertFromString("Control")),
                () => Export());

            // undo shortcut
            _shortcuts.Add(
                new Tuple<Keys, Keys>((Keys)keyConverter.ConvertFromString("Z"),
                                      (Keys)modifiersKeyConverter.ConvertFromString("Control")),
                () => _mainView.Undo());

            // redo shortcut
            _shortcuts.Add(
                new Tuple<Keys, Keys>((Keys)keyConverter.ConvertFromString("Y"),
                                      (Keys)modifiersKeyConverter.ConvertFromString("Control")),
                () => _mainView.Redo());

            // snap shortcut
            _shortcuts.Add(
                new Tuple<Keys, Keys>((Keys)keyConverter.ConvertFromString("S"),
                                      Keys.None),
                () => _mainView.ToggleSnap());

            // clear shortcut
            _shortcuts.Add(
                new Tuple<Keys, Keys>((Keys)keyConverter.ConvertFromString("Delete"),
                                      (Keys)modifiersKeyConverter.ConvertFromString("Control")),
                () => _mainView.Clear());

            // editor shortcuts
            foreach (var editor in _mainView.Editors)
            {
                var _editor = editor;
                _shortcuts.Add(
                    new Tuple<Keys, Keys>((Keys)keyConverter.ConvertFromString(editor.Key),
                                          editor.Modifiers == "" ? Keys.None : (Keys)modifiersKeyConverter.ConvertFromString(editor.Modifiers)),
                    () => _mainView.EnableEditor(_editor));
            }
        }

        private void Initialize()
        {
            // create grid canvas
            //CreateGrid();

            // handle keyboard input
            KeyDown += (sender, e) =>
            {
                Action action;
                bool result = _shortcuts.TryGetValue(new Tuple<Keys, Keys>(e.KeyCode, e.Modifiers), out action);
                if (result == true && action != null)
                {
                    action();
                }
            };

            // open file dialog
            this.openFileDialog1.FileOk += (sender, e) =>
            {
                string path = openFileDialog1.FileName;
                int filterIndex = openFileDialog1.FilterIndex;
                _mainView.Open(path, filterIndex - 1);
                _mainView.Render();
            };

            // save file dialog
            this.saveFileDialog1.FileOk += (sender, e) =>
            {
                string path = saveFileDialog1.FileName;
                int filterIndex = saveFileDialog1.FilterIndex;
                _mainView.Save(path, filterIndex - 1);
            };

            // export file dialog
            this.saveFileDialog2.FileOk += (sender, e) =>
            {
                string path = saveFileDialog2.FileName;
                int filterIndex = saveFileDialog2.FilterIndex;
                _mainView.Export(path, filterIndex - 1);
            };

            // draw canvas panel
            this.canvasPanel1.Invalidate();
        }

        private string FilesFilter()
        {
            bool first = true;
            string filter = string.Empty;
            foreach (var serializer in _mainView.Files)
            {
                filter += string.Format("{0}{1} File (*.{2})|*.{2}", first == false ? "|" : string.Empty, serializer.Name, serializer.Extension);
                if (first == true)
                {
                    first = false;
                }
            }
            return filter;
        }

        private string CreatorsFilter()
        {
            bool first = true;
            string filter = string.Empty;
            foreach (var creator in _mainView.Creators)
            {
                filter += string.Format("{0}{1} File (*.{2})|*.{2}", first == false ? "|" : string.Empty, creator.Name, creator.Extension);
                if (first == true)
                {
                    first = false;
                }
            }
            return filter;
        }

        private void Open()
        {
            string filter = FilesFilter();
            int defaultFilterIndex = _mainView.Files.IndexOf(_mainView.Files.Where(c => c.Name == "Json").FirstOrDefault()) + 1;

            openFileDialog1.Filter = filter;
            openFileDialog1.FilterIndex = defaultFilterIndex;
            openFileDialog1.ShowDialog(this);
        }

        private void Save()
        {
            string filter = FilesFilter();
            int defaultFilterIndex = _mainView.Files.IndexOf(_mainView.Files.Where(c => c.Name == "Json").FirstOrDefault()) + 1;
            
            saveFileDialog1.Filter = filter;
            saveFileDialog1.FilterIndex = defaultFilterIndex;
            saveFileDialog1.FileName = "canvas";
            saveFileDialog1.ShowDialog(this);
        }

        private void Export()
        {
            string filter = CreatorsFilter();
            int defaultFilterIndex = _mainView.Creators.IndexOf(_mainView.Creators.Where(c => c.Name == "Pdf").FirstOrDefault()) + 1;

            saveFileDialog2.Filter = filter;
            saveFileDialog2.FilterIndex = defaultFilterIndex;
            saveFileDialog2.FileName = "canvas";
            saveFileDialog2.ShowDialog(this);
        }
    }

    public class MainView
    {
        private ILifetimeScope _backgroundScope;
        private ILifetimeScope _drawingScope;

        public ICollection<IEditor> Editors { get; set; }
        public IList<IFile<ICanvas, Stream>> Files { get; set; }
        public IList<ICreator<ICanvas>> Creators { get; set; }
        public ICanvas BackgroundCanvas { get; set; }
        public ICanvas DrawingCanvas { get; set; }

        public void Build(WinFormsCanvasPanel panel)
        {
            var bootstrapper = new Bootstrapper();
            var container = bootstrapper.Build(panel);

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
            var drawingCanvas = _drawingScope.Resolve<ICanvas>();
            drawingCanvas.Render(null);
        }
    }
}
