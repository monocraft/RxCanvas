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
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Reflection;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Joins;
using System.Reactive.Linq;
using System.Reactive.PlatformServices;
using System.Reactive.Subjects;
using System.Reactive.Threading;
using Autofac;
using Autofac.Builder;
using Autofac.Core;
using Autofac.Features;
using Autofac.Util;
using RxCanvas.Core;
using RxCanvas.Model;
using RxCanvas.Editors;
using RxCanvas.Xaml;
using RxCanvas.Serializers;
using RxCanvas.Creators;

namespace RxCanvas
{
    public partial class MainWindow : Window
    {
        private IContainer _container;
        private ILifetimeScope _backgroundScope;
        private ILifetimeScope _drawingScope;
        private ICollection<IEditor> _editors;
        private IList<ISerializer<ICanvas>> _serializers;
        private IList<ICreator<ICanvas>> _creators;
        private IDictionary<Tuple<Key, ModifierKeys>, Action> _shortcuts;
        private ICanvas _backgroundCanvas;
        private ICanvas _drawingCanvas;

        public MainWindow()
        {
            InitializeComponent();
            RegisterAndBuild();
        }

        private void RegisterAndBuild()
        {
            // register components
            var builder = new ContainerBuilder();

            var editorAssembly = Assembly.GetAssembly(typeof(PortableXDefaultsFactory));
            builder.RegisterAssemblyTypes(editorAssembly)
                .Where(t => t.Name.EndsWith("Editor"))
                .AsImplementedInterfaces()
                .InstancePerLifetimeScope();

            var serializerAssembly = Assembly.GetAssembly(typeof(JsonXModelSerializer));
            builder.RegisterAssemblyTypes(serializerAssembly)
                .Where(t => t.Name.EndsWith("Serializer"))
                .AsImplementedInterfaces()
                .InstancePerLifetimeScope();

            var creatorAssembly = Assembly.GetAssembly(typeof(CoreCanvasPdfCreator));
            builder.RegisterAssemblyTypes(creatorAssembly)
                .Where(t => t.Name.EndsWith("Creator"))
                .AsImplementedInterfaces()
                .InstancePerLifetimeScope();

            builder.Register<ICoreToModelConverter>(f => new CoreToXModelConverter()).SingleInstance();
            builder.Register<IModelToNativeConverter>(f => new XModelToWpfConverter()).SingleInstance();
            builder.Register<ICanvasFactory>(f => new PortableXDefaultsFactory()).SingleInstance();

            builder.Register<ICanvas>(c =>
            {
                var canvasFactory = c.Resolve<ICanvasFactory>();
                var xcanvas = canvasFactory.CreateCanvas();
                var nativeConverter = c.Resolve<IModelToNativeConverter>();
                return nativeConverter.Convert(xcanvas);
            }).InstancePerLifetimeScope();

            // create container ans scopes
            _container = builder.Build();
            _backgroundScope = _container.BeginLifetimeScope();
            _drawingScope = _container.BeginLifetimeScope();

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

            // add canvas to root layout
            Layout.Children.Add(_backgroundCanvas.Native as UIElement);
            Layout.Children.Add(_drawingCanvas.Native as UIElement);

            // create grid canvas
            CreateGrid();

            // handle keyboard input
            PreviewKeyDown += (sender, e) =>
            {
                //MessageBox.Show(Keyboard.Modifiers.ToString());
                Action action;
                bool result = _shortcuts.TryGetValue(new Tuple<Key, ModifierKeys>(e.Key, Keyboard.Modifiers), out action);
                if (result == true && action != null)
                {
                    action();
                }
            };

            // set data context
            DataContext = _drawingCanvas;
        }

        private void InitlializeShortucts()
        {
            // shortcuts dictionary
            _shortcuts = new Dictionary<Tuple<Key, ModifierKeys>, Action>();

            // key converters
            var keyConverter = new KeyConverter();
            var modifiersKeyConverter = new ModifierKeysConverter();

            // editor shortcuts
            foreach (var editor in _editors)
            {
                var _editor = editor;
                _shortcuts.Add(
                    new Tuple<Key, ModifierKeys>((Key)keyConverter.ConvertFromString(editor.Key),
                                                 (ModifierKeys)modifiersKeyConverter.ConvertFromString(editor.Modifiers)),
                    () => EnableEditor(_editor));
            }

            // snap shortcut
            _shortcuts.Add(
                new Tuple<Key, ModifierKeys>((Key)keyConverter.ConvertFromString("S"),
                                             (ModifierKeys)modifiersKeyConverter.ConvertFromString("")),
                () => ToggleSnap());

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

            // clear shortcut
            _shortcuts.Add(
                new Tuple<Key, ModifierKeys>((Key)keyConverter.ConvertFromString("Delete"),
                                             (ModifierKeys)modifiersKeyConverter.ConvertFromString("Control")),
                () => Clear());
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
            xline.X1 = x1;
            xline.Y1 = y1;
            xline.X2 = x2;
            xline.Y2 = y2;
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

        private void Open()
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

            var dlg = new OpenFileDialog()
            {
                Filter = filter,
            };

            if (dlg.ShowDialog() == true)
            {
                var serializer = _serializers[dlg.FilterIndex - 1];
                var xcanvas = serializer.Deserialize(dlg.FileName);
                ConvertToNative(xcanvas);
            }
        }

        private void Save()
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

            var dlg = new SaveFileDialog()
            {
                Filter = filter,
                FileName = "canvas"
            };

            if (dlg.ShowDialog() == true)
            {
                var canvas = ConvertToModel();
                var serializer = _serializers[dlg.FilterIndex - 1];
                serializer.Serialize(dlg.FileName, canvas);
            }
        }

        private void Export()
        {
            bool first = true;
            string filter = string.Empty;
            foreach(var creator in _creators)
            {
                filter += string.Format("{0}{1} File (*.{2})|*.{2}", first == false ? "|" : string.Empty, creator.Name, creator.Extension);
                if (first == true)
                {
                    first = false;
                }
            }

            var dlg = new SaveFileDialog()
            {
                Filter = filter,
                FileName = "canvas"
            };

            if (dlg.ShowDialog() == true)
            {
                var canvas = ConvertToModel();
                var creator = _creators[dlg.FilterIndex - 1];
                creator.Save(dlg.FileName, canvas);
            }
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

        private void Clear()
        {
            var drawingCanvas = _drawingScope.Resolve<ICanvas>();
            drawingCanvas.Clear();
        }
    }
}
