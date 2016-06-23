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
        public Importer<T[]> ToArrayImporter() =>
            Importer.Create(JsonImport.CreateArrayImporter(Import));
    }
}