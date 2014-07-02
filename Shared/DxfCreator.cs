using RxCanvas.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RxCanvas.Creators
{
    public class DxfCreator : ICreator
    {
        public string Name { get; set; }
        public string Extension { get; set; }

        public DxfCreator()
        {
            Name = "Dxf";
            Extension = "dxf";
        }

        public void Save(string path, ICanvas canvas)
        {
            throw new NotImplementedException();
        }

        public void Save(string path, IEnumerable<ICanvas> canvases)
        {
            throw new NotImplementedException();
        }
    }
}
