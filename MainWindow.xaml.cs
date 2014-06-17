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
using Newtonsoft.Json;
using System.Runtime.Serialization.Formatters;
using System.Runtime.Serialization;
using RxCanvas.Core;
using RxCanvas.Model;
using RxCanvas.Editors;
using RxCanvas.Xaml;
using RxCanvas.Json;
using RxCanvas.Pdf;

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
            RegisterAndBuild();
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

            builder.Register<ISerializer<ICanvas>>(f => new JsonXModelSerializer()).SingleInstance();
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
                var canvasSerializer = _drawingScope.Resolve<ISerializer<ICanvas>>();
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
                var canvasSerializer = _drawingScope.Resolve<ISerializer<ICanvas>>();
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

        private void Export()
        {
            var dlg = new SaveFileDialog()
            {
                Filter = "Pdf File (*.pdf)|*.pdf",
                FileName = "canvas"
            };

            if (dlg.ShowDialog() == true)
            {
                var canvasSerializer = _drawingScope.Resolve<ISerializer<ICanvas>>();
                var canvas = ConvertToModel();
                var writer = new PdfCreator();
                writer.Save(dlg.FileName, canvas);
            }
        }

        private void Clear()
        {
            var drawingCanvas = _drawingScope.Resolve<ICanvas>();
            drawingCanvas.Clear();
        }
    }

    public class PanAndZoomBorder : Border
    {
        private bool initialize = true;
        private UIElement _child = null;
        private Point _origin;
        private Point _start;

        private TranslateTransform GetTranslateTransform(UIElement element)
        {
            return (TranslateTransform)((TransformGroup)element.RenderTransform).Children.First(tr => tr is TranslateTransform);
        }

        private ScaleTransform GetScaleTransform(UIElement element)
        {
            return (ScaleTransform)((TransformGroup)element.RenderTransform).Children.First(tr => tr is ScaleTransform);
        }

        public override UIElement Child
        {
            get { return base.Child; }
            set
            {
                if (value != null && value != this.Child)
                {
                    _child = value;
                    if (initialize)
                    {
                        var group = new TransformGroup();
                        var st = new ScaleTransform();
                        group.Children.Add(st);
                        var tt = new TranslateTransform();
                        group.Children.Add(tt);
                        _child.RenderTransform = group;
                        _child.RenderTransformOrigin = new Point(0.0, 0.0);
                        this.MouseWheel += Border_MouseWheel;
                        this.MouseRightButtonDown += Border_MouseRightButtonDown;
                        this.MouseRightButtonUp += Border_MouseRightButtonUp;
                        this.MouseMove += Border_MouseMove;
                        this.PreviewMouseDown += Border_PreviewMouseDown;
                        initialize = false;
                    }
                }
                base.Child = value;
            }
        }

        public void Reset()
        {
            if (initialize == false && _child != null)
            {
                var st = GetScaleTransform(_child);
                st.ScaleX = 1.0;
                st.ScaleY = 1.0;
                var tt = GetTranslateTransform(_child);
                tt.X = 0.0;
                tt.Y = 0.0;
            }
        }

        private void Border_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (initialize == false && _child != null)
            {
                var st = GetScaleTransform(_child);
                var tt = GetTranslateTransform(_child);
                double zoom = e.Delta > 0 ? .2 : -.2;
                if (!(e.Delta > 0) && (st.ScaleX < .4 || st.ScaleY < .4))
                    return;
                Point relative = e.GetPosition(_child);
                double abosuluteX = relative.X * st.ScaleX + tt.X;
                double abosuluteY = relative.Y * st.ScaleY + tt.Y;
                st.ScaleX += zoom;
                st.ScaleY += zoom;
                tt.X = abosuluteX - relative.X * st.ScaleX;
                tt.Y = abosuluteY - relative.Y * st.ScaleY;
            }
        }

        private void Border_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (initialize == false && _child != null)
            {
                var tt = GetTranslateTransform(_child);
                _start = e.GetPosition(this);
                _origin = new Point(tt.X, tt.Y);
                this.Cursor = Cursors.Hand;
                _child.CaptureMouse();
            }
        }

        private void Border_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (initialize == false && _child != null)
            {
                _child.ReleaseMouseCapture();
                this.Cursor = Cursors.Arrow;
            }
        }

        private void Border_MouseMove(object sender, MouseEventArgs e)
        {
            if (initialize == false && _child != null && _child.IsMouseCaptured)
            {
                var tt = GetTranslateTransform(_child);
                Vector v = _start - e.GetPosition(this);
                tt.X = _origin.X - v.X;
                tt.Y = _origin.Y - v.Y;
            }
        }

        private void Border_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Middle && e.ClickCount == 2 && initialize == false && _child != null)
            {
                this.Reset();
            }
        }
    }
}

