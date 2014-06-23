using Autofac;
using RxCanvas.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RxCanvas
{
    public class History : IHistory
    {
        private ICanvas _canvas;
        private IBinaryFile<ICanvas, Stream> _file;

        private Stack<byte[]> _undos;
        private Stack<byte[]> _redos;

        public History(ICanvas canvas, IBinaryFile<ICanvas, Stream> file)
        {
            _file = file;
            _canvas = canvas;

            _undos = new Stack<byte[]>();
            _redos = new Stack<byte[]>();
        }

        public void Snapshot()
        {
            using (var stream = new MemoryStream())
            {
                _file.Write(stream, _canvas);
                _undos.Push(stream.ToArray());
            }
        }

        public void Undo()
        {
            throw new NotImplementedException();
        }

        public void Redo()
        {
            throw new NotImplementedException();
        }
    }
}
