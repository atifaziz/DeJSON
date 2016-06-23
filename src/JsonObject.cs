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
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using Jayrock.Json;
    using Mannex.Collections.Generic;
    using JayrockJsonBuffer = Jayrock.Json.JsonBuffer;

    public sealed class JsonObject : IList<KeyValuePair<string, JsonBuffer>>
    {
        readonly KeyValuePair<string, JayrockJsonBuffer>[] _members;

        public JsonObject(KeyValuePair<string, JayrockJsonBuffer>[] members)
        {
            if (members == null) throw new ArgumentNullException(nameof(members));
            _members = members;
        }

        public static JsonObject Import(string json) =>
            Import(JsonText.CreateReader(json));

        internal static JsonObject Import(JsonReader reader)
        {
            if (!reader.MoveToContent())
                throw new Exception(/* TODO */);
            if (reader.TokenClass == JsonTokenClass.Null)
            {
                reader.ReadNull();
                return null;
            }
            KeyValuePair<string, JayrockJsonBuffer>[] members = null;
            reader.ReadToken(JsonTokenClass.Object);
            var count = 0;
            for (; reader.TokenClass != JsonTokenClass.EndObject; count++)
            {
                var name = reader.ReadMember();
                var value = JayrockJsonBuffer.From(reader);
                if (members == null || count >= members.Length)
                    Array.Resize(ref members, members?.Length * 2 ?? 4);
                members[count] = name.AsKeyTo(value);
            }
            Array.Resize(ref members, count);
            reader.ReadToken(JsonTokenClass.EndObject);
            return new JsonObject(members);
        }

        public int Count => _members.Length;

        public KeyValuePair<string, JsonBuffer> this[int index]
        {
            get
            {
                if (index < 0 || index >= Count)
                    throw new ArgumentOutOfRangeException(nameof(index), index, null);
                var member = _members[index];
                return member.Key.AsKeyTo(new JsonBuffer(member.Value));
            }
        }

        public JsonBuffer this[string name]
        {
            get
            {
                var value = Find(name);
                if (value == null)
                    throw new KeyNotFoundException($"JSON object does not have a member named \"{name}\".");
                return value.Value;
            }
        }

        public IEnumerable<string> Names => from m in _members select m.Key;
        public IEnumerable<JsonBuffer> Values => from m in this select m.Value;

        public JsonBuffer? Find(string name)
        {
            var i = IndexOf(name);
            return i >= 0 ? new JsonBuffer(_members[i].Value) : null as JsonBuffer?;
        }

        public bool Contains(string name) => IndexOf(name) >= 0;

        public int IndexOf(string name)
        {
            if (name == null) throw new ArgumentNullException(nameof(name));
            for (var i = 0; i < Count; i++)
            {
                if (_members[i].Key.Equals(name, StringComparison.OrdinalIgnoreCase))
                    return i;
            }
            return -1;
        }

        public IEnumerator<KeyValuePair<string, JsonBuffer>> GetEnumerator()
        {
            // ReSharper disable once LoopCanBeConvertedToQuery
            foreach (var member in _members)
                yield return member.Key.AsKeyTo(new JsonBuffer(member.Value));
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        bool ICollection<KeyValuePair<string, JsonBuffer>>.IsReadOnly => true;

        bool ICollection<KeyValuePair<string, JsonBuffer>>.Contains(KeyValuePair<string, JsonBuffer> item) =>
            IndexOf(item) >= 0;

        int IList<KeyValuePair<string, JsonBuffer>>.IndexOf(KeyValuePair<string, JsonBuffer> item) =>
            IndexOf(item);

        int IndexOf(KeyValuePair<string, JsonBuffer> item)
        {
            var i = IndexOf(item.Key);
            return i >= 0 && this[i].Value.Equals(item.Value) ? i : 0;
        }

        KeyValuePair<string, JsonBuffer> IList<KeyValuePair<string, JsonBuffer>>.this[int index]
        {
            get { return this[index]; }
            // ReSharper disable once ValueParameterNotUsed
            set { ThrowReadOnlyError(); }
        }

        void ICollection<KeyValuePair<string, JsonBuffer>>.CopyTo(KeyValuePair<string, JsonBuffer>[] array, int arrayIndex) =>
            this.ToArray().CopyTo(array, arrayIndex);

        static void ThrowReadOnlyError() { throw new InvalidOperationException("Collection is read-only"); }
        static T ThrowReadOnlyError<T>() { ThrowReadOnlyError(); return default(T); }

        void ICollection<KeyValuePair<string, JsonBuffer>>.Add(KeyValuePair<string, JsonBuffer> item) => ThrowReadOnlyError();
        void ICollection<KeyValuePair<string, JsonBuffer>>.Clear() => ThrowReadOnlyError();
        void IList<KeyValuePair<string, JsonBuffer>>.Insert(int index, KeyValuePair<string, JsonBuffer> item) => ThrowReadOnlyError();
        bool ICollection<KeyValuePair<string, JsonBuffer>>.Remove(KeyValuePair<string, JsonBuffer> item) => ThrowReadOnlyError<bool>();
        void IList<KeyValuePair<string, JsonBuffer>>.RemoveAt(int index) => ThrowReadOnlyError();
    }
}