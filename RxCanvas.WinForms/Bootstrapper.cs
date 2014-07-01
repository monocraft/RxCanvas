using Autofac;
using Autofac.Integration.Mef;
using RxCanvas.Binary;
using RxCanvas.Bounds;
using RxCanvas.Creators;
using RxCanvas.Editors;
using RxCanvas.Interfaces;
using RxCanvas.Model;
using RxCanvas.Serializers;
using RxCanvas.WinForms;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition.Hosting;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace RxCanvas.Views
{
    public class Bootstrapper
    {
        public IContainer Build()
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();

            // register components
            var builder = new ContainerBuilder();
            var catalog = new DirectoryCatalog(AppDomain.CurrentDomain.BaseDirectory);

            builder.RegisterComposablePartCatalog(catalog);

            builder.RegisterAssemblyTypes(assemblies)
                .As<IEditor>()
                .InstancePerLifetimeScope();

            builder.Register<IModelConverter>(c => new XModelConverter()).SingleInstance();
            builder.Register<ICanvasFactory>(c => new XCanvasFactory()).SingleInstance();

            builder.Register<IBoundsFactory>(c =>
            {
                var nativeConverter = c.Resolve<INativeConverter>();
                var canvasFactory = c.Resolve<ICanvasFactory>();
                return new BoundsFactory(nativeConverter, canvasFactory);
            }).InstancePerLifetimeScope();

            builder.Register<ICanvas>(c =>
            {
                var nativeConverter = c.Resolve<INativeConverter>();
                var canvasFactory = c.Resolve<ICanvasFactory>();
                var binaryFile = c.Resolve<IList<IFile>>().Where(e => e.Name == "Binary").FirstOrDefault();
                var xcanvas = canvasFactory.CreateCanvas();
                xcanvas.History = new BinaryHistory(binaryFile);
                return nativeConverter.Convert(xcanvas);
            }).InstancePerLifetimeScope();

            builder.Register<INativeConverter>(c =>
            {
                var panel = new WinFormsCanvasPanel();
                panel.Location = new System.Drawing.Point(100, 12);
                panel.Name = "canvasPanel";
                panel.Size = new System.Drawing.Size(600, 600);
                panel.TabIndex = 0;
                return new WinFormsConverter(panel);
            }).SingleInstance();

            // create container
            return builder.Build();
        }
    }
}
