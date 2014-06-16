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

        private INative CreateGridLine(ICanvasConverter nf, ICanvasFactory pf, IColor stroke,  double thickness,  double x1, double y1,  double x2, double y2)
        {
            var xline = pf.CreateLine();
            xline.X1 = x1;
            xline.Y1 = y1;
            xline.X2 = x2;
            xline.Y2 = y2;
            xline.Stroke = stroke;
            xline.StrokeThickness = thickness;
            return nf.Convert(xline);
        }

        private void CreateGrid(ICanvas canvas, double width, double height, double size, double originX, double originY)
        {
            var nf = _backgroundScope.Resolve<ICanvasConverter>();
            var pf = _backgroundScope.Resolve<ICanvasFactory>();

            double thickness = 2.0;

            var stroke = pf.CreateColor();
            stroke.A = 0xFF;
            stroke.R = 0xE8;
            stroke.G = 0xE8;
            stroke.B = 0xE8;

            for (double y = size; y < height; y += size)
            {
                canvas.Add(CreateGridLine(nf, pf, stroke, thickness, originX, y, width, y));
            }

            for (double x = size; x < width; x += size)
            {
                canvas.Add(CreateGridLine(nf, pf, stroke, thickness, x, originY, x, height));
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

            builder.Register<ICanvasConverter>(f => new ModelToWpfConverter()).InstancePerLifetimeScope();
            builder.Register<ICanvasFactory>(f => new PortableXDefaultsFactory()).InstancePerLifetimeScope();

            builder.Register<ICanvas>(c =>
            {
                var portableFactory = c.Resolve<ICanvasFactory>();
                var xcanvas = portableFactory.CreateCanvas();
                var nativeFactory = c.Resolve<ICanvasConverter>();
                return nativeFactory.Convert(xcanvas);
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
            CreateGrid(_backgroundCanvas, 600.0, 600.0, 30.0, 0.0, 0.0);

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

        private void SaveJson()
        {
            var nativeCanvas = _drawingScope.Resolve<ICanvas>();

            var modelFactory = new CoreToModelConverter();
            var canvas = modelFactory.Convert(nativeCanvas);

            var dlg = new SaveFileDialog()
            {
                Filter = "Json File (*.json)|*.json",
                FileName = "canvas"
            };

            if (dlg.ShowDialog() == true)
            {
                var path = dlg.FileName;
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
        }

        private void Clear()
        {
            var canvas = _drawingScope.Resolve<ICanvas>();
            canvas.Clear();
        }
    }
}