namespace RxCanvas.Json
{
    public class XModelSerializationBinder : SerializationBinder
    {
        public override void BindToName(Type serializedType, out string assemblyName, out string typeName)
        {
            assemblyName = null;
            typeName = serializedType.Name.Substring(1, serializedType.Name.Length - 1);
        }

        public override Type BindToType(string assemblyName, string typeName)
        {
            string resolvedTypeName = string.Format("RxCanvas.Model.{0}, RxCanvas.Model", 'X' + typeName);
            return Type.GetType(resolvedTypeName, true);
        }
    }

    public class JsonXModelSerializer : ISerializer<ICanvas>
    {
        public void Serialize(string path, ICanvas canvas)
        {
            var json = JsonSerialize(canvas);
            Save(path, json);
        }

        public ICanvas Deserialize(string path)
        {
            string json = Open(path);
            var canvas = JsonDeserialize(json);
            return canvas;
        }

        private static string JsonSerialize(ICanvas canvas)
        {
            var binder = new XModelSerializationBinder();
            var json = JsonConvert.SerializeObject(canvas,
                Formatting.Indented,
                new JsonSerializerSettings()
                {
                    ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                    TypeNameHandling = TypeNameHandling.Auto,
                    Binder = binder,
                    NullValueHandling = NullValueHandling.Ignore
                });
            return json;
        }

        private static ICanvas JsonDeserialize(string json)
        {
            var binder = new XModelSerializationBinder();
            var canvas = JsonConvert.DeserializeObject<RxCanvas.Model.XCanvas>(json,
                new JsonSerializerSettings()
                {
                    ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                    TypeNameHandling = TypeNameHandling.Auto,
                    Binder = binder,
                    NullValueHandling = NullValueHandling.Ignore
                });
            return canvas;
        }

        private static string Open(string path)
        {
            string json;
            using (var fs = System.IO.File.Open(path, System.IO.FileMode.Open))
            {
                using (var reader = new System.IO.StreamReader(fs))
                {
                    json = reader.ReadToEnd();
                }
            }
            return json;
        }

        private static void Save(string path, string json)
        {
            using (var fs = System.IO.File.Create(path))
            {
                using (var writer = new System.IO.StreamWriter(fs))
                {
                    writer.Write(json);
                }
            }
        }
    }
}

namespace RxCanvas.Pdf
{
    using RxCanvas.Core;
    using PdfSharp;
    using PdfSharp.Drawing;
    using PdfSharp.Pdf;

    public class PdfCreator
    {
        private Func<double, double> X;
        private Func<double, double> Y;

        public void Save(string path, ICanvas canvas)
        {
            using (var document = new PdfDocument())
            {
                AddPage(document, canvas);
                document.Save(path);
            }
        }

        public void Save(string path, IEnumerable<ICanvas> canvases)
        {
            using (var document = new PdfDocument())
            {
                foreach (var canvas in canvases)
                {
                    AddPage(document, canvas);
                }
                document.Save(path);
            }
        }

        private void AddPage(PdfDocument document, ICanvas canvas)
        {
            PdfPage page = document.AddPage();
            page.Size = PageSize.A4;
            page.Orientation = PageOrientation.Landscape;
            using (XGraphics gfx = XGraphics.FromPdfPage(page))
            {
                double scaleX = page.Width.Value / canvas.Width;
                double scaleY = page.Height.Value / canvas.Height;
                double scale = Math.Min(scaleX, scaleY);
                X = (x) => x * scale;
                Y = (y) => y * scale;
                DrawCanvas(gfx, canvas);
            }
        }

        private void DrawLine(XGraphics gfx, ILine line)
        {
            var pen = new XPen(XColor.FromArgb(line.Stroke.A, line.Stroke.R, line.Stroke.G, line.Stroke.B), X(line.StrokeThickness));
            gfx.DrawLine(pen, X(line.X1), Y(line.Y1), X(line.X2), Y(line.Y2));
        }

        private void DrawBezier(XGraphics gfx, IBezier bezier)
        {
            var pen = new XPen(XColor.FromArgb(bezier.Stroke.A, bezier.Stroke.R, bezier.Stroke.G, bezier.Stroke.B), X(bezier.StrokeThickness));
            gfx.DrawBezier(pen, 
                X(bezier.Start.X), Y(bezier.Start.Y),
                X(bezier.Point1.X), Y(bezier.Point1.Y),
                X(bezier.Point2.X), Y(bezier.Point2.Y),
                X(bezier.Point3.X), Y(bezier.Point3.Y));
        }

