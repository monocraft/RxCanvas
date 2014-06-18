using Autofac;
using RxCanvas.Core;
using RxCanvas.Creators;
using RxCanvas.Editors;
using RxCanvas.Model;
using RxCanvas.Serializers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
//using System.ComponentModel;
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
        private IContainer _container;
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
                .SingleInstance();

            var creatorAssembly = Assembly.GetAssembly(typeof(CoreCanvasPdfCreator));
            builder.RegisterAssemblyTypes(creatorAssembly)
                .Where(t => t.Name.EndsWith("Creator"))
                .AsImplementedInterfaces()
                .SingleInstance();

            builder.Register<ICoreToModelConverter>(f => new CoreToXModelConverter()).SingleInstance();
            builder.Register<IModelToNativeConverter>(f => new XModelToWindowsFormsConverter(this.canvasPanel1)).SingleInstance();
            builder.Register<ICanvasFactory>(f => new PortableXDefaultsFactory()).SingleInstance();

            builder.Register<ICanvas>(c =>
            {
                var canvasFactory = c.Resolve<ICanvasFactory>();
                var xcanvas = canvasFactory.CreateCanvas();
                var nativeConverter = c.Resolve<IModelToNativeConverter>();
                return nativeConverter.Convert(xcanvas);
            }).InstancePerLifetimeScope();

            builder.Register<ITextFile>(f => new Utf8TextFile()).SingleInstance();

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

            /*
            // add canvas to root layout
            Layout.Children.Add(_backgroundCanvas.Native as UIElement);
            Layout.Children.Add(_drawingCanvas.Native as UIElement);

            // create grid canvas
            CreateGrid();
            */

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

            // open file dialog events
            this.openFileDialog1.FileOk += (sender, e) =>
            {
                string path = openFileDialog1.FileName;
                int fileterIndex = openFileDialog1.FilterIndex;
                Open(path, fileterIndex);
                var drawingCanvas = _drawingScope.Resolve<ICanvas>();
                drawingCanvas.Render(null);
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
                Keys key = (Keys)keyConverter.ConvertFromString(editor.Key);
                Keys modifiers = editor.Modifiers == "" ? Keys.None : (Keys)modifiersKeyConverter.ConvertFromString(editor.Modifiers);
                Action action = () => EnableEditor(_editor);
                _shortcuts.Add(new Tuple<Keys, Keys>(key, modifiers), action);
            }

            /*
            // snap shortcut
            _shortcuts.Add(
                new Tuple<Keys, Keys>((Keys)keyConverter.ConvertFromString("S"),
                                      (Keys)modifiersKeyConverter.ConvertFromString("")),
                () => ToggleSnap());
            */

            // open shortcut
            _shortcuts.Add(
                new Tuple<Keys, Keys>((Keys)keyConverter.ConvertFromString("O"),
                                      (Keys)modifiersKeyConverter.ConvertFromString("Control")),
                () => Open());

            /*
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
            */

            // clear shortcut
            _shortcuts.Add(
                new Tuple<Keys, Keys>((Keys)keyConverter.ConvertFromString("Delete"),
                    (Keys)modifiersKeyConverter.ConvertFromString("Control")),
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

            int defaultFilterIndex = _serializers.IndexOf(_serializers.Where(c => c.Name == "Json").FirstOrDefault()) + 1;

            openFileDialog1.Filter = filter;
            openFileDialog1.FilterIndex = defaultFilterIndex;
            openFileDialog1.ShowDialog();
        }

        private void Open(string path, int filterIndex)
        {
            var file = _drawingScope.Resolve<ITextFile>();
            var serializer = _serializers[filterIndex - 1];
            var json = file.Open(path);
            var xcanvas = serializer.Deserialize(json);
            ConvertToNative(xcanvas);
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

        private void Clear()
        {
            var drawingCanvas = _drawingScope.Resolve<ICanvas>();
            drawingCanvas.Clear();
            this.canvasPanel1.Invalidate();
        }
    }

    public class CanvasPanel : Panel
    {
        public ICanvas Canvas { get; set; }

        public CanvasPanel()
        {
            this.SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Draw(e.Graphics, Canvas);
        }

        private void Draw(Graphics g, ICanvas canvas)
        {
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
            g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
            g.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
            g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;

            g.Clear(Color.White);

            if (Canvas == null)
            {
                return;
            }

            foreach (var child in canvas.Children)
            {
                if (child is ILine)
                {
                    var line = child as ILine;
                    Pen pen = new Pen(Color.FromArgb(line.Stroke.A, line.Stroke.R, line.Stroke.G, line.Stroke.B), (float)line.StrokeThickness);
                    g.DrawLine(pen, (float)line.X1, (float)line.Y1, (float)line.X2, (float)line.Y2);
                    pen.Dispose();
                }
                else if (child is IBezier)
                {
                    var bezier = child as IBezier;
                    Pen pen = new Pen(Color.FromArgb(bezier.Stroke.A, bezier.Stroke.R, bezier.Stroke.G, bezier.Stroke.B), (float)bezier.StrokeThickness);
                    g.DrawBezier(pen,
                        (float)bezier.Start.X, (float)bezier.Start.Y,
                        (float)bezier.Point1.X, (float)bezier.Point1.Y,
                        (float)bezier.Point2.X, (float)bezier.Point2.Y,
                        (float)bezier.Point3.X, (float)bezier.Point3.Y);
                    pen.Dispose();
                }
                else if (child is IQuadraticBezier)
                {
                    var quadraticBezier = child as IQuadraticBezier;
                    Pen pen = new Pen(Color.FromArgb(quadraticBezier.Stroke.A, quadraticBezier.Stroke.R, quadraticBezier.Stroke.G, quadraticBezier.Stroke.B), (float)quadraticBezier.StrokeThickness);
                    double x1 = quadraticBezier.Start.X;
                    double y1 = quadraticBezier.Start.Y;
                    double x2 = quadraticBezier.Start.X + (2.0 * (quadraticBezier.Point1.X - quadraticBezier.Start.X)) / 3.0;
                    double y2 = quadraticBezier.Start.Y + (2.0 * (quadraticBezier.Point1.Y - quadraticBezier.Start.Y)) / 3.0;
                    double x3 = x2 + (quadraticBezier.Point2.X - quadraticBezier.Start.X) / 3.0;
                    double y3 = y2 + (quadraticBezier.Point2.Y - quadraticBezier.Start.Y) / 3.0;
                    double x4 = quadraticBezier.Point2.X;
                    double y4 = quadraticBezier.Point2.Y;
                    g.DrawBezier(pen,
                        (float)x1, (float)y1,
                        (float)x2, (float)y2,
                        (float)x3, (float)y3,
                        (float)x4, (float)y4);
                    pen.Dispose();
                }
                else if (child is IArc)
                {
                    var arc = child as IArc;
                    if (arc.Width > 0.0 && arc.Height > 0.0)
                    {
                        Pen pen = new Pen(Color.FromArgb(arc.Stroke.A, arc.Stroke.R, arc.Stroke.G, arc.Stroke.B), (float)arc.StrokeThickness);
                        g.DrawArc(pen, (float)arc.X, (float)arc.Y, (float)arc.Width, (float)arc.Height, (float)arc.StartAngle, (float)arc.SweepAngle);
                        pen.Dispose();
                    }
                }
                else if (child is IRectangle)
                {
                    var rectangle = child as IRectangle;
                    Pen pen = new Pen(Color.FromArgb(rectangle.Stroke.A, rectangle.Stroke.R, rectangle.Stroke.G, rectangle.Stroke.B), (float)rectangle.StrokeThickness);
                    g.DrawRectangle(pen, (float)rectangle.X, (float)rectangle.Y, (float)rectangle.Width, (float)rectangle.Height);
                    pen.Dispose();
                }
                else if (child is IEllipse)
                {
                    var ellipse = child as IEllipse;
                    Pen pen = new Pen(Color.FromArgb(ellipse.Stroke.A, ellipse.Stroke.R, ellipse.Stroke.G, ellipse.Stroke.B), (float)ellipse.StrokeThickness);
                    g.DrawEllipse(pen, (float)ellipse.X, (float)ellipse.Y, (float)ellipse.Width, (float)ellipse.Height);
                    pen.Dispose();
                }
                else if (child is IText)
                {
                    var text = child as IText;
                    Brush brush = new SolidBrush(Color.FromArgb(text.Foreground.A, text.Foreground.R, text.Foreground.G, text.Foreground.B));
                    Font font = new Font("Callibri", (float)text.Size);
                    g.DrawString(text.Text, font, brush,
                        new RectangleF((float)text.X, (float)text.Y, (float)text.Width, (float)text.Height),
                        new StringFormat() { Alignment = (StringAlignment)text.HorizontalAlignment, LineAlignment = (StringAlignment)text.VerticalAlignment });
                    brush.Dispose();
                    font.Dispose();
                }
            }
        }
    }

    public class WinFormsCanvas : ICanvas
    {
        public object Native { get; set; }

        public IObservable<ImmutablePoint> Downs { get; set; }
        public IObservable<ImmutablePoint> Ups { get; set; }
        public IObservable<ImmutablePoint> Moves { get; set; }

        public IList<INative> Children { get; set; }

        public double Width { get; set; }
        public double Height { get; set; }
        public IColor Background { get; set; }

        public bool EnableSnap { get; set; }
        public double SnapX { get; set; }
        public double SnapY { get; set; }

        public bool IsCaptured { get; set; }

        private CanvasPanel _control;

        public double Snap(double val, double snap)
        {
            double r = val % snap;
            return r >= snap / 2.0 ? val + snap - r : val - r;
        }

        public WinFormsCanvas(ICanvas canvas, CanvasPanel control)
        {
            Background = canvas.Background;
            SnapX = canvas.SnapX;
            SnapY = canvas.SnapY;
            EnableSnap = canvas.EnableSnap;

            Children = new ObservableCollection<INative>();

            Downs = Observable.FromEventPattern<MouseEventArgs>(control, "MouseDown").Select(e =>
            {
                var p = e.EventArgs.Location;
                return new ImmutablePoint(EnableSnap ? Snap((double)p.X, SnapX) : (double)p.X,
                    EnableSnap ? Snap((double)p.Y, SnapY) : (double)p.Y);
            });

            Ups = Observable.FromEventPattern<MouseEventArgs>(control, "MouseUp").Select(e =>
            {
                var p = e.EventArgs.Location;
                return new ImmutablePoint(EnableSnap ? Snap((double)p.X, SnapX) : (double)p.X,
                    EnableSnap ? Snap((double)p.Y, SnapY) : (double)p.Y);
            });

            Moves = Observable.FromEventPattern<MouseEventArgs>(control, "MouseMove").Select(e =>
            {
                var p = e.EventArgs.Location;
                return new ImmutablePoint(EnableSnap ? Snap((double)p.X, SnapX) : (double)p.X,
                    EnableSnap ? Snap((double)p.Y, SnapY) : (double)p.Y);
            });

            _control = control;
            _control.Canvas = this;

            Native = control;
        }

        public void Capture()
        {
            IsCaptured = true;
        }

        public void ReleaseCapture()
        {
            IsCaptured = false;
        }

        public void Add(INative value)
        {
            Children.Add(value);
        }

        public void Remove(INative value)
        {
            Children.Remove(value);
        }

        public void Clear()
        {
            Children.Clear();
        }

        public void Render(INative context)
        {
            _control.Invalidate();
        }
    }

    public class XModelToWindowsFormsConverter : IModelToNativeConverter
    {
        private CanvasPanel _control;

        public XModelToWindowsFormsConverter(CanvasPanel control)
        {
            _control = control;
        }

        public ILine Convert(ILine line)
        {
            return line;
        }

        public IBezier Convert(IBezier bezier)
        {
            return bezier;
        }

        public IQuadraticBezier Convert(IQuadraticBezier quadraticBezier)
        {
            return quadraticBezier;
        }

        public IArc Convert(IArc arc)
        {
            return arc;
        }

        public IRectangle Convert(IRectangle rectangle)
        {
            return rectangle;
        }

        public IEllipse Convert(IEllipse ellipse)
        {
            return ellipse;
        }

        public IText Convert(IText text)
        {
            return text;
        }

        public ICanvas Convert(ICanvas canvas)
        {
            return new WinFormsCanvas(canvas, _control);
        }
    }
}
