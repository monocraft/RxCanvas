using Autofac;
using RxCanvas.Interfaces;
using RxCanvas.WinForms;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RxCanvas.Views
{
    public class NativeModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.Register<INativeConverter>(c =>
            {
                var panel = new WinFormsCanvasPanel();
                panel.Location = new System.Drawing.Point(100, 12);
                panel.Name = "canvasPanel";
                panel.Size = new System.Drawing.Size(600, 600);
                panel.TabIndex = 0;
                return new WinFormsConverter(panel);
            }).SingleInstance();
        }
    }
}
