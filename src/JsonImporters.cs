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
    using System.Globalization;
    using System.Linq;
    using Jayrock.Json;
    using Mannex.Collections.Generic;

    public static class JsonImporters
    {
        internal static readonly Func<Type, Delegate> Map;

        static JsonImporters()
        {
            var importers = new Delegate[]
            {
                new Func<JsonReader, bool>           (ImportBoolean             ),
                new Func<JsonReader, int>            (ImportInt32               ),
                new Func<JsonReader, long>           (ImportInt64               ),
                new Func<JsonReader, float>          (ImportSingle              ),
                new Func<JsonReader, double>         (ImportDouble              ),
                new Func<JsonReader, DateTime>       (ImportDateTime            ),
                new Func<JsonReader, DateTimeOffset> (ImportDateTimeOffset      ),
                new Func<JsonReader, string>         (ImportNullOrString        ),
                new Func<JsonReader, bool?>          (ImportNullOrBoolean       ),
                new Func<JsonReader, int?>           (ImportNullOrInt32         ),
                new Func<JsonReader, long?>          (ImportNullOrInt64         ),
                new Func<JsonReader, float?>         (ImportNullOrSingle        ),
                new Func<JsonReader, double?>        (ImportNullOrDouble        ),
                new Func<JsonReader, DateTime?>      (ImportNullOrDateTime      ),
                new Func<JsonReader, DateTimeOffset?>(ImportNullOrDateTimeOffset),
                new Func<JsonReader, JsonValue>      (ImportJsonValue           ),
                new Func<JsonReader, JsonObject>     (JsonObject.Import         ),
            };

            var importerByType = importers.ToDictionary(e => e.GetType().GetGenericArguments().Last(), e => e);
            Map = type => importerByType.GetValue(type, it => new Exception($"Don't know how to import {it.FullName} from JSON."));
        }

        public static Importer<bool>            Boolean          = Importer.Create(ImportBoolean);
        public static Importer<int>             Int32            = Importer.Create(ImportInt32);
        public static Importer<long>            Int64            = Importer.Create(ImportInt64);
        public static Importer<float>           Single           = Importer.Create(ImportSingle);
        public static Importer<double>          Double           = Importer.Create(ImportDouble);
        public static Importer<DateTime>        DateTime         = Importer.Create(ImportDateTime);
        public static Importer<DateTimeOffset>  DateTimeOffset   = Importer.Create(ImportDateTimeOffset);
        public static Importer<bool?>           NullableBoolean  = Importer.Create(ImportNullOrBoolean);
        public static Importer<int?>            NullableInt32    = Importer.Create(ImportNullOrInt32);
        public static Importer<long?>           NullableInt64    = Importer.Create(ImportNullOrInt64);
        public static Importer<float?>          NullableSingle   = Importer.Create(ImportNullOrSingle);
        public static Importer<double?>         NullableDouble   = Importer.Create(ImportNullOrDouble);
        public static Importer<DateTime?>       NullableDateTime = Importer.Create(ImportNullOrDateTime);
        public static Importer<DateTimeOffset?> NullableDateTimeOffset =
                                                                   Importer.Create(ImportNullOrDateTimeOffset);
        public static Importer<string>          String           = Importer.Create(ImportNullOrString);
        public static Importer<JsonValue>       JsonValue        = Importer.Create(ImportJsonValue);
        public static Importer<JsonObject>      JsonObject       = Importer.Create(DeJson.JsonObject.Import);

        static bool     ImportBoolean(JsonReader reader)    => reader.ReadBoolean();
        static int      ImportInt32(JsonReader reader)      => reader.ReadNumber().ToInt32();
        static long     ImportInt64(JsonReader reader)      => reader.ReadNumber().ToInt64();
        static float    ImportSingle(JsonReader reader)     => reader.ReadNumber().ToSingle();
        static double   ImportDouble(JsonReader reader)     => reader.ReadNumber().ToDouble();
        static string   ImportString(JsonReader reader)     => reader.ReadString();

        static readonly string[] DateTimeFormats = {
            "yyyy-MM-dd'T'HH:mm:ss.fffffffK",
            "yyyy-MM-dd'T'HH:mm:ss.ffffK",
            "yyyy-MM-dd'T'HH:mm:ss.fffK",
            "yyyy-MM-dd'T'HH:mm:ss.ffK",
            "yyyy-MM-dd'T'HH:mm:ss.fK",
            "yyyy-MM-dd'T'HH:mm:ssK",
            "yyyy-MM-dd'T'HH:mm:ss.fffffK",
            "yyyy-MM-dd'T'HH:mm:ss.ffffffK",
            "yyyy-MM-dd'T'HH:mm:ss.ffffffffK",
            "yyyy-MM-dd'T'HH:mm:ss.fffffffffK",
            "yyyy-MM-dd'T'HH:mm:ss.ffffffffffK",
            "yyyy-MM-dd'T'HH:mmK",
            "yyyy-MM-dd",
        };

        static DateTime ImportDateTime(JsonReader reader) =>
            System.DateTime.ParseExact(reader.ReadString(), DateTimeFormats,
                CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces);

        static DateTimeOffset ImportDateTimeOffset(JsonReader reader) =>
            System.DateTimeOffset.Parse(reader.ReadString(),
                CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);

        static bool?           ImportNullOrBoolean(JsonReader reader)        => Import(reader, ImportBoolean       , null as bool?          , v => v);
        static int?            ImportNullOrInt32(JsonReader reader)          => Import(reader, ImportInt32         , null as int?           , v => v);
        static long?           ImportNullOrInt64(JsonReader reader)          => Import(reader, ImportInt64         , null as long?          , v => v);
        static float?          ImportNullOrSingle(JsonReader reader)         => Import(reader, ImportSingle        , null as float?         , v => v);
        static double?         ImportNullOrDouble(JsonReader reader)         => Import(reader, ImportDouble        , null as double?        , v => v);
        static DateTime?       ImportNullOrDateTime(JsonReader reader)       => Import(reader, ImportDateTime      , null as DateTime?      , v => v);
        static DateTimeOffset? ImportNullOrDateTimeOffset(JsonReader reader) => Import(reader, ImportDateTimeOffset, null as DateTimeOffset?, v => v);
        static string          ImportNullOrString(JsonReader reader)         => Import(reader, ImportString        , null                   , v => v);

        static JsonValue ImportJsonValue(JsonReader reader) =>
            new JsonValue(JsonBuffer.From(reader));

        static TResult Import<T, TResult>(JsonReader reader,
            Func<JsonReader, T> importer,
            TResult nil,
            Func<T, TResult> selector)
        {
            if (!reader.MoveToContent())
                throw new Exception("Unexpected EOF.");
            if (reader.TokenClass == JsonTokenClass.Null)
            {
                reader.ReadNull();
                return nil;
            }
            return selector(importer(reader));
        }
    }
}
