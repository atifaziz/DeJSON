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

        public static JsonImporter<T> Create<T>(Expression<Func<T>> prototype) =>
            Create((Func<JsonReader, T>)Create(prototype.Body, JsonImporters.Map));

        static Delegate Create(Expression prototype, Func<Type, Delegate> mapper)
        {
            var newObject = prototype as NewExpression;
            if (newObject != null)
            {
                if (newObject.Members == null)
                    throw new ArgumentException("Prototype object must have at least one member.", nameof(prototype));
                return CreateObjectImporter(newObject, newObject.Type, mapper);
            }

            var newArray = prototype as NewArrayExpression;
            if (newArray != null)
            {
                var elementType = newArray.Type.GetElementType();
                var newElement = newArray.Expressions.Cast<NewExpression>().Single();
                return CreateArrayImporter(elementType, CreateObjectImporter(newElement, elementType, mapper));
            }

            return prototype.Type.IsArray
                 ? CreateArrayImporter(prototype.Type.GetElementType(), mapper(prototype.Type.GetElementType()))
                 : mapper(prototype.Type);
        }

        static Delegate CreateObjectImporter(NewExpression newExpression, Type type, Func<Type, Delegate> mapper)
        {
            var properties = (newExpression.Members ?? Enumerable.Empty<MemberInfo>()).Cast<PropertyInfo>().ToArray();
            var names = from p in properties select p.Name;
            var propertyTypes = properties.Select(p => p.PropertyType).ToArray();
            var paramz = properties.Select(p => Expression.Parameter(p.PropertyType))
                                   .ToArray();

            var lambdaType =
                SelectorTypes[properties.Length - 1]
                    .MakeGenericType(propertyTypes.Concat(new[] { type }).ToArray());

            var selectorLambda =
                Expression.Lambda(lambdaType,
                                  parameters: paramz,
                                  body: Expression.New(newExpression.Constructor,
                                                       // ReSharper disable once CoVariantArrayConversion
                                                       paramz));

            var createImporterMethod =
                ((MethodInfo) MethodBase.GetMethodFromHandle(CreateImporterMethods[lambdaType.GetGenericArguments().Length - 2]))
                    .MakeGenericMethod(lambdaType.GetGenericArguments());

            var args =
                new object[] { names }
                    .Concat(from arg in newExpression.Arguments select Create(arg, mapper))
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
