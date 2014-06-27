﻿using RxCanvas.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RxCanvas.Binary
{
    public class BinaryHistory : IHistory
    {
        private IBinaryFile<ICanvas, Stream> _file;

        private Stack<byte[]> _undos;
        private Stack<byte[]> _redos;

        public BinaryHistory(IBinaryFile<ICanvas, Stream> file)
        {
            _file = file;
            _undos = new Stack<byte[]>();
            _redos = new Stack<byte[]>();
        }

        private void PushUndo(ICanvas canvas)
        {
            using (var stream = new MemoryStream())
            {
                _file.Write(stream, canvas);
                _undos.Push(stream.ToArray());
            }
        }

        private void PushRedo(ICanvas canvas)
        {
            using (var stream = new MemoryStream())
            {
                _file.Write(stream, canvas);
                _redos.Push(stream.ToArray());
            }
        }

        private ICanvas PopUndo()
        {
            byte[] buffer = _undos.Pop();
            using (var stream = new MemoryStream(buffer))
            {
                return _file.Read(stream);
            }
        }

        private ICanvas PopRedo()
        {
            byte[] buffer = _redos.Pop();
            using (var stream = new MemoryStream(buffer))
            {
                return _file.Read(stream);
            }
        }

        public void Snapshot(ICanvas canvas)
        {
            _redos.Clear();
            PushUndo(canvas);
        }

        public ICanvas Undo(ICanvas canvas)
        {
            if (_undos.Count <= 0)
            {
                return null;
            }

            PushRedo(canvas);
            return PopUndo();
        }

        public ICanvas Redo(ICanvas canvas)
        {
            if (_redos.Count <= 0)
            {
                return null;
            }

            PushUndo(canvas);
            return PopRedo();
        }

        public void Clear()
        {
            _undos.Clear();
            _redos.Clear();
        }
    }
}
