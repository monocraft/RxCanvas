using Autofac;
using RxCanvas.Core;
using RxCanvas.Creators;
using RxCanvas.Editors;
using RxCanvas.Model;
using RxCanvas.Serializers;
using System;
using System.Collections.Generic;
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

            var editorAssembly = Assembly.GetAssembly(typeof(PortableXDefaultsFactory));
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
            builder.Register<ICanvasFactory>(c => new PortableXDefaultsFactory()).SingleInstance();
            builder.Register<IModelToNativeConverter>(c => new XModelToWinFormsConverter(panel)).SingleInstance();

            builder.Register<ICanvas>(c =>
            {
                var canvasFactory = c.Resolve<ICanvasFactory>();
                var xcanvas = canvasFactory.CreateCanvas();
                var nativeConverter = c.Resolve<IModelToNativeConverter>();
                return nativeConverter.Convert(xcanvas);
            }).InstancePerLifetimeScope();

            builder.Register<ITextFile>(f => new Utf8TextFile()).SingleInstance();

            // create container
            return builder.Build();
        }
    }
}
