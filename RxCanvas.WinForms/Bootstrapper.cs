using Autofac;
using RxCanvas.Binary;
using RxCanvas.Bounds;
using RxCanvas.Creators;
using RxCanvas.Editors;
using RxCanvas.Interfaces;
using RxCanvas.Model;
using RxCanvas.Serializers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace RxCanvas.WinForms
{
    public class Bootstrapper
    {
        public IContainer Build(WinFormsCanvasPanel panel)
        {
            // register components
            var builder = new ContainerBuilder();

            var editorAssembly = Assembly.GetAssembly(typeof(XModelFactory));
            builder.RegisterAssemblyTypes(editorAssembly)
                .Where(t => t.Name.EndsWith("Editor"))
                .AsImplementedInterfaces()
                .InstancePerLifetimeScope();

            var serializerAssembly = Assembly.GetAssembly(typeof(JsonXModelSerializer));
            builder.RegisterAssemblyTypes(serializerAssembly)
                .Where(t => t.Name.EndsWith("Serializer"))
                .AsImplementedInterfaces()
                .SingleInstance();

            var creatorAssembly = Assembly.GetAssembly(typeof(CoreCanvasPdfCreator));
            builder.RegisterAssemblyTypes(creatorAssembly)
                .Where(t => t.Name.EndsWith("Creator"))
                .AsImplementedInterfaces()
                .SingleInstance();

            builder.Register<ICoreToModelConverter>(c => new CoreToXModelConverter()).SingleInstance();
            builder.Register<ICanvasFactory>(c => new XModelFactory()).SingleInstance();
            builder.Register<IModelToNativeConverter>(c => new XModelToWinFormsConverter(panel)).SingleInstance();

            builder.Register<ITextFile>(c => new Utf8TextFile()).SingleInstance();
            builder.Register<IBinaryFile<ICanvas, Stream>>(c => new BinaryFile()).SingleInstance();

            builder.Register<IBoundsFactory>(c =>
            {
                var nativeConverter = c.Resolve<IModelToNativeConverter>();
                var canvasFactory = c.Resolve<ICanvasFactory>();
                return new BoundsFactory(nativeConverter, canvasFactory);
            }).InstancePerLifetimeScope();

            builder.Register<ICanvas>(c =>
            {
                var nativeConverter = c.Resolve<IModelToNativeConverter>();
                var canvasFactory = c.Resolve<ICanvasFactory>();
                var binaryFile = c.Resolve<IBinaryFile<ICanvas, Stream>>();
                var xcanvas = canvasFactory.CreateCanvas();
                xcanvas.History = new History(binaryFile);
                return nativeConverter.Convert(xcanvas);
            }).InstancePerLifetimeScope();

            // create container
            return builder.Build();
        }
    }
}
