using Microsoft.Win32;
using RxCanvas.Views;
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
                new Tuple<Key, ModifierKeys>(
                    (Key)keyConverter.ConvertFromString("O"),
                    (ModifierKeys)modifiersKeyConverter.ConvertFromString("Control")),
                () => Open());

            // save shortcut
            _shortcuts.Add(
                new Tuple<Key, ModifierKeys>(
                    (Key)keyConverter.ConvertFromString("S"),
                    (ModifierKeys)modifiersKeyConverter.ConvertFromString("Control")),
                () => Save());

            // export shortcut
            _shortcuts.Add(
                new Tuple<Key, ModifierKeys>(
                    (Key)keyConverter.ConvertFromString("E"),
                    (ModifierKeys)modifiersKeyConverter.ConvertFromString("Control")),
                () => Export());

            // undo shortcut
            _shortcuts.Add(
                new Tuple<Key, ModifierKeys>(
                    (Key)keyConverter.ConvertFromString("Z"),
                    (ModifierKeys)modifiersKeyConverter.ConvertFromString("Control")),
                () => _mainView.Undo());

            // redo shortcut
            _shortcuts.Add(
                new Tuple<Key, ModifierKeys>(
                    (Key)keyConverter.ConvertFromString("Y"),
                    (ModifierKeys)modifiersKeyConverter.ConvertFromString("Control")),
                () => _mainView.Redo());

            // snap shortcut
            _shortcuts.Add(
                new Tuple<Key, ModifierKeys>(
                    (Key)keyConverter.ConvertFromString("S"),
                    ModifierKeys.None),
                () => _mainView.ToggleSnap());

            // clear shortcut
            _shortcuts.Add(
                new Tuple<Key, ModifierKeys>(
                    (Key)keyConverter.ConvertFromString("Delete"),
                    (ModifierKeys)modifiersKeyConverter.ConvertFromString("Control")),
                () => _mainView.Clear());

            // editor shortcuts
            foreach (var editor in _mainView.Editors)
            {
                var _editor = editor;
                _shortcuts.Add(
                    new Tuple<Key, ModifierKeys>(
                        (Key)keyConverter.ConvertFromString(editor.Key),
                        editor.Modifiers == "" ? ModifierKeys.None : (ModifierKeys)modifiersKeyConverter.ConvertFromString(editor.Modifiers)),
                    () => _mainView.Enable(_editor));
            }
        }

        private void Initialize()
        {
            // add canvas to root layout
            Layout.Children.Add(_mainView.Layers[0].Native as UIElement);
            Layout.Children.Add(_mainView.Layers[1].Native as UIElement);

            // create grid canvas
            _mainView.CreateGrid(600.0, 600.0, 30.0, 0.0, 0.0);

            // handle keyboard input
            PreviewKeyDown += (sender, e) =>
            {
                Action action;
                bool result = _shortcuts.TryGetValue(
                    new Tuple<Key, ModifierKeys>(e.Key, Keyboard.Modifiers), 
                    out action);

                if (result == true && action != null)
                {
                    action();
                }
            };

            // set data context
            DataContext = _mainView.Layers[1];
        }

        private string FilesFilter()
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

        private string CreatorsFilter()
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
            string filter = FilesFilter();
            int defaultFilterIndex = _mainView.Files
                .IndexOf(_mainView.Files.Where(c => c.Name == "Json")
                .FirstOrDefault()) + 1;

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
            int defaultFilterIndex = _mainView.Files
                .IndexOf(_mainView.Files.Where(c => c.Name == "Json")
                .FirstOrDefault()) + 1;

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
            int defaultFilterIndex = _mainView.Creators
                .IndexOf(_mainView.Creators.Where(c => c.Name == "Pdf")
                .FirstOrDefault()) + 1;

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
}
