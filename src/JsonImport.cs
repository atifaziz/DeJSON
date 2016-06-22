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

    static class Importer
    {
        internal static Importer<T> Create<T>(Func<JsonReader, T> func) =>
            new Importer<T>(func);
    }

    public sealed class Importer<T>
    {
        readonly Func<JsonReader, T> _impl;
        public Importer(Func<JsonReader, T> impl) { _impl = impl; }
        internal T Import(JsonReader reader) => _impl(reader);
        public T Import(string json) => _impl(ReadJson(json));
        static JsonReader ReadJson(string json) => JsonText.CreateReader(json);
    }

    public static partial class JsonImport
    {
        public static Importer<T[]> CreateArrayImporter<T>(this Importer<T> importer)
        {
            if (importer == null) throw new ArgumentNullException(nameof(importer));
            return Importer.Create(CreateArrayImporterCore(importer.Import));
        }

        static Func<JsonReader, T[]> CreateArrayImporterCore<T>(Func<JsonReader, T> importer) =>
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
            CreateImporter(prototype, new Delegate[]
            {
                new Func<JsonReader, bool   >(ImportBoolean   ),
                new Func<JsonReader, bool?  >(TryImportBoolean),
                new Func<JsonReader, int    >(ImportInt32     ),
                new Func<JsonReader, int?   >(TryImportInt32  ),
                new Func<JsonReader, long   >(ImportInt64     ),
                new Func<JsonReader, long?  >(TryImportInt64  ),
                new Func<JsonReader, float  >(ImportSingle    ),
                new Func<JsonReader, float? >(TryImportSingle ),
                new Func<JsonReader, double >(ImportDouble    ),
                new Func<JsonReader, double?>(TryImportDouble ),
                new Func<JsonReader, string >(TryImportString ),
            }
            .ToDictionary(e => e.GetType().GetGenericArguments().Last(), e => e));

        static Func<JsonReader, T> CreateImporter<T>(Expression<Func<T>> prototype, IDictionary<Type, Delegate> map)
        {
            if (prototype == null) throw new ArgumentNullException(nameof(prototype));
            var newExpression = prototype.Body as NewExpression;
            if (newExpression?.Members == null)
                throw new ArgumentException(null, nameof(prototype));
            return (Func<JsonReader, T>) CreateImporterLambda(newExpression, typeof(T), map);
        }

        static Exception CannotImportTypeError(Type type) =>
            new Exception($"Don't know how to import {type.FullName} from JSON.");

        static Delegate CreateImporterLambda(NewExpression @new, Type type, IDictionary<Type, Delegate> map)
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
                     ? CreateImporterLambda(p.New, p.New.Type, map)
                     : p.Array != null
                     ? CreateArrayImporterLambda(p.Array.Type.GetElementType(), CreateImporterLambda(p.Array.Expressions.Cast<NewExpression>().Single(), p.Array.Type.GetElementType(), map)).Compile()
                     : p.Const == null
                     ? null // TODO
                     : p.Const.Type.IsArray
                     ? CreateArrayImporterLambda(p.Const.Type.GetElementType(), map.GetValue(p.Property.PropertyType.GetElementType(), CannotImportTypeError)).Compile()
                     : map.GetValue(p.Property.PropertyType, CannotImportTypeError);

            readers = readers.ToArray();

            var names = from p in properties select p.Name;
            var propertyTypes = properties.Select(p => p.PropertyType).ToArray();
            var paramz = properties.Select(p => Expression.Parameter(p.PropertyType))
                                   .ToArray();

            var lambdaType = GenericFuncDefinitions[properties.Length - 1].MakeGenericType(propertyTypes.Concat(new[] { type }).ToArray());
            var selectorLambda =
                Expression.Lambda(lambdaType,
                                  parameters: paramz,
                                  body: Expression.New(@new.Constructor,
                                                       // ReSharper disable once CoVariantArrayConversion
                                                       paramz));

            var importCreatorMethod =
                typeof(JsonImport)
                    .FindMembers(MemberTypes.Method, BindingFlags.Static | BindingFlags.NonPublic, filterCriteria: null,
                                 filter: (m, _) => m.Name == nameof(JsonImport.CreateImporter))
                    .Cast<MethodInfo>()
                    .ToArray()
                    .Single(m => m.GetGenericArguments().Length == lambdaType.GetGenericArguments().Length)
                    .MakeGenericMethod(lambdaType.GetGenericArguments());

            var selector = selectorLambda.Compile();

            var args =
                new object[] { names }
                    .Concat(readers)
                    .Concat(new object[] { selector })
                    .ToArray();

            var importer = (Delegate) importCreatorMethod.Invoke(null, args.ToArray());
            return importer;
        }

        static readonly MethodInfo CreateArrayImporterCoreGenericMethodDefinition =
            new Func<Func<JsonReader, int>, Func<JsonReader, int[]>>(CreateArrayImporterCore).Method.GetGenericMethodDefinition();

        static LambdaExpression CreateArrayImporterLambda(Type type, Delegate del)
        {
            var dels = CreateArrayImporterCoreGenericMethodDefinition.MakeGenericMethod(type).Invoke(null, new object[] { del });
            var reader = Expression.Parameter(typeof(JsonReader));
            var body = Expression.Invoke(Expression.Constant(dels), reader);
            var lambdaType = typeof(Func<,>).MakeGenericType(typeof(JsonReader), type.MakeArrayType());
            return Expression.Lambda(lambdaType, body, reader);
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

        public static Importer<bool   > BooleanImporter    = Importer.Create(ImportBoolean   );
        public static Importer<bool?  > OptBooleanImporter = Importer.Create(TryImportBoolean);
        public static Importer<int    > Int32Importer      = Importer.Create(ImportInt32     );
        public static Importer<int?   > OptInt32Importer   = Importer.Create(TryImportInt32  );
        public static Importer<long   > Int64Importer      = Importer.Create(ImportInt64     );
        public static Importer<long?  > OptInt64Importer   = Importer.Create(TryImportInt64  );
        public static Importer<float  > SingleImporter     = Importer.Create(ImportSingle    );
        public static Importer<float? > OptSingleImporter  = Importer.Create(TryImportSingle );
        public static Importer<double > DoubleImporter     = Importer.Create(ImportDouble    );
        public static Importer<double?> OptDoubleImporter  = Importer.Create(TryImportDouble );
        public static Importer<string > StringImporter     = Importer.Create(ImportString    );
        public static Importer<string > OptStringImporter  = Importer.Create(TryImportString );

        static bool    ImportBoolean(JsonReader reader)    => reader.ReadBoolean();
        static bool?   TryImportBoolean(JsonReader reader) => TryImportNullable(reader, ImportBoolean);
        static int     ImportInt32(JsonReader reader)      => reader.ReadNumber().ToInt32();
        static int?    TryImportInt32(JsonReader reader)   => TryImportNullable(reader, ImportInt32);
        static long    ImportInt64(JsonReader reader)      => reader.ReadNumber().ToInt64();
        static long?   TryImportInt64(JsonReader reader)   => TryImportNullable(reader, ImportInt64);
        static float   ImportSingle(JsonReader reader)     => reader.ReadNumber().ToSingle();
        static float?  TryImportSingle(JsonReader reader)  => TryImportNullable(reader, ImportSingle);
        static double  ImportDouble(JsonReader reader)     => reader.ReadNumber().ToDouble();
        static double? TryImportDouble(JsonReader reader)  => TryImportNullable(reader, ImportDouble);
        static string  ImportString(JsonReader reader)     => reader.ReadString();
        static string  TryImportString(JsonReader reader)  => TryImport(reader, ImportString);

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
