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

    public static partial class JsonImport
    {
        public static Importer<T> CreateImporter<T>(Expression<Func<T>> prototype) =>
            Importer.Create(CreateImporter(prototype, JsonImporters.Map));

        static Func<JsonReader, T> CreateImporter<T>(Expression<Func<T>> prototype, Func<Type, Delegate> mapper)
        {
            if (prototype == null) throw new ArgumentNullException(nameof(prototype));
            var newExpression = prototype.Body as NewExpression;
            if (newExpression?.Members == null)
                throw new ArgumentException(/* TODO */ null, nameof(prototype));
            return (Func<JsonReader, T>) CreateImporter(newExpression, typeof(T), mapper);
        }

        static readonly RuntimeMethodHandle[] CreateImporterMethods =
            typeof(JsonImport)
                .FindMembers(MemberTypes.Method, BindingFlags.Static | BindingFlags.NonPublic,
                             filterCriteria: null,
                             filter: (m, _) => m.Name == nameof(JsonImport.CreateImporter))
                .Cast<MethodInfo>()
                .Where(m => m.IsGenericMethodDefinition)
                .OrderBy(m => m.GetGenericArguments().Length)
                .Select(m => m.MethodHandle)
                .ToArray();

        static Delegate CreateImporter(NewExpression @new, Type type, Func<Type, Delegate> mapper)
        {
            var properties = (@new.Members ?? Enumerable.Empty<MemberInfo>()).Cast<PropertyInfo>().ToArray();
            var readers =
                from p in properties.Zip(@new.Arguments, (prop, arg) => new
                {
                    Property = prop,
                    New      = arg as NewExpression,
                    Array    = arg as NewArrayExpression,
                    Const    = arg as ConstantExpression,
                })
                select p.New != null
                     ? CreateImporter(p.New, p.New.Type, mapper)
                     : p.Array != null
                     ? CreateArrayImporter(p.Array.Type.GetElementType(), CreateImporter(p.Array.Expressions.Cast<NewExpression>().Single(), p.Array.Type.GetElementType(), mapper))
                     : p.Const == null
                     ? null // TODO
                     : p.Const.Type.IsArray
                     ? CreateArrayImporter(p.Const.Type.GetElementType(), mapper(p.Property.PropertyType.GetElementType()))
                     : mapper(p.Property.PropertyType);

            readers = readers.ToArray();

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
                                  body: Expression.New(@new.Constructor,
                                                       // ReSharper disable once CoVariantArrayConversion
                                                       paramz));

            var createImporterMethod =
                ((MethodInfo) MethodBase.GetMethodFromHandle(CreateImporterMethods[lambdaType.GetGenericArguments().Length - 1]))
                    .MakeGenericMethod(lambdaType.GetGenericArguments());

            var args =
                new object[] { names }
                    .Concat(readers)
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
