using Autofac;
using RxCanvas.Interfaces;
using RxCanvas.Wpf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RxCanvas
{
    public class WpfModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.Register<INativeConverter>(c => new WpfConverter()).SingleInstance();
        }
    }
}
