using Autofac;
using RxCanvas.Binary;
using RxCanvas.Bounds;
using RxCanvas.Creators;
using RxCanvas.Editors;
using RxCanvas.Interfaces;
using RxCanvas.Model;
using RxCanvas.Serializers;
using RxCanvas.Xaml;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace RxCanvas
{
    public class Bootstrapper
    {
        public IContainer Build()
        {
            // register components
            var builder = new ContainerBuilder();

            var editorAssembly = Assembly.GetAssembly(typeof(XCanvasFactory));
            builder.RegisterAssemblyTypes(editorAssembly)
                .Where(t => t.Name.EndsWith("Editor"))
                .AsImplementedInterfaces()
                .InstancePerLifetimeScope();

            var serializerAssembly = Assembly.GetAssembly(typeof(JsonFile));
            builder.RegisterAssemblyTypes(serializerAssembly)
                .Where(t => t.Name.EndsWith("File"))
                .AsImplementedInterfaces()
                .SingleInstance();

            var creatorAssembly = Assembly.GetAssembly(typeof(PdfCreator));
            builder.RegisterAssemblyTypes(creatorAssembly)
                .Where(t => t.Name.EndsWith("Creator"))
                .AsImplementedInterfaces()
                .SingleInstance();

            builder.Register<IModelConverter>(c => new XModelConverter()).SingleInstance();
            builder.Register<ICanvasFactory>(c => new XCanvasFactory()).SingleInstance();
            builder.Register<INativeConverter>(c => new WpfConverter()).SingleInstance();

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
                var binaryFile = c.Resolve<IList<IFile<ICanvas, Stream>>>().Where(e => e.Name == "Binary").FirstOrDefault();
                var xcanvas = canvasFactory.CreateCanvas();
                xcanvas.History = new BinaryHistory(binaryFile);
                return nativeConverter.Convert(xcanvas);
            }).InstancePerLifetimeScope();

            // create container
            return builder.Build();
        }
    }
}
