namespace DeJson.Tests
{
    using NUnit.Framework;

    [TestFixture]
    public class ImporterTests
    {
        [Test]
        public void ToArrayImporter()
        {
            var importer = JsonImporters.Int32.ToArrayImporter();
            var result = importer.Import("[123, 456, 789]");
            Assert.That(result, Is.EquivalentTo(new[] { 123, 456, 789 }));
        }
    }
}