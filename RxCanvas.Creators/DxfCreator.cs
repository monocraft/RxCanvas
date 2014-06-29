using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RxCanvas.Interfaces;

namespace RxCanvas.Creators
{
    public class DxfCreator : ICreator<ICanvas>
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
