using Autofac;
using Autofac.Integration.Mef;
using RxCanvas.Binary;
using RxCanvas.Bounds;
using RxCanvas.Editors;
using RxCanvas.Interfaces;
using RxCanvas.Model;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition.Hosting;
using System.Linq;
using System.Reflection;

namespace RxCanvas.Views
{
    public class Bootstrapper
    {
        public IContainer Build()
        {
            var builder = new ContainerBuilder();

            // mef exports
            var catalog = new DirectoryCatalog(AppDomain.CurrentDomain.BaseDirectory);
            builder.RegisterComposablePartCatalog(catalog);

            // shared editors
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            builder.RegisterAssemblyTypes(assemblies)
                .As<IEditor>()
                .InstancePerLifetimeScope();

            // shared model
            builder.Register<IModelConverter>(c => new XModelConverter()).SingleInstance();
            builder.Register<ICanvasFactory>(c => new XCanvasFactory()).SingleInstance();

            // native bounds
            builder.Register<IBoundsFactory>(c =>
            {
                var nativeConverter = c.Resolve<INativeConverter>();
                var canvasFactory = c.Resolve<ICanvasFactory>();
                return new BoundsFactory(nativeConverter, canvasFactory);
            }).InstancePerLifetimeScope();

            // native canvas
            builder.Register<ICanvas>(c =>
            {
                var nativeConverter = c.Resolve<INativeConverter>();
                var canvasFactory = c.Resolve<ICanvasFactory>();
                var binaryFile = c.Resolve<IList<IFile>>().Where(e => e.Name == "Binary").FirstOrDefault();
                var xcanvas = canvasFactory.CreateCanvas();
                xcanvas.History = new BinaryHistory(binaryFile);
                return nativeConverter.Convert(xcanvas);
            }).InstancePerLifetimeScope();

            // native modules
            builder.RegisterAssemblyModules(Assembly.GetExecutingAssembly());

            return builder.Build();
        }
    }
}
