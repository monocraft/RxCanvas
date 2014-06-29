using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Win32;
using System.Diagnostics;
using Autofac;
using RxCanvas.Interfaces;
using RxCanvas.Binary;
using RxCanvas.Model;
using System.IO;

namespace RxCanvas
{
    public partial class MainWindow : Window
    {
        private MainView _mainView;
        private IDictionary<Tuple<Key, ModifierKeys>, Action> _shortcuts;

        public MainWindow()
        {
            InitializeComponent();

            _mainView = new MainView();
            _mainView.Build();

            InitlializeShortucts();
            Initialize();
        }

        private void InitlializeShortucts()
        {
            // shortcuts dictionary
            _shortcuts = new Dictionary<Tuple<Key, ModifierKeys>, Action>();

            // key converters
            var keyConverter = new KeyConverter();
            var modifiersKeyConverter = new ModifierKeysConverter();

            // open shortcut
            _shortcuts.Add(
                new Tuple<Key, ModifierKeys>((Key)keyConverter.ConvertFromString("O"),
                                             (ModifierKeys)modifiersKeyConverter.ConvertFromString("Control")),
                () => Open());

            // save shortcut
            _shortcuts.Add(
                new Tuple<Key, ModifierKeys>((Key)keyConverter.ConvertFromString("S"),
                                             (ModifierKeys)modifiersKeyConverter.ConvertFromString("Control")),
                () => Save());

            // export shortcut
            _shortcuts.Add(
                new Tuple<Key, ModifierKeys>((Key)keyConverter.ConvertFromString("E"),
                                             (ModifierKeys)modifiersKeyConverter.ConvertFromString("Control")),
                () => Export());

            // undo shortcut
            _shortcuts.Add(
                new Tuple<Key, ModifierKeys>((Key)keyConverter.ConvertFromString("Z"),
                                             (ModifierKeys)modifiersKeyConverter.ConvertFromString("Control")),
                () => _mainView.Undo());

            // redo shortcut
            _shortcuts.Add(
                new Tuple<Key, ModifierKeys>((Key)keyConverter.ConvertFromString("Y"),
                                             (ModifierKeys)modifiersKeyConverter.ConvertFromString("Control")),
                () => _mainView.Redo());

            // snap shortcut
            _shortcuts.Add(
                new Tuple<Key, ModifierKeys>((Key)keyConverter.ConvertFromString("S"),
                                             ModifierKeys.None),
                () => _mainView.ToggleSnap());

            // clear shortcut
            _shortcuts.Add(
                new Tuple<Key, ModifierKeys>((Key)keyConverter.ConvertFromString("Delete"),
                                             (ModifierKeys)modifiersKeyConverter.ConvertFromString("Control")),
                () => _mainView.Clear());

            // editor shortcuts
            foreach (var editor in _mainView.Editors)
            {
                var _editor = editor;
                _shortcuts.Add(
                    new Tuple<Key, ModifierKeys>((Key)keyConverter.ConvertFromString(editor.Key),
                                                 editor.Modifiers == "" ? ModifierKeys.None : (ModifierKeys)modifiersKeyConverter.ConvertFromString(editor.Modifiers)),
                    () => _mainView.EnableEditor(_editor));
            }
        }

        private void Initialize()
        {
            // add canvas to root layout
            Layout.Children.Add(_mainView.BackgroundCanvas.Native as UIElement);
            Layout.Children.Add(_mainView.DrawingCanvas.Native as UIElement);

            // create grid canvas
            _mainView.CreateGrid();

            // handle keyboard input
            PreviewKeyDown += (sender, e) =>
            {
                Action action;
                bool result = _shortcuts.TryGetValue(new Tuple<Key, ModifierKeys>(e.Key, Keyboard.Modifiers), out action);
                if (result == true && action != null)
                {
                    action();
                }
            };

            // set data context
            DataContext = _mainView.DrawingCanvas;
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
            var dlg = new OpenFileDialog()
            {
                Filter = filter,
                FilterIndex = defaultFilterIndex
            };

            if (dlg.ShowDialog(this) == true)
            {
                string path = dlg.FileName;
                int filterIndex = dlg.FilterIndex;
                _mainView.Open(path, filterIndex - 1);
            }
        }

        private void Save()
        {
            string filter = FilesFilter();
            int defaultFilterIndex = _mainView.Files.IndexOf(_mainView.Files.Where(c => c.Name == "Json").FirstOrDefault()) + 1;
            var dlg = new SaveFileDialog()
            {
                Filter = filter,
                FilterIndex = defaultFilterIndex,
                FileName = "canvas"
            };

            if (dlg.ShowDialog(this) == true)
            {
                string path = dlg.FileName;
                int filterIndex = dlg.FilterIndex;
                _mainView.Save(path, filterIndex - 1);
            }
        }

        private void Export()
        {
            string filter = CreatorsFilter();
            int defaultFilterIndex = _mainView.Creators.IndexOf(_mainView.Creators.Where(c => c.Name == "Pdf").FirstOrDefault()) + 1;
            var dlg = new SaveFileDialog()
            {
                Filter = filter,
                FilterIndex = defaultFilterIndex,
                FileName = "canvas"
            };

            if (dlg.ShowDialog() == true)
            {
                string path = dlg.FileName;
                int filterIndex = dlg.FilterIndex;
                _mainView.Export(path, filterIndex - 1);
            }
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

        public void Build()
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

            // block demo
            /*
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

            _files[index].Save(path, xcanvas);
            */
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
        }

        public void Undo()
        {
            var drawingCanvas = _drawingScope.Resolve<ICanvas>();
            var xcanvas = drawingCanvas.History.Undo(drawingCanvas);
            if (xcanvas != null)
            {
                Open(xcanvas);
            }
        }

        public void Redo()
        {
            var drawingCanvas = _drawingScope.Resolve<ICanvas>();
            var xcanvas = drawingCanvas.History.Redo(drawingCanvas);
            if (xcanvas != null)
            {
                Open(xcanvas);
            }
        }
    }
}
