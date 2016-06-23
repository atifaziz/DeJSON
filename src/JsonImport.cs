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
    using Mannex.Collections.Generic;

    public static partial class JsonImport
    {
        static readonly Func<Type, Delegate> ImporterMapper;

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

        public static Importer<T> CreateImporter<T>(Expression<Func<T>> prototype) =>
            Importer.Create(CreateImporterCore(prototype));

        static Func<JsonReader, T> CreateImporterCore<T>(Expression<Func<T>> prototype) =>
            CreateImporter(prototype, ImporterMapper);

        static Func<JsonReader, T> CreateImporter<T>(Expression<Func<T>> prototype, Func<Type, Delegate> mapper)
        {
            if (prototype == null) throw new ArgumentNullException(nameof(prototype));
            var newExpression = prototype.Body as NewExpression;
            if (newExpression?.Members == null)
                throw new ArgumentException(null, nameof(prototype));
            return (Func<JsonReader, T>) CreateImporterLambda(newExpression, typeof(T), mapper);
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

        static Delegate CreateImporterLambda(NewExpression @new, Type type, Func<Type, Delegate> mapper)
        {
            var properties = (@new.Members ?? Enumerable.Empty<MemberInfo>()).Cast<PropertyInfo>().ToArray();
            var readers =
                from p in properties.Zip(@new.Arguments, (p, a) => new
                {
                    Property = p,
                    New      = a as NewExpression,
                    Array    = a as NewArrayExpression,
                    Const    = a as ConstantExpression,
                })
                select p.New != null
                     ? CreateImporterLambda(p.New, p.New.Type, mapper)
                     : p.Array != null
                     ? CreateArrayImporter(p.Array.Type.GetElementType(), CreateImporterLambda(p.Array.Expressions.Cast<NewExpression>().Single(), p.Array.Type.GetElementType(), mapper))
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

            var lambdaType = SelectorTypes[properties.Length - 1].MakeGenericType(propertyTypes.Concat(new[] { type }).ToArray());
            var selectorLambda =
                Expression.Lambda(lambdaType,
                                  parameters: paramz,
                                  body: Expression.New(@new.Constructor,
                                                       // ReSharper disable once CoVariantArrayConversion
                                                       paramz));

            var createImporterCreatorMethod =
                ((MethodInfo) MethodBase.GetMethodFromHandle(CreateImporterMethods[lambdaType.GetGenericArguments().Length - 1]))
                    .MakeGenericMethod(lambdaType.GetGenericArguments());

            var selector = selectorLambda.Compile();

            var args =
                new object[] { names }
                    .Concat(readers)
                    .Concat(new object[] { selector })
                    .ToArray();

            var importer = (Delegate) createImporterCreatorMethod.Invoke(null, args.ToArray());
            return importer;
        }

        static readonly MethodInfo CreateArrayImporterCoreGenericMethodDefinition =
            new Func<Func<JsonReader, int>, Func<JsonReader, int[]>>(CreateArrayImporter).Method.GetGenericMethodDefinition();

        static Delegate CreateArrayImporter(Type type, Delegate del) =>
            (Delegate)
                CreateArrayImporterCoreGenericMethodDefinition
                    .MakeGenericMethod(type)
                    .Invoke(null, new object[] { del });

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

        static JsonImport()
        {
            var importers = new Delegate[]
            {
                new Func<JsonReader, bool>      (ImportBoolean    ),
                new Func<JsonReader, int>       (ImportInt32      ),
                new Func<JsonReader, long>      (ImportInt64      ),
                new Func<JsonReader, float>     (ImportSingle     ),
                new Func<JsonReader, double>    (ImportDouble     ),
                new Func<JsonReader, string>    (TryImportString  ),
                new Func<JsonReader, bool?>     (TryImportBoolean ),
                new Func<JsonReader, int?>      (TryImportInt32   ),
                new Func<JsonReader, long?>     (TryImportInt64   ),
                new Func<JsonReader, float?>    (TryImportSingle  ),
                new Func<JsonReader, double?>   (TryImportDouble  ),
                new Func<JsonReader, JsonValue>(ImportJson       ),
                new Func<JsonReader, JsonObject>(JsonObject.Import),
            };

            var importerByType = importers.ToDictionary(e => e.GetType().GetGenericArguments().Last(), e => e);
            ImporterMapper = type => importerByType.GetValue(type, it => new Exception($"Don't know how to import {it.FullName} from JSON."));
        }

        public static Importer<bool>       BooleanImporter    = Importer.Create(ImportBoolean);
        public static Importer<int>        Int32Importer      = Importer.Create(ImportInt32);
        public static Importer<long>       Int64Importer      = Importer.Create(ImportInt64);
        public static Importer<float>      SingleImporter     = Importer.Create(ImportSingle);
        public static Importer<double>     DoubleImporter     = Importer.Create(ImportDouble);
        public static Importer<string>     StringImporter     = Importer.Create(ImportString);

        public static Importer<bool?>      OptBooleanImporter = Importer.Create(TryImportBoolean);
        public static Importer<int?>       OptInt32Importer   = Importer.Create(TryImportInt32);
        public static Importer<long?>      OptInt64Importer   = Importer.Create(TryImportInt64);
        public static Importer<float?>     OptSingleImporter  = Importer.Create(TryImportSingle);
        public static Importer<double?>    OptDoubleImporter  = Importer.Create(TryImportDouble);
        public static Importer<string>     OptStringImporter  = Importer.Create(TryImportString);

        public static Importer<JsonValue> JsonImporter       = Importer.Create(ImportJson);
        public static Importer<JsonObject> JsonObjectImporter = Importer.Create(JsonObject.Import);

        static bool    ImportBoolean(JsonReader reader)    => reader.ReadBoolean();
        static int     ImportInt32(JsonReader reader)      => reader.ReadNumber().ToInt32();
        static long    ImportInt64(JsonReader reader)      => reader.ReadNumber().ToInt64();
        static float   ImportSingle(JsonReader reader)     => reader.ReadNumber().ToSingle();
        static double  ImportDouble(JsonReader reader)     => reader.ReadNumber().ToDouble();
        static string  ImportString(JsonReader reader)     => reader.ReadString();

        static bool?   TryImportBoolean(JsonReader reader) => TryImportNullable(reader, ImportBoolean);
        static int?    TryImportInt32(JsonReader reader)   => TryImportNullable(reader, ImportInt32);
        static long?   TryImportInt64(JsonReader reader)   => TryImportNullable(reader, ImportInt64);
        static float?  TryImportSingle(JsonReader reader)  => TryImportNullable(reader, ImportSingle);
        static double? TryImportDouble(JsonReader reader)  => TryImportNullable(reader, ImportDouble);
        static string  TryImportString(JsonReader reader)  => TryImport(reader, ImportString);

        static JsonValue ImportJson(JsonReader reader) =>
            new JsonValue(JsonBuffer.From(reader));

        static T? TryImportNullable<T>(JsonReader reader, Func<JsonReader, T> selector)
            where T : struct
        {
            if (!reader.MoveToContent())
                throw new Exception("Unexpected EOF.");
            if (reader.TokenClass == JsonTokenClass.Null)
            {
                reader.ReadNull();
                return null;
            }
            return selector(reader);
        }

        static T TryImport<T>(JsonReader reader, Func<JsonReader, T> selector)
            where T : class
        {
            if (reader.TokenClass == JsonTokenClass.Null)
            {
                reader.ReadNull();
                return null;
            }
            return selector(reader);
        }
    }
}