        private void DrawQuadraticBezier(XGraphics gfx, IQuadraticBezier quadraticBezier)
        {
            double x1 = quadraticBezier.Start.X;
            double y1 = quadraticBezier.Start.Y;
            double x2 = quadraticBezier.Start.X + (2.0 * (quadraticBezier.Point1.X - quadraticBezier.Start.X)) / 3.0;
            double y2 = quadraticBezier.Start.Y + (2.0 * (quadraticBezier.Point1.Y - quadraticBezier.Start.Y)) / 3.0;
            double x3 = x2 + (quadraticBezier.Point2.X - quadraticBezier.Start.X) / 3.0;
            double y3 = y2 + (quadraticBezier.Point2.Y - quadraticBezier.Start.Y) / 3.0;
            double x4 = quadraticBezier.Point2.X;
            double y4 = quadraticBezier.Point2.Y;
            var pen = new XPen(XColor.FromArgb(quadraticBezier.Stroke.A, quadraticBezier.Stroke.R, quadraticBezier.Stroke.G, quadraticBezier.Stroke.B), X(quadraticBezier.StrokeThickness));
            gfx.DrawBezier(pen,
                X(x1), Y(y1),
                X(x2), Y(y2),
                X(x3), Y(y3),
                X(x4), Y(y4));
        }

        private void DrawArc(XGraphics gfx, IArc arc)
        {
            var pen = new XPen(XColor.FromArgb(arc.Stroke.A, arc.Stroke.R, arc.Stroke.G, arc.Stroke.B), X(arc.StrokeThickness));
            gfx.DrawArc(pen, X(arc.X), Y(arc.Y), X(arc.Width), Y(arc.Height), arc.StartAngle, arc.SweepAngle);
        }

        private void DrawRectangle(XGraphics gfx, IRectangle rectangle)
        {
            double st = rectangle.StrokeThickness;
            double hst = st / 2.0;
            if (rectangle.IsFilled)
            {
                var pen = new XPen(XColor.FromArgb(rectangle.Stroke.A, rectangle.Stroke.R, rectangle.Stroke.G, rectangle.Stroke.B), X(rectangle.StrokeThickness));
                var brush = new XSolidBrush(XColor.FromArgb(rectangle.Fill.A, rectangle.Fill.R, rectangle.Fill.G, rectangle.Fill.B));
                gfx.DrawRectangle(pen, brush, X(rectangle.X + hst), Y(rectangle.Y + hst), X(rectangle.Width - st), Y(rectangle.Height - st));
            }
            else
            {
                var pen = new XPen(XColor.FromArgb(rectangle.Stroke.A, rectangle.Stroke.R, rectangle.Stroke.G, rectangle.Stroke.B), X(rectangle.StrokeThickness));
                gfx.DrawRectangle(pen, X(rectangle.X + hst), Y(rectangle.Y + hst), X(rectangle.Width - st), Y(rectangle.Height - st));
            }
        }

        private void DrawEllipse(XGraphics gfx, IEllipse ellipse)
        {
            double st = ellipse.StrokeThickness;
            double hst = st / 2.0;
            if (ellipse.IsFilled)
            {
                var pen = new XPen(XColor.FromArgb(ellipse.Stroke.A, ellipse.Stroke.R, ellipse.Stroke.G, ellipse.Stroke.B),  X(ellipse.StrokeThickness));
                var brush = new XSolidBrush(XColor.FromArgb(ellipse.Fill.A, ellipse.Fill.R, ellipse.Fill.G, ellipse.Fill.B));
                gfx.DrawEllipse(pen, brush, X(ellipse.X + hst), Y(ellipse.Y + hst), X(ellipse.Width - st), Y(ellipse.Height - st));
            }
            else
            {
                var pen = new XPen(XColor.FromArgb(ellipse.Stroke.A, ellipse.Stroke.R, ellipse.Stroke.G, ellipse.Stroke.B), X(ellipse.StrokeThickness));
                gfx.DrawEllipse(pen, X(ellipse.X + hst), Y(ellipse.Y + hst), X(ellipse.Width - st), Y(ellipse.Height - st));
            }
        }

        private void DrawCanvas(XGraphics gfx, ICanvas canvas)
        {
            foreach (var child in canvas.Children)
            {
                if (child is ILine)
                {
                    DrawLine(gfx, child as ILine);
                }
                else if (child is IBezier)
                {
                    DrawBezier(gfx, child as IBezier);
                }
                else if (child is IQuadraticBezier)
                {
                    DrawQuadraticBezier(gfx, child as IQuadraticBezier);
                }
                else if (child is IArc)
                {
                    DrawArc(gfx, child as IArc);
                }
                else if (child is IRectangle)
                {
                    DrawRectangle(gfx, child as IRectangle);
                }
                else if (child is IEllipse)
                {
                    DrawEllipse(gfx, child as IEllipse);
                }
                else
                {
                    throw new NotSupportedException();
                }
            }
        }
    }
}
