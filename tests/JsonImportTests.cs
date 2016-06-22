namespace DeJson.Tests
{
    using System;
    using Jayrock.Json;
    using NUnit.Framework;

    [TestFixture]
    public class JsonImportTests
    {
        [TestCase(true, "true")]
        [TestCase(false, "false")]
        public void ImportBoolean(bool expected, string json) =>
            Assert.That(JsonImport.ImportBoolean(ReadJson(json)), Is.EqualTo(expected));

        [TestCase(true , "true")]
        [TestCase(false, "false")]
        [TestCase(null , "null")]
        public void TryImportBoolean(bool? expected, string json) =>
            Assert.That(JsonImport.TryImportBoolean(ReadJson(json)), Is.EqualTo(expected));

        [TestCase(42, "42")]
        public void ImportInt32(int expected, string json) =>
            Assert.That(JsonImport.ImportInt32(ReadJson(json)), Is.EqualTo(expected));

        [TestCase(42, "42")]
        [TestCase(null, "null")]
        public void TryImportInt32(int? expected, string json) =>
            Assert.That(JsonImport.TryImportInt32(ReadJson(json)), Is.EqualTo(expected));

        [TestCase(42L, "42")]
        public void ImportInt64(long expected, string json) =>
            Assert.That(JsonImport.ImportInt64(ReadJson(json)), Is.EqualTo(expected));

        [TestCase(42L, "42")]
        [TestCase(null, "null")]
        public void TryImportInt64(long? expected, string json) =>
            Assert.That(JsonImport.TryImportInt64(ReadJson(json)), Is.EqualTo(expected));

        [TestCase(3.14f, "3.14")]
        public void ImportSingle(float expected, string json) =>
            Assert.That(JsonImport.ImportSingle(ReadJson(json)), Is.EqualTo(expected));

        [TestCase(3.14f, "3.14")]
        [TestCase(null, "null")]
        public void TryImportSingle(float? expected, string json) =>
            Assert.That(JsonImport.TryImportSingle(ReadJson(json)), Is.EqualTo(expected));

        [TestCase(3.14, "3.14")]
        public void ImportDouble(double expected, string json) =>
            Assert.That(JsonImport.ImportDouble(ReadJson(json)), Is.EqualTo(expected));

        [TestCase(3.14, "3.14")]
        [TestCase(null, "null")]
        public void TryImportDouble(double? expected, string json) =>
            Assert.That(JsonImport.TryImportDouble(ReadJson(json)), Is.EqualTo(expected));

        [TestCase("foobar", @"foobar")]
        [TestCase("foo\nbar", @"'foo\nbar'")]
        public void ImportString(string expected, string json) =>
            Assert.That(JsonImport.ImportString(ReadJson(json)), Is.EqualTo(expected));

        [Test]
        public void ImportObject()
        {
            var importer = JsonImport.CreateImporter(() => new
            {
                x     = default(int),
                y     = default(int?),
                z     = default(double),
                label = default(string),
            });

            var obj = importer(ReadJson(@"{ x: 12, y: 34, z: 56.78, label: foobar }"));

            Assert.That(obj, Is.EqualTo(new
            {
                x = 12,
                y = 34 as int?,
                z = 56.78,
                label = "foobar"
            }));
        }

        [Test]
        public void ImportNestedObject()
        {
            var importer = JsonImport.CreateImporter(() => new
            {
                pt = new
                {
                    x = default(int),
                    y = default(int?),
                    z = default(double),
                },
                label = default(string),
            });

            var obj = importer(ReadJson(@"{ pt: { x: 12, y: 34, z: 56.78 }, label: foobar }"));

            Assert.That(obj, Is.EqualTo(new
            {
                pt = new
                {
                    x = 12,
                    y = 34 as int?,
                    z = 56.78,
                },
                label = "foobar"
            }));
        }

        [Test]
        public void ImportNestedArrays()
        {
            var importer = JsonImport.CreateImporter(() => new
            {
                xs = default(int[]),
                tags = default(string[])
            });
            var obj = importer(ReadJson(@"{ tags: [foo, bar, baz], xs: [123, 456, 789] }"));
            Assert.That(obj.xs, Is.EquivalentTo(new[] { 123, 456, 789 }));
            Assert.That(obj.tags, Is.EquivalentTo(new[] { "foo", "bar", "baz" }));
        }

        [Test]
        public void ImportNestedObjectArray()
        {
            var importer = JsonImport.CreateImporter(() => new
            {
                points = new[] { new { x = default(int), y = default(int) } },
            });

            var obj = importer(ReadJson(@"{
                points: [
                    { x: 12, y: 23 },
                    { x: 34, y: 45 },
                    { x: 56, y: 67 },
                    { x: 78, y: 89 },
                ]
            }"));

            Assert.That(obj.points, Is.EquivalentTo(new[]
            {
                new { x = 12, y = 23 },
                new { x = 34, y = 45 },
                new { x = 56, y = 67 },
                new { x = 78, y = 89 },
            }));
        }

        [Test]
        public void ImportArray()
        {
            var importer = new Func<JsonReader, int>(JsonImport.ImportInt32).CreateArrayImporter();
            var result = importer(ReadJson("[123, 456, 789]"));
            Assert.That(result, Is.EquivalentTo(new[] { 123, 456, 789 }));
        }

        static JsonReader ReadJson(string json) =>
            JsonText.CreateReader(json);
    }
}
