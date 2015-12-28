﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;
using Orleans.Runtime;

#if !DNXCORE50
namespace Orleans.Serialization
{
    public class BinaryFormatterSerializer : IExternalSerializer
    {
        private TraceLogger logger;
        public void Initialize(TraceLogger logger)
        {
            this.logger = logger;
        }

        public bool IsSupportedType(Type itemType)
        {
            return itemType.GetTypeInfo().IsSerializable;
        }

        public object DeepCopy(object source)
        {
            if (source == null)
            {
                return null;
            }

            var formatter = new BinaryFormatter();
            object ret = null;
            using (var memoryStream = new MemoryStream())
            {
                formatter.Serialize(memoryStream, source);
                memoryStream.Flush();
                memoryStream.Seek(0, SeekOrigin.Begin);
                formatter.Binder = DynamicBinder.Instance;
                ret = formatter.Deserialize(memoryStream);
            }

            return ret;
        }

        public void Serialize(object item, BinaryTokenStreamWriter writer, Type expectedType)
        {
            if (writer == null)
            {
                throw new ArgumentNullException("writer");
            }

            if (item == null)
            {
                writer.WriteNull();
                return;
            }

            var formatter = new BinaryFormatter();
            byte[] bytes;
            using (var memoryStream = new MemoryStream())
            {
                formatter.Serialize(memoryStream, item);
                memoryStream.Flush();
                bytes = memoryStream.ToArray();
            }
            
            writer.Write(bytes.Length);
            writer.Write(bytes);
        }

        public object Deserialize(Type expectedType, BinaryTokenStreamReader reader)
        {
            if (reader == null)
            {
                throw new ArgumentNullException("reader");
            }

            var n = reader.ReadInt();
            var bytes = reader.ReadBytes(n);
            var formatter = new BinaryFormatter();
            object retVal = null;
            using (var memoryStream = new MemoryStream(bytes))
            {
                retVal = formatter.Deserialize(memoryStream);
            }

            return retVal;
        }


        /// <summary>
        /// This appears necessary because the BinaryFormatter by default will not see types
        /// that are defined by the InvokerGenerator.
        /// Needs to be public since it used by generated client code.
        /// </summary>
        class DynamicBinder : SerializationBinder
        {
            public static readonly SerializationBinder Instance = new DynamicBinder();

            private readonly Dictionary<string, Assembly> assemblies = new Dictionary<string, Assembly>();

            public override Type BindToType(string assemblyName, string typeName)
            {
                lock (this.assemblies)
                {
                    Assembly result;
                    if (!this.assemblies.TryGetValue(assemblyName, out result))
                    {
                        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                            this.assemblies[assembly.GetName().FullName] = assembly;

                        // in some cases we have to explicitly load the assembly even though it seems to be already loaded but for some reason it's not listed in AppDomain.CurrentDomain.GetAssemblies()
                        if (!this.assemblies.TryGetValue(assemblyName, out result))
                            this.assemblies[assemblyName] = Assembly.Load(new AssemblyName(assemblyName));

                        result = this.assemblies[assemblyName];
                    }

                    return result.GetType(typeName);
                }
            }
        }
    }
}
#endif
