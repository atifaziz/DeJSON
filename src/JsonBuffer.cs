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
    using Jayrock.Json;

    public struct JsonBuffer : IEquatable<JsonBuffer>
    {
        readonly Jayrock.Json.JsonBuffer _buffer;

        internal JsonBuffer(Jayrock.Json.JsonBuffer buffer) { _buffer = buffer; }

        public static readonly JsonBuffer Empty = new JsonBuffer();

        public bool IsEmpty      => _buffer.IsEmpty;
        public bool IsNull       => _buffer.IsNull;
        public bool IsScalar     => _buffer.IsScalar;
        public bool IsBoolean    => IsScalarType(JsonTokenClass.Boolean);
        public bool IsNumber     => IsScalarType(JsonTokenClass.Number);
        public bool IsString     => IsScalarType(JsonTokenClass.String);
        public bool IsStructured => _buffer.IsStructured;
        public bool IsObject     => _buffer.IsObject;
        public bool IsArray      => _buffer.IsArray;

        bool IsScalarType(JsonTokenClass klass) =>
            IsScalar && _buffer[0].Class == klass;

        // ReSharper disable once ImpureMethodCallOnReadonlyValueField
        internal JsonReader CreateReader() =>
            _buffer.CreateReader();

        public override int GetHashCode() =>
            _buffer.GetHashCode();

        // ReSharper disable once ImpureMethodCallOnReadonlyValueField
        public bool Equals(JsonBuffer other) =>
            _buffer.Equals(other._buffer);

        public override bool Equals(object obj) =>
            obj is JsonBuffer && Equals((JsonBuffer) obj);

        public override string ToString() => _buffer.ToString();

        public T Import<T>(Importer<T> importer) =>
            importer.Import(CreateReader());
    }
}