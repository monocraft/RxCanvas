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
using System.Runtime.Serialization.Formatters;
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
using Newtonsoft.Json;
using RxCanvas.Core;
using RxCanvas.Model;
using RxCanvas.Editors;
using RxCanvas.Xaml;

namespace RxCanvas
{
    public partial class MainWindow : Window
    {
        private IContainer _container;
        private ILifetimeScope _backgroundScope;
        private ILifetimeScope _drawingScope;
        private ICollection<IEditor> _editors;
        private IDictionary<Tuple<Key, ModifierKeys>, Action> _shortcuts;
        private ICanvas _backgroundCanvas;
        private ICanvas _drawingCanvas;

        public MainWindow()
        {
            InitializeComponent();

            try
            {
                RegisterAndBuild();
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message + Environment.NewLine + ex.StackTrace);
            }
        }

        private void RegisterAndBuild()
        {
            // register components
            var builder = new ContainerBuilder();

            var assembly = Assembly.GetAssembly(typeof(PortableXDefaultsFactory));
            builder.RegisterAssemblyTypes(assembly)
                .Where(t => t.Name.EndsWith("Editor"))
                .AsImplementedInterfaces()
                .InstancePerLifetimeScope();

            builder.Register<ICanvasSerializer>(f => new JsonXModelSerializer()).SingleInstance();
            builder.Register<ICoreToModelConverter>(f => new CoreToXModelConverter()).SingleInstance();
            builder.Register<IModelToNativeConverter>(f => new XModelToWpfConverter()).SingleInstance();
            builder.Register<ICanvasFactory>(f => new PortableXDefaultsFactory()).SingleInstance();

            builder.Register<ICanvas>(c =>
            {
                var portableFactory = c.Resolve<ICanvasFactory>();
                var xcanvas = portableFactory.CreateCanvas();
                var nativeConverter = c.Resolve<IModelToNativeConverter>();
                return nativeConverter.Convert(xcanvas);
            }).InstancePerLifetimeScope();

            // resolve dependencies
            _container = builder.Build();
            _backgroundScope = _container.BeginLifetimeScope();
            _drawingScope = _container.BeginLifetimeScope();

            _backgroundCanvas = _backgroundScope.Resolve<ICanvas>();
            _drawingCanvas = _drawingScope.Resolve<ICanvas>();
            _editors = _drawingScope.Resolve<ICollection<IEditor>>();

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
                () => ToggleSpan());

            // open shortcut
            _shortcuts.Add(
                new Tuple<Key, ModifierKeys>((Key)keyConverter.ConvertFromString("O"),
                                             (ModifierKeys)modifiersKeyConverter.ConvertFromString("Control")),
                () => OpenJson());

            // save shortcut
            _shortcuts.Add(
                new Tuple<Key, ModifierKeys>((Key)keyConverter.ConvertFromString("S"),
                                             (ModifierKeys)modifiersKeyConverter.ConvertFromString("Control")),
                () => SaveJson());

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

        private void ToggleSpan()
        {
            var canvas = _drawingScope.Resolve<ICanvas>();
            canvas.EnableSnap = canvas.EnableSnap ? false : true;
        }

        private void OpenJson()
        {
            var dlg = new OpenFileDialog()
            {
                Filter = "Json File (*.json)|*.json",
            };

            if (dlg.ShowDialog() == true)
            {
                var canvasSerializer = _drawingScope.Resolve<ICanvasSerializer>();
                var xcanvas = canvasSerializer.Deserialize(dlg.FileName);
                ConvertToNative(xcanvas);
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
                else
                {
                    throw new NotSupportedException();
                }
            }
        }

        private void SaveJson()
        {
            var dlg = new SaveFileDialog()
            {
                Filter = "Json File (*.json)|*.json",
                FileName = "canvas"
            };

            if (dlg.ShowDialog() == true)
            {
                var canvasSerializer = _drawingScope.Resolve<ICanvasSerializer>();
                var canvas = ConvertToModel();
                canvasSerializer.Serialize(dlg.FileName, canvas);
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

    public class JsonXModelSerializer : ICanvasSerializer
    {
        public void Serialize(string path, ICanvas canvas)
        {
            var json = JsonConvert.SerializeObject(canvas,
                Formatting.Indented,
                new JsonSerializerSettings()
                {
                    ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                    TypeNameHandling = TypeNameHandling.Auto,
                    TypeNameAssemblyFormat = FormatterAssemblyStyle.Simple
                });

            using (var fs = System.IO.File.Create(path))
            {
                using (var writer = new System.IO.StreamWriter(fs))
                {
                    writer.Write(json);
                }
            }
        }

        public ICanvas Deserialize(string path)
        {
            string json;

            using (var fs = System.IO.File.Open(path, System.IO.FileMode.Open))
            {
                using (var reader = new System.IO.StreamReader(fs))
                {
                    json = reader.ReadToEnd();
                }
            }

            var canvas = JsonConvert.DeserializeObject<XCanvas>(json,
                new JsonSerializerSettings()
                {
                    ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                    TypeNameHandling = TypeNameHandling.Auto,
                    TypeNameAssemblyFormat = FormatterAssemblyStyle.Simple
                });

            return canvas;
        }
    }
}
