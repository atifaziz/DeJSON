namespace DeJson.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using NUnit.Framework;

    [TestFixture]
    public class JsonObjectTests
    {
        [Test]
        public void Empty()
        {
            AssertEmpty(JsonObject.Empty);
        }

        static void AssertEmpty(JsonObject obj)
        {
            Assert.That(obj.Count, Is.EqualTo(0));
            Assert.That(obj.Names.Any(), Is.False);
            Assert.That(obj.Values.Any(), Is.False);
            Assert.That(obj.IndexOf("foobar"), Is.EqualTo(-1));
            Assert.That(obj.Find("foobar"), Is.Null);
        }

        [Test]
        public void Init()
        {
            var obj = new JsonObject(new[]
            {
                new KeyValuePair<string, JsonValue>("foo", JsonImport.JsonImporter.Import("123")),
                new KeyValuePair<string, JsonValue>("bar", JsonImport.JsonImporter.Import("456")),
                new KeyValuePair<string, JsonValue>("baz", JsonImport.JsonImporter.Import("789")),
            });

            using (var e = obj.GetEnumerator())
            {
                Assert.That(e.MoveNext(), Is.True);
                Assert.That(e.Current.Key, Is.EqualTo("foo"));
                Assert.That(e.Current.Value.ToString(), Is.EqualTo("123"));

                Assert.That(e.MoveNext(), Is.True);
                Assert.That(e.Current.Key, Is.EqualTo("bar"));
                Assert.That(e.Current.Value.ToString(), Is.EqualTo("456"));

                Assert.That(e.MoveNext(), Is.True);
                Assert.That(e.Current.Key, Is.EqualTo("baz"));
                Assert.That(e.Current.Value.ToString(), Is.EqualTo("789"));

                Assert.That(e.MoveNext(), Is.False);
            }
        }

        [Test]
        public void CannotInitWithNullMembers()
        {
            // ReSharper disable once ObjectCreationAsStatement
            var e = Assert.Throws<ArgumentNullException>(() => new JsonObject(null));
            Assert.That(e.ParamName, Is.EqualTo("members"));
        }

        [Test]
        public void CannotInitWithAnyMemberNameBeingNull()
        {
            // ReSharper disable once ObjectCreationAsStatement
            var e = Assert.Throws<ArgumentException>(() => new JsonObject(new[]
            {
                new KeyValuePair<string, JsonValue>("foo", JsonImport.JsonImporter.Import("123")),
                new KeyValuePair<string, JsonValue>(null, JsonImport.JsonImporter.Import("789")),
            }));
            Assert.That(e.ParamName, Is.EqualTo("members"));
            Assert.That(e.Message.IndexOf("(#2)", StringComparison.Ordinal), Is.GreaterThan(0));
        }

        [Test]
        public void CannotInitWithAnyMemberValueBeingEmpty()
        {
            // ReSharper disable once ObjectCreationAsStatement
            var e = Assert.Throws<ArgumentException>(() => new JsonObject(new[]
            {
                new KeyValuePair<string, JsonValue>("foo", JsonImport.JsonImporter.Import("123")),
                new KeyValuePair<string, JsonValue>("bar", JsonValue.Empty),
            }));
            Assert.That(e.ParamName, Is.EqualTo("members"));
            Assert.That(e.Message.IndexOf("(#2)", StringComparison.Ordinal), Is.GreaterThan(0));
        }

        [Test]
        public void IndexerOnEmptyThrows()
        {
            var e = Assert.Throws<ArgumentOutOfRangeException>(() =>
            {   // ReSharper disable once UnusedVariable
                var _ = JsonObject.Import("{}")[0];
            });
            Assert.That(e.ParamName, Is.EqualTo("index"));
            Assert.That(e.ActualValue, Is.EqualTo(0));
        }

        [Test]
        public void InitEmpty() => AssertEmpty(JsonObject.Import("{}"));

        [Test]
        public void ImportEmpty() => AssertEmpty(JsonObject.Import("{}"));

        [Test]
        public void IndexerWithNegativeIndexThrows()
        {
            var e = Assert.Throws<ArgumentOutOfRangeException>(() =>
            {   // ReSharper disable once UnusedVariable
                var _ = JsonObject.Import("{}")[-1];
            });
            Assert.That(e.ParamName, Is.EqualTo("index"));
            Assert.That(e.ActualValue, Is.EqualTo(-1));
        }

        [TestCase("{ foo: 123, bar: 456, baz: 789 }", "foo", "123")]
        [TestCase("{ foo: 123, bar: 456, baz: 789 }", "bar", "456")]
        [TestCase("{ foo: 123, bar: 456, baz: 789 }", "baz", "789")]
        public void IndexerWithName(string json, string name, string expected)
        {
            var obj = JsonObject.Import(json);
            Assert.That(obj[name].ToString(), Is.EqualTo(expected));
        }

        [TestCase("{ foo: 123, bar: 456, baz: 789 }", 0, "foo", "123")]
        [TestCase("{ foo: 123, bar: 456, baz: 789 }", 1, "bar", "456")]
        [TestCase("{ foo: 123, bar: 456, baz: 789 }", 2, "baz", "789")]
        public void IndexerWithIndex(string json, int index, string key, string value)
        {
            var obj = JsonObject.Import(json);
            var member = obj[index];
            Assert.That(member.Key, Is.EqualTo(key));
            Assert.That(member.Value.ToString(), Is.EqualTo(value));
        }

        [Test]
        public void IndexerWithNonExistingNameThrows()
        {
            const string name = "foobar";
            var e = Assert.Throws<KeyNotFoundException>(() =>
            {   // ReSharper disable once UnusedVariable
                var _ = JsonObject.Import("{}")[name];
            });
            const char quote = '\"';
            var index = e.Message.IndexOf(quote + name + quote, StringComparison.Ordinal);
            Assert.That(index, Is.GreaterThanOrEqualTo(0));
        }

        [Test]
        public void Members()
        {
            var obj = JsonObject.Import("{ foo: 123, bar: 456, baz: 789 }");

            Assert.That(obj.Count, Is.EqualTo(3));

            Assert.That(obj.Names.ToArray(),
                        Is.EquivalentTo(new[] { "foo", "bar", "baz" }));

            Assert.That(obj.Values.Select(v => v.ToString()).ToArray(),
                        Is.EquivalentTo(new[] { "123", "456", "789" }));
        }

        [Test]
        public void Enumeration()
        {
            using (var e = JsonObject.Import("{ foo: 123, bar: 456, baz: 789 }").GetEnumerator())
            {
                Assert.That(e.MoveNext(), Is.True);
                Assert.That(e.Current.Key, Is.EqualTo("foo"));
                Assert.That(e.Current.Value.ToString(), Is.EqualTo("123"));

                Assert.That(e.MoveNext(), Is.True);
                Assert.That(e.Current.Key, Is.EqualTo("bar"));
                Assert.That(e.Current.Value.ToString(), Is.EqualTo("456"));

                Assert.That(e.MoveNext(), Is.True);
                Assert.That(e.Current.Key, Is.EqualTo("baz"));
                Assert.That(e.Current.Value.ToString(), Is.EqualTo("789"));

                Assert.That(e.MoveNext(), Is.False);
            }
        }

        [TestCase("{ foo: 123, bar: 456 }", "foo", "123")]
        [TestCase("{ foo: 123, bar: 456 }", "bar", "456")]
        [TestCase("{ foo: 123, bar: 456 }", "baz", null)]
        public void Find(string json, string name, string expected)
        {
            var obj = JsonObject.Import(json);
            Assert.That(obj.Find(name)?.ToString(), Is.EqualTo(expected));
        }

        [TestCase("{ foo: 123, bar: 456 }", "foo", 0)]
        [TestCase("{ foo: 123, bar: 456 }", "bar", 1)]
        [TestCase("{ foo: 123, bar: 456 }", "baz", -1)]
        public void IndexOf(string json, string name, int expected)
        {
            var obj = JsonObject.Import(json);
            Assert.That(obj.IndexOf(name), Is.EqualTo(expected));
        }

        [TestCase("{ foo: 123, bar: 456 }", "foo", true)]
        [TestCase("{ foo: 123, bar: 456 }", "bar", true)]
        [TestCase("{ foo: 123, bar: 456 }", "baz", false)]
        public void IndexOf(string json, string name, bool expected)
        {
            var obj = JsonObject.Import(json);
            Assert.That(obj.Contains(name), Is.EqualTo(expected));
        }
    }
}