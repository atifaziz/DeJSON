namespace DeJson.Tests
{
    using NUnit.Framework;

    [TestFixture]
    public class JsonImportersTests
    {
        [TestCase(true, "true")]
        [TestCase(false, "false")]
        public void ImportBoolean(bool expected, string json) =>
            Assert.That(JsonImporters.Boolean.Import(json), Is.EqualTo(expected));

        [TestCase(true , "true")]
        [TestCase(false, "false")]
        [TestCase(null , "null")]
        public void ImportNullableBoolean(bool? expected, string json) =>
            Assert.That(JsonImporters.NullableBoolean.Import(json), Is.EqualTo(expected));

        [TestCase(42, "42")]
        public void ImportInt32(int expected, string json) =>
            Assert.That(JsonImporters.Int32.Import(json), Is.EqualTo(expected));

        [TestCase(42, "42")]
        [TestCase(null, "null")]
        public void TryImportInt32(int? expected, string json) =>
            Assert.That(JsonImporters.NullableInt32.Import(json), Is.EqualTo(expected));

        [TestCase(42L, "42")]
        public void ImportInt64(long expected, string json) =>
            Assert.That(JsonImporters.Int64.Import(json), Is.EqualTo(expected));

        [TestCase(42L, "42")]
        [TestCase(null, "null")]
        public void TryImportInt64(long? expected, string json) =>
            Assert.That(JsonImporters.NullableInt64.Import(json), Is.EqualTo(expected));

        [TestCase(3.14f, "3.14")]
        public void ImportSingle(float expected, string json) =>
            Assert.That(JsonImporters.Single.Import(json), Is.EqualTo(expected));

        [TestCase(3.14f, "3.14")]
        [TestCase(null, "null")]
        public void ImportNullableSingle(float? expected, string json) =>
            Assert.That(JsonImporters.NullableSingle.Import(json), Is.EqualTo(expected));

        [TestCase(3.14, "3.14")]
        public void ImportDouble(double expected, string json) =>
            Assert.That(JsonImporters.Double.Import(json), Is.EqualTo(expected));

        [TestCase(3.14, "3.14")]
        [TestCase(null, "null")]
        public void ImportNullableDouble(double? expected, string json) =>
            Assert.That(JsonImporters.NullableDouble.Import(json), Is.EqualTo(expected));

        [TestCase("foobar", @"foobar")]
        [TestCase("foo\nbar", @"'foo\nbar'")]
        public void ImportString(string expected, string json) =>
            Assert.That(JsonImporters.String.Import(json), Is.EqualTo(expected));
    }
}