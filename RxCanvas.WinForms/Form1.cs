using Autofac;
using RxCanvas.Core;
using RxCanvas.Creators;
using RxCanvas.Editors;
using RxCanvas.Model;
using RxCanvas.Serializers;
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

namespace RxCanvas.WinForms
{
    public partial class Form1 : Form
    {
        private ILifetimeScope _backgroundScope;
        private ILifetimeScope _drawingScope;
        private ICollection<IEditor> _editors;
        private IList<ISerializer<ICanvas>> _serializers;
        private IList<ICreator<ICanvas>> _creators;
        private IDictionary<Tuple<Keys, Keys>, Action> _shortcuts;
        private ICanvas _backgroundCanvas;
        private ICanvas _drawingCanvas;

        public Form1()
        {
            InitializeComponent();
            this.SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.DoubleBuffer, true);
            RegisterAndBuild();
        }

        private void RegisterAndBuild()
        {
            var bootstrapper = new Bootstrapper();
            var container = bootstrapper.Build(this.canvasPanel1);

            // create scopes
            _backgroundScope = container.BeginLifetimeScope();
            _drawingScope = container.BeginLifetimeScope();

            // resolve dependencies
            _backgroundCanvas = _backgroundScope.Resolve<ICanvas>();
            _drawingCanvas = _drawingScope.Resolve<ICanvas>();

            _editors = _drawingScope.Resolve<ICollection<IEditor>>();
            _serializers = _drawingScope.Resolve<IList<ISerializer<ICanvas>>>();
            _creators = _drawingScope.Resolve<IList<ICreator<ICanvas>>>();

            // set default editor
            _editors.Where(e => e.Name == "Line").FirstOrDefault().IsEnabled = true;

            // initialize shortcuts
            InitlializeShortucts();

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
                Open(path, filterIndex - 1);
                Render();
            };

            // save file dialog
            this.saveFileDialog1.FileOk += (sender, e) =>
            {
                string path = saveFileDialog1.FileName;
                int filterIndex = saveFileDialog1.FilterIndex;
                Save(path, filterIndex - 1);
            };

            // export file dialog
            this.saveFileDialog2.FileOk += (sender, e) =>
            {
                string path = saveFileDialog2.FileName;
                int filterIndex = saveFileDialog2.FilterIndex;
                Export(path, filterIndex - 1);
            };

