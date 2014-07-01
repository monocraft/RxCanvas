using RxCanvas.Views;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace RxCanvas.WinForms
{
    public partial class Form1 : Form
    {
        private MainView _mainView;
        private IDictionary<Tuple<Keys, Keys>, Action> _shortcuts;

        public Form1()
        {
            InitializeComponent();

            this.SetStyle(
                ControlStyles.AllPaintingInWmPaint 
                | ControlStyles.UserPaint 
                | ControlStyles.DoubleBuffer
                | ControlStyles.SupportsTransparentBackColor, 
                true);

            _mainView = new MainView();

            // background layer
            _mainView.Layers[0].Background.A = 0xFF;
            _mainView.Layers[0].Background.R = 0xFF;
            _mainView.Layers[0].Background.G = 0xFF;
            _mainView.Layers[0].Background.B = 0xFF;

            // drawing layer
            _mainView.Layers[1].Background.A = 0xFF;
            _mainView.Layers[1].Background.R = 0xF5;
            _mainView.Layers[1].Background.G = 0xF5;
            _mainView.Layers[1].Background.B = 0xF5;

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
                new Tuple<Keys, Keys>(
                    (Keys)keyConverter.ConvertFromString("O"),
                    (Keys)modifiersKeyConverter.ConvertFromString("Control")),
                () => Open());

            // save shortcut
            _shortcuts.Add(
                new Tuple<Keys, Keys>(
                    (Keys)keyConverter.ConvertFromString("S"),
                    (Keys)modifiersKeyConverter.ConvertFromString("Control")),
                () => Save());

            // export shortcut
            _shortcuts.Add(
                new Tuple<Keys, Keys>(
                    (Keys)keyConverter.ConvertFromString("E"),
                    (Keys)modifiersKeyConverter.ConvertFromString("Control")),
                () => Export());

            // undo shortcut
            _shortcuts.Add(
                new Tuple<Keys, Keys>(
                    (Keys)keyConverter.ConvertFromString("Z"),
                    (Keys)modifiersKeyConverter.ConvertFromString("Control")),
                () => _mainView.Undo());

            // redo shortcut
            _shortcuts.Add(
                new Tuple<Keys, Keys>(
                    (Keys)keyConverter.ConvertFromString("Y"),
                    (Keys)modifiersKeyConverter.ConvertFromString("Control")),
                () => _mainView.Redo());

            // snap shortcut
            _shortcuts.Add(
                new Tuple<Keys, Keys>(
                    (Keys)keyConverter.ConvertFromString("S"),
                    Keys.None),
                () => _mainView.ToggleSnap());

            // clear shortcut
            _shortcuts.Add(
                new Tuple<Keys, Keys>(
                    (Keys)keyConverter.ConvertFromString("Delete"),
                    (Keys)modifiersKeyConverter.ConvertFromString("Control")),
                () => _mainView.Clear());

            // editor shortcuts
            foreach (var editor in _mainView.Editors)
            {
                var _editor = editor;
                _shortcuts.Add(
                    new Tuple<Keys, Keys>(
                        (Keys)keyConverter.ConvertFromString(editor.Key),
                        editor.Modifiers == "" ? Keys.None : (Keys)modifiersKeyConverter.ConvertFromString(editor.Modifiers)),
                    () => _mainView.Enable(_editor));
            }
        }

        private void Initialize()
        {
            // add canvas panel to root layout, same panel is used for all layers
            this.SuspendLayout();
            this.Controls.Add(_mainView.Layers.LastOrDefault().Native as WinFormsCanvasPanel);
            this.ResumeLayout(false);

            // create grid canvas
            _mainView.CreateGrid(600.0, 600.0, 30.0, 0.0, 0.0);

            // handle keyboard input
            KeyDown += (sender, e) =>
            {
                Action action;
                bool result = _shortcuts.TryGetValue(
                    new Tuple<Keys, Keys>(e.KeyCode, e.Modifiers), 
                    out action);

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
            _mainView.Render();
        }

        private string ToFileFilter()
        {
            bool first = true;
            string filter = string.Empty;
            foreach (var serializer in _mainView.Files)
            {
                filter += string.Format(
                    "{0}{1} File (*.{2})|*.{2}", 
                    first == false ? "|" : string.Empty, 
                    serializer.Name, 
                    serializer.Extension);

                if (first == true)
                {
                    first = false;
                }
            }
            return filter;
        }

        private string ToCreatorFilter()
        {
            bool first = true;
            string filter = string.Empty;
            foreach (var creator in _mainView.Creators)
            {
                filter += string.Format(
                    "{0}{1} File (*.{2})|*.{2}", 
                    first == false ? "|" : string.Empty, 
                    creator.Name, 
                    creator.Extension);

                if (first == true)
                {
                    first = false;
                }
            }
            return filter;
        }

        private void Open()
        {
            string filter = ToFileFilter();
            int defaultFilterIndex = _mainView.Files
                .IndexOf(_mainView.Files.Where(c => c.Name == "Json")
                .FirstOrDefault()) + 1;

            openFileDialog1.Filter = filter;
            openFileDialog1.FilterIndex = defaultFilterIndex;
            openFileDialog1.ShowDialog(this);
        }

        private void Save()
        {
            string filter = ToFileFilter();
            int defaultFilterIndex = _mainView.Files
                .IndexOf(_mainView.Files.Where(c => c.Name == "Json")
                .FirstOrDefault()) + 1;
            
            saveFileDialog1.Filter = filter;
            saveFileDialog1.FilterIndex = defaultFilterIndex;
            saveFileDialog1.FileName = "canvas";
            saveFileDialog1.ShowDialog(this);
        }

        private void Export()
        {
            string filter = ToCreatorFilter();
            int defaultFilterIndex = _mainView.Creators
                .IndexOf(_mainView.Creators.Where(c => c.Name == "Pdf")
                .FirstOrDefault()) + 1;

            saveFileDialog2.Filter = filter;
            saveFileDialog2.FilterIndex = defaultFilterIndex;
            saveFileDialog2.FileName = "canvas";
            saveFileDialog2.ShowDialog(this);
        }
    }
}
