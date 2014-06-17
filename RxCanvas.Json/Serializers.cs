using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RxCanvas.Core;
using RxCanvas.Model;
using Newtonsoft.Json;
using System.Runtime.Serialization.Formatters;
using System.Runtime.Serialization;

namespace RxCanvas.Serializers
{
    public class XModelSerializationBinder : SerializationBinder
    {
        public override void BindToName(Type serializedType, out string assemblyName, out string typeName)
        {
            assemblyName = null;
            typeName = serializedType.Name.Substring(1, serializedType.Name.Length - 1);
        }

        public override Type BindToType(string assemblyName, string typeName)
        {
            string resolvedTypeName = string.Format("RxCanvas.Model.{0}, RxCanvas.Model", 'X' + typeName);
            return Type.GetType(resolvedTypeName, true);
        }
    }

    public class JsonXModelSerializer : ISerializer<ICanvas>
    {
        public string Name { get; set; }
        public string Extension { get; set; }

        public JsonXModelSerializer()
        {
            Name = "Json";
            Extension = "json";
        }

        public void Serialize(string path, ICanvas canvas)
        {
            var json = JsonSerialize(canvas);
            Save(path, json);
        }

        public ICanvas Deserialize(string path)
        {
            string json = Open(path);
            var canvas = JsonDeserialize(json);
            return canvas;
        }

        private static string JsonSerialize(ICanvas canvas)
        {
            var binder = new XModelSerializationBinder();
            var json = JsonConvert.SerializeObject(canvas,
                Formatting.Indented,
                new JsonSerializerSettings()
                {
                    ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                    TypeNameHandling = TypeNameHandling.Auto,
                    Binder = binder,
                    NullValueHandling = NullValueHandling.Ignore
                });
            return json;
        }

        private static ICanvas JsonDeserialize(string json)
        {
            var binder = new XModelSerializationBinder();
            var canvas = JsonConvert.DeserializeObject<XCanvas>(json,
                new JsonSerializerSettings()
                {
                    ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                    TypeNameHandling = TypeNameHandling.Auto,
                    Binder = binder,
                    NullValueHandling = NullValueHandling.Ignore
                });
            return canvas;
        }

        private static string Open(string path)
        {
            string json;
            using (var fs = System.IO.File.Open(path, System.IO.FileMode.Open))
            {
                using (var reader = new System.IO.StreamReader(fs))
                {
                    json = reader.ReadToEnd();
                }
            }
            return json;
        }

        private static void Save(string path, string json)
        {
            using (var fs = System.IO.File.Create(path))
            {
                using (var writer = new System.IO.StreamWriter(fs))
                {
                    writer.Write(json);
                }
            }
        }
    }
}