            // draw canvas panel
            this.canvasPanel1.Invalidate();
        }

        private void InitlializeShortucts()
        {
            // shortcuts dictionary
            _shortcuts = new Dictionary<Tuple<Keys, Keys>, Action>();

            // key converters
            var keyConverter = new KeysConverter();
            var modifiersKeyConverter = new KeysConverter();

            // editor shortcuts
            foreach (var editor in _editors)
            {
                var _editor = editor;
                _shortcuts.Add(
                    new Tuple<Keys, Keys>((Keys)keyConverter.ConvertFromString(editor.Key), 
                                          editor.Modifiers == "" ? Keys.None : (Keys)modifiersKeyConverter.ConvertFromString(editor.Modifiers)), 
                    () => EnableEditor(_editor));
            }

            // snap shortcut
            _shortcuts.Add(
                new Tuple<Keys, Keys>((Keys)keyConverter.ConvertFromString("S"),
                                      Keys.None),
                () => ToggleSnap());

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

            // clear shortcut
            _shortcuts.Add(
                new Tuple<Keys, Keys>((Keys)keyConverter.ConvertFromString("Delete"),
                    (Keys)modifiersKeyConverter.ConvertFromString("Control")),
                () => Clear());
        }

        private void Open()
        {
            string filter = CreateSerializersFilter();
            int defaultFilterIndex = _serializers.IndexOf(_serializers.Where(c => c.Name == "Json").FirstOrDefault()) + 1;

            openFileDialog1.Filter = filter;
            openFileDialog1.FilterIndex = defaultFilterIndex;
            openFileDialog1.ShowDialog();
        }

        private void Save()
        {
            string filter = CreateSerializersFilter();
            int defaultFilterIndex = _serializers.IndexOf(_serializers.Where(c => c.Name == "Json").FirstOrDefault()) + 1;
            
            saveFileDialog1.Filter = filter;
            saveFileDialog1.FilterIndex = defaultFilterIndex;
            saveFileDialog1.FileName = "canvas";
            saveFileDialog1.ShowDialog();
        }

        private void Export()
        {
            string filter = CreateCreatorsFilter();
            int defaultFilterIndex = _creators.IndexOf(_creators.Where(c => c.Name == "Pdf").FirstOrDefault()) + 1;

            saveFileDialog2.Filter = filter;
            saveFileDialog2.FilterIndex = defaultFilterIndex;
            saveFileDialog2.FileName = "canvas";
            saveFileDialog2.ShowDialog();
        }

        private string CreateSerializersFilter()
        {
            bool first = true;
            string filter = string.Empty;
            foreach (var serializer in _serializers)
            {
                filter += string.Format("{0}{1} File (*.{2})|*.{2}", first == false ? "|" : string.Empty, serializer.Name, serializer.Extension);
                if (first == true)
                {
                    first = false;
                }
            }
            return filter;
        }

        private string CreateCreatorsFilter()
        {
            bool first = true;
            string filter = string.Empty;
            foreach (var creator in _creators)
            {
                filter += string.Format("{0}{1} File (*.{2})|*.{2}", first == false ? "|" : string.Empty, creator.Name, creator.Extension);
                if (first == true)
                {
                    first = false;
                }
            }
            return filter;
        }

        private void Open(string path, int index)
        {
            var file = _drawingScope.Resolve<ITextFile>();
            var serializer = _serializers[index];
            var json = file.Open(path);
            var xcanvas = serializer.Deserialize(json);
            ConvertToNative(xcanvas);
        }

        private void Save(string path, int index)
        {
            var canvas = ConvertToModel();
            var file = _drawingScope.Resolve<ITextFile>();
            var serializer = _serializers[index];
            var json = serializer.Serialize(canvas);
            file.Save(path, json);
        }

        private void Export(string path, int index)
        {
            var canvas = ConvertToModel();
            var creator = _creators[index];
            creator.Save(path, canvas);
        }

        private void ConvertToNative(ICanvas xcanvas)
        {
            var drawingCanvas = _drawingScope.Resolve<ICanvas>();
            var nativeConverter = _drawingScope.Resolve<IModelToNativeConverter>();

            drawingCanvas.Clear();

            foreach (var child in xcanvas.Children)
            {
                if (child is ILine)
                {
                    var native = nativeConverter.Convert(child as ILine);
                    drawingCanvas.Add(native);
                }
                else if (child is IBezier)
                {
                    var native = nativeConverter.Convert(child as IBezier);
                    drawingCanvas.Add(native);
                }
                else if (child is IQuadraticBezier)
                {
                    var native = nativeConverter.Convert(child as IQuadraticBezier);
                    drawingCanvas.Add(native);
                }
                else if (child is IArc)
                {
                    var native = nativeConverter.Convert(child as IArc);
                    drawingCanvas.Add(native);
                }
                else if (child is IRectangle)
                {
                    var native = nativeConverter.Convert(child as IRectangle);
                    drawingCanvas.Add(native);
                }
                else if (child is IEllipse)
                {
                    var native = nativeConverter.Convert(child as IEllipse);
                    drawingCanvas.Add(native);
                }
                else if (child is IText)
                {
                    var native = nativeConverter.Convert(child as IText);
                    drawingCanvas.Add(native);
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
            var modelConverter = _drawingScope.Resolve<ICoreToModelConverter>();
            var canvas = modelConverter.Convert(drawingCanvas);
            return canvas;
        }

        private void CreateGrid()
        {
            var backgroundCanvas = _backgroundScope.Resolve<ICanvas>();
            var nativeConverter = _backgroundScope.Resolve<IModelToNativeConverter>();
            var canvasFactory = _backgroundScope.Resolve<ICanvasFactory>();
            CreateGrid(nativeConverter, canvasFactory, backgroundCanvas, 600.0, 600.0, 30.0, 0.0, 0.0);
        }

        private INative CreateGridLine(IModelToNativeConverter nativeConverter, ICanvasFactory canvasFactory, IColor stroke, double thickness, double x1, double y1, double x2, double y2)
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

        private void CreateGrid(IModelToNativeConverter nativeConverter, ICanvasFactory canvasFactory, ICanvas canvas, double width, double height, double size, double originX, double originY)
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

        private void EnableEditor(IEditor _editor)
        {
            foreach (var editor in _editors)
            {
                editor.IsEnabled = false;
            };
            _editor.IsEnabled = true;
        }

        private void ToggleSnap()
        {
            var canvas = _drawingScope.Resolve<ICanvas>();
            canvas.EnableSnap = canvas.EnableSnap ? false : true;
        }

        private void Clear()
        {
            var drawingCanvas = _drawingScope.Resolve<ICanvas>();
            drawingCanvas.Clear();
            this.canvasPanel1.Invalidate();
        }

        private void Render()
        {
            var drawingCanvas = _drawingScope.Resolve<ICanvas>();
            drawingCanvas.Render(null);
        }
    }
}
