#region Copyright (c) 2016 Atif Aziz. All rights reserved.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//
#endregion

namespace DeJson
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Reflection;
    using Jayrock.Json;

    public sealed class JsonImporter<T>
    {
        readonly Func<JsonReader, T> _impl;
        public JsonImporter(Func<JsonReader, T> impl) { _impl = impl; }
        internal T Import(JsonReader reader) => _impl(reader);
        public T Import(string json) => _impl(ReadJson(json));
        public T Import(JsonValue json) => _impl(json.CreateReader());
        static JsonReader ReadJson(string json) => JsonText.CreateReader(json);
        public JsonImporter<T[]> ToArrayImporter() =>
            JsonImporter.Create(JsonImporter.CreateArrayImporter(Import));
    }

    public static partial class JsonImporter
    {
        static readonly RuntimeMethodHandle[] CreateImporterMethods =
            typeof(JsonImporter)
                .FindMembers(MemberTypes.Method, BindingFlags.Static | BindingFlags.NonPublic,
                             filterCriteria: null,
                             filter: (m, _) => m.Name == nameof(JsonImporter.CreateImporter))
                .Cast<MethodInfo>()
                .Where(m => m.IsGenericMethodDefinition)
                .OrderBy(m => m.GetGenericArguments().Length)
                .Select(m => m.MethodHandle)
                .ToArray();

        internal static JsonImporter<T> Create<T>(Func<JsonReader, T> func) =>
            new JsonImporter<T>(func);

        public static JsonImporter<T> FromPrototype<T>(T prototype) =>
            (JsonImporter<T>)
                Cache.GetOrAdd(typeof(T), t => Create((Func<JsonReader, T>) FromPrototype(typeof(T), JsonImporters.Map)));

        static bool LikeAnonymousClass(Type type) =>
            type.IsNotPublic && type.IsClass && type.IsSealed
            && type.IsDefined(typeof(System.Runtime.CompilerServices.CompilerGeneratedAttribute), false);

        static Delegate FromPrototype(Type prototype, Func<Type, Delegate> mapper)
        {
            if (LikeAnonymousClass(prototype))
                return CreateObjectImporter(prototype, mapper, nameof(prototype));

            if (prototype.IsArray && LikeAnonymousClass(prototype.GetElementType()))
            {
                var elementType = prototype.GetElementType();
                return CreateArrayImporter(elementType, CreateObjectImporter(elementType, mapper, nameof(prototype)));
            }

            return prototype.IsArray
                 ? CreateArrayImporter(prototype.GetElementType(), mapper(prototype.GetElementType()))
                 : mapper(prototype);
        }

        static readonly ConcurrentDictionary<Type, object> Cache = new ConcurrentDictionary<Type, object>();

        static Delegate CreateObjectImporter(Type type, Func<Type, Delegate> mapper, string publicParamName)
        {
            var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            if (properties.Length == 0)
                throw new ArgumentException("Prototype object must have at least one member.", publicParamName);

            var names = from p in properties select p.Name;
            var propertyTypes = properties.Select(p => p.PropertyType).ToArray();
            var paramz = properties.Select(p => Expression.Parameter(p.PropertyType))
                                   .ToArray();

            var lambdaType =
                SelectorTypes[properties.Length - 1]
                    .MakeGenericType(propertyTypes.Concat(new[] { type }).ToArray());

            var ctor = type.GetConstructors().Single();

            var selectorLambda =
                Expression.Lambda(lambdaType,
                                  parameters: paramz,
                                  body: Expression.New(ctor,
                                                       // ReSharper disable once CoVariantArrayConversion
                                                       paramz));

            var createImporterMethod =
                ((MethodInfo) MethodBase.GetMethodFromHandle(CreateImporterMethods[lambdaType.GetGenericArguments().Length - 2]))
                    .MakeGenericMethod(lambdaType.GetGenericArguments());

            var args =
                new object[] { names }
                    .Concat(from arg in ctor.GetParameters()
                            select FromPrototype(arg.ParameterType, mapper))
                    .Concat(new object[] { selectorLambda.Compile() });

            return (Delegate) createImporterMethod.Invoke(null, args.ToArray());
        }

        static IEnumerator<string> Members(JsonReader reader)
        {
            if (!reader.MoveToContent())
                throw new Exception();
            reader.ReadToken(JsonTokenClass.Object);
            while (reader.TokenClass == JsonTokenClass.Member)
                yield return reader.ReadMember();
            reader.ReadToken(JsonTokenClass.EndObject);
            // TODO consider asserting depth on entry/exit
        }

        static readonly MethodInfo CreateArrayImporterGenericMethodDefinition =
            new Func<Func<JsonReader, int>, Func<JsonReader, int[]>>(CreateArrayImporter).Method.GetGenericMethodDefinition();

        internal static Func<JsonReader, T[]> CreateArrayImporter<T>(Func<JsonReader, T> importer) =>
            reader =>
            {
                if (!reader.MoveToContent())
                    throw new Exception();
                reader.ReadToken(JsonTokenClass.Array);
                var list = new List<T>();
                while (reader.TokenClass != JsonTokenClass.EndArray)
                    list.Add(importer(reader));
                reader.Read();
                return list.ToArray();
            };

        static Delegate CreateArrayImporter(Type type, Delegate del) =>
            (Delegate)
                CreateArrayImporterGenericMethodDefinition
                    .MakeGenericMethod(type)
                    .Invoke(null, new object[] { del });
    }
}
