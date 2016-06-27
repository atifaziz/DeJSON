namespace DeJson.Tests
{
    using System;
    using System.Linq;
    using NUnit.Framework;

    [TestFixture]
    public class JsonImportTests
    {
        [Test]
        public void ImportObject()
        {
            var importer = JsonImport.CreateImporter(() => new
            {
                x     = default(int),
                y     = default(int?),
                z     = default(double),
                label = default(string),
                date  = default(DateTime),
            });

            var obj = importer.Import(@"{ x: 12, y: 34, z: 56.78,
                                          label: foobar, date: '2000-12-04' }");

            Assert.That(obj, Is.EqualTo(new
            {
                x = 12,
                y = 34 as int?,
                z = 56.78,
                label = "foobar",
                date  = new DateTime(2000, 12, 04),
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

            var obj = importer.Import("{ pt: { x: 12, y: 34, z: 56.78 }, label: foobar }");

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
            var obj = importer.Import("{ tags: [foo, bar, baz], xs: [123, 456, 789] }");
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

            var obj = importer.Import(@"{
                points: [
                    { x: 12, y: 23 },
                    { x: 34, y: 45 },
                    { x: 56, y: 67 },
                    { x: 78, y: 89 },
                ]
            }");

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
            var importer = JsonImporters.Int32.ToArrayImporter();
            var result = importer.Import("[123, 456, 789]");
            Assert.That(result, Is.EquivalentTo(new[] { 123, 456, 789 }));
        }

        [Test]
        public void ImportObjectArray()
        {
            var importer = JsonImport.CreateImporter(() => new[]
            {
                new { x = default(int),
                      y = default(int) },
            });

            var pts = importer.Import(@"[
                { x: 12, y: 23 },
                { x: 34, y: 45 },
                { x: 56, y: 67 },
                { x: 78, y: 89 },
            ]");

            Assert.That(pts.Length, Is.EqualTo(4));

            using (var pt = pts.AsEnumerable().GetEnumerator())
            {
                Assert.That(pt.MoveNext(), Is.True);
                Assert.That(pt.Current.x, Is.EqualTo(12));
                Assert.That(pt.Current.y, Is.EqualTo(23));

                Assert.That(pt.MoveNext(), Is.True);
                Assert.That(pt.Current.x, Is.EqualTo(34));
                Assert.That(pt.Current.y, Is.EqualTo(45));

                Assert.That(pt.MoveNext(), Is.True);
                Assert.That(pt.Current.x, Is.EqualTo(56));
                Assert.That(pt.Current.y, Is.EqualTo(67));

                Assert.That(pt.MoveNext(), Is.True);
                Assert.That(pt.Current.x, Is.EqualTo(78));
                Assert.That(pt.Current.y, Is.EqualTo(89));

                Assert.That(pt.MoveNext(), Is.False);
            }
        }

        [Test]
        public void ImportJson()
        {
            var importer = JsonImport.CreateImporter(() => new
            {
                pt = default(JsonValue),
                label = default(string),
            });

            var obj = importer.Import("{ pt: { x: 12, y: 34, z: 56.78 }, label: foobar }");

            Assert.That(obj.label, Is.EqualTo("foobar"));

            var pointImporter = JsonImport.CreateImporter(() => new
            {
                x = default(int),
                y = default(int?),
                z = default(double),
            });

            var pt = pointImporter.Import(obj.pt);

            Assert.That(pt, Is.EqualTo(new
            {
                x = 12,
                y = 34 as int?,
                z = 56.78,
            }));
        }

        [Test]
        public void ImportJsonObject()
        {
            var importer = JsonImport.CreateImporter(() => new
            {
                pt = default(JsonObject),
                label = default(string),
            });

            var obj = importer.Import("{ pt: { x: 12, y: 34, z: 56.78 }, label: foobar }");

            Assert.That(obj.label, Is.EqualTo("foobar"));
            Assert.That(obj.pt.Count, Is.EqualTo(3));

            var pt = obj.pt.GetEnumerator();

            Assert.That(pt.MoveNext(), Is.True);
            Assert.That(pt.Current.Key, Is.EqualTo("x"));
            Assert.That(pt.Current.Value.Import(JsonImporters.Int32), Is.EqualTo(12));

            Assert.That(pt.MoveNext(), Is.True);
            Assert.That(pt.Current.Key, Is.EqualTo("y"));
            Assert.That(pt.Current.Value.Import(JsonImporters.Int32), Is.EqualTo(34));

            Assert.That(pt.MoveNext(), Is.True);
            Assert.That(pt.Current.Key, Is.EqualTo("z"));
            Assert.That(pt.Current.Value.Import(JsonImporters.Double), Is.EqualTo(56.78));
        }
    }
}
