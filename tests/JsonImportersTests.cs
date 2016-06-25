namespace DeJson.Tests
{
    using System;
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

        [TestCase(2000, 12, 04, 00, 00, 00, 000.0000, DateTimeKind.Unspecified, "2000-12-04")]
        [TestCase(2000, 12, 04, 06, 22, 00, 000.0000, DateTimeKind.Unspecified, "2000-12-04T06:22")]
        [TestCase(2000, 12, 04, 05, 22, 00, 000.0000, DateTimeKind.Utc        , "2000-12-04T05:22Z")]
        [TestCase(2000, 12, 04, 05, 22, 00, 000.0000, DateTimeKind.Utc        , "2000-12-04T06:22+01:00")]
        [TestCase(2000, 12, 04, 06, 22, 42, 000.0000, DateTimeKind.Unspecified, "2000-12-04T06:22:42")]
        [TestCase(2000, 12, 04, 05, 22, 42, 000.0000, DateTimeKind.Utc        , "2000-12-04T05:22:42Z")]
        [TestCase(2000, 12, 04, 05, 22, 42, 000.0000, DateTimeKind.Utc        , "2000-12-04T06:22:42+01:00")]
        [TestCase(2000, 12, 04, 06, 22, 42, 100.0000, DateTimeKind.Unspecified, "2000-12-04T06:22:42.1")]
        [TestCase(2000, 12, 04, 05, 22, 42, 100.0000, DateTimeKind.Utc        , "2000-12-04T05:22:42.1Z")]
        [TestCase(2000, 12, 04, 05, 22, 42, 100.0000, DateTimeKind.Utc        , "2000-12-04T06:22:42.1+01:00")]
        [TestCase(2000, 12, 04, 06, 22, 42, 120.0000, DateTimeKind.Unspecified, "2000-12-04T06:22:42.12")]
        [TestCase(2000, 12, 04, 05, 22, 42, 120.0000, DateTimeKind.Utc        , "2000-12-04T05:22:42.12Z")]
        [TestCase(2000, 12, 04, 05, 22, 42, 120.0000, DateTimeKind.Utc        , "2000-12-04T06:22:42.12+01:00")]
        [TestCase(2000, 12, 04, 06, 22, 42, 123.0000, DateTimeKind.Unspecified, "2000-12-04T06:22:42.123")]
        [TestCase(2000, 12, 04, 05, 22, 42, 123.0000, DateTimeKind.Utc        , "2000-12-04T05:22:42.123Z")]
        [TestCase(2000, 12, 04, 05, 22, 42, 123.0000, DateTimeKind.Utc        , "2000-12-04T06:22:42.123+01:00")]
        [TestCase(2000, 12, 04, 06, 22, 42, 123.4000, DateTimeKind.Unspecified, "2000-12-04T06:22:42.1234")]
        [TestCase(2000, 12, 04, 05, 22, 42, 123.4000, DateTimeKind.Utc        , "2000-12-04T05:22:42.1234Z")]
        [TestCase(2000, 12, 04, 05, 22, 42, 123.4000, DateTimeKind.Utc        , "2000-12-04T06:22:42.1234+01:00")]
        [TestCase(2000, 12, 04, 06, 22, 42, 123.4500, DateTimeKind.Unspecified, "2000-12-04T06:22:42.12345")]
        [TestCase(2000, 12, 04, 05, 22, 42, 123.4500, DateTimeKind.Utc        , "2000-12-04T05:22:42.12345Z")]
        [TestCase(2000, 12, 04, 05, 22, 42, 123.4500, DateTimeKind.Utc        , "2000-12-04T06:22:42.12345+01:00")]
        [TestCase(2000, 12, 04, 06, 22, 42, 123.4560, DateTimeKind.Unspecified, "2000-12-04T06:22:42.123456")]
        [TestCase(2000, 12, 04, 05, 22, 42, 123.4560, DateTimeKind.Utc        , "2000-12-04T05:22:42.123456Z")]
        [TestCase(2000, 12, 04, 05, 22, 42, 123.4560, DateTimeKind.Utc        , "2000-12-04T06:22:42.123456+01:00")]
        [TestCase(2000, 12, 04, 06, 22, 42, 123.4567, DateTimeKind.Unspecified, "2000-12-04T06:22:42.1234567")]
        [TestCase(2000, 12, 04, 05, 22, 42, 123.4567, DateTimeKind.Utc        , "2000-12-04T05:22:42.1234567Z")]
        [TestCase(2000, 12, 04, 05, 22, 42, 123.4567, DateTimeKind.Utc        , "2000-12-04T06:22:42.1234567+01:00")]
        public void ImportDateTime(int year, int month, int day, int hour, int minute, int second, double ms, DateTimeKind kind, string json)
        {
            var actual = JsonImporters.DateTime.Import("'" + json + "'");
            var expected = new DateTime(year, month, day, hour, minute, second, kind).AddTicks((int) (ms * 1e4));

            Assert.That(actual.Kind, Is.EqualTo(kind == DateTimeKind.Unspecified
                                                ? DateTimeKind.Unspecified
                                                : DateTimeKind.Local));

            Assert.That(actual, Is.EqualTo(expected.Kind == DateTimeKind.Utc
                                           ? expected.ToLocalTime()
                                           : expected));
        }

        [TestCase(2000, 12, 04, 06, 22, 00, 000.0000, 01, 00, "2000-12-04T05:22Z")]
        [TestCase(2000, 12, 04, 06, 22, 00, 000.0000, 01, 00, "2000-12-04T06:22+01:00")]
        [TestCase(2000, 12, 04, 06, 22, 42, 000.0000, 01, 00, "2000-12-04T05:22:42Z")]
        [TestCase(2000, 12, 04, 06, 22, 42, 000.0000, 01, 00, "2000-12-04T06:22:42+01:00")]
        [TestCase(2000, 12, 04, 06, 22, 42, 100.0000, 01, 00, "2000-12-04T05:22:42.1Z")]
        [TestCase(2000, 12, 04, 06, 22, 42, 100.0000, 01, 00, "2000-12-04T06:22:42.1+01:00")]
        [TestCase(2000, 12, 04, 06, 22, 42, 120.0000, 01, 00, "2000-12-04T05:22:42.12Z")]
        [TestCase(2000, 12, 04, 06, 22, 42, 120.0000, 01, 00, "2000-12-04T06:22:42.12+01:00")]
        [TestCase(2000, 12, 04, 06, 22, 42, 123.0000, 01, 00, "2000-12-04T05:22:42.123Z")]
        [TestCase(2000, 12, 04, 06, 22, 42, 123.0000, 01, 00, "2000-12-04T06:22:42.123+01:00")]
        [TestCase(2000, 12, 04, 06, 22, 42, 123.4000, 01, 00, "2000-12-04T05:22:42.1234Z")]
        [TestCase(2000, 12, 04, 06, 22, 42, 123.4000, 01, 00, "2000-12-04T06:22:42.1234+01:00")]
        [TestCase(2000, 12, 04, 06, 22, 42, 123.4500, 01, 00, "2000-12-04T05:22:42.12345Z")]
        [TestCase(2000, 12, 04, 06, 22, 42, 123.4500, 01, 00, "2000-12-04T06:22:42.12345+01:00")]
        [TestCase(2000, 12, 04, 06, 22, 42, 123.4560, 01, 00, "2000-12-04T05:22:42.123456Z")]
        [TestCase(2000, 12, 04, 06, 22, 42, 123.4560, 01, 00, "2000-12-04T06:22:42.123456+01:00")]
        [TestCase(2000, 12, 04, 06, 22, 42, 123.4567, 01, 00, "2000-12-04T05:22:42.1234567Z")]
        [TestCase(2000, 12, 04, 06, 22, 42, 123.4567, 01, 00, "2000-12-04T06:22:42.1234567+01:00")]
        public void ImportDateTimeOffset(int year, int month, int day, int hour, int minute, int second, double ms,
                                         int hoffset, int moffset, string json)
        {
            var actual = JsonImporters.DateTimeOffset.Import("'" + json + "'");
            var expected = new DateTimeOffset(year, month, day, hour, minute, second, new TimeSpan(hoffset, moffset, 0)).AddTicks((int) (ms * 1e4));
            Assert.That(actual, Is.EqualTo(expected));
        }
    }
}