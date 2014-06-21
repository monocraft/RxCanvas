using RxCanvas.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RxCanvas
{
    public class Utf8TextFile : ITextFile
    {
        public string Open(string path)
        {
            using (var ts = System.IO.File.OpenText(path))
            {
                return ts.ReadToEnd();
            }
        }

        public void Save(string path, string text)
        {
            using (var ts = System.IO.File.CreateText(path))
            {
                ts.Write(text);
            }
        }
    }
}
