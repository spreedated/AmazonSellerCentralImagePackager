using Processor;
using System.IO.Compression;

namespace UnitTests
{
    [TestFixture]
    public sealed class PackagerTests
    {
        private string basePath;
        private string sourceImagePath;

        [SetUp]
        public void Setup()
        {
            this.basePath = Path.Combine(Path.GetTempPath(), $"AmazonPicturePackagerTests_{Guid.NewGuid():N}");

            Directory.CreateDirectory(this.basePath);

            this.sourceImagePath = Path.Combine(this.basePath, "source.jpg");

            File.WriteAllBytes(this.sourceImagePath, [0x01, 0x02, 0x03, 0x04]);
        }

        [Test]
        public void PackAsync_WithZeroFilesPerZip_ThrowsArgumentOutOfRangeException()
        {
            Packager sut = new(this.basePath);

            Assert.That(
                async () => await sut.PackAsync(
                    this.sourceImagePath,
                    ["B012345678"],
                    "MAIN",
                    0,
                    false,
                    CancellationToken.None),
                Throws.TypeOf<ArgumentOutOfRangeException>());
        }

        [Test]
        public void Constructor_WithValidBasePath_SetsBasePath()
        {
            Packager sut = new(this.basePath);

            Assert.That(sut.BasePath, Is.EqualTo(this.basePath));
        }

        [Test]
        public void Constructor_WithNullBasePath_ThrowsFileNotFoundException()
        {
            FileNotFoundException? exception = Assert.Throws<FileNotFoundException>(
                () => new Packager(null!));

            Assert.That(exception, Is.Not.Null);
            Assert.That(exception!.Message, Does.Contain("Invalid BasePath"));
        }

        [Test]
        public void Constructor_WithEmptyBasePath_ThrowsFileNotFoundException()
        {
            FileNotFoundException? exception = Assert.Throws<FileNotFoundException>(
                () => new Packager(string.Empty));

            Assert.That(exception, Is.Not.Null);
            Assert.That(exception!.Message, Does.Contain("Invalid BasePath"));
        }

        [Test]
        public void Constructor_WithNonExistingBasePath_ThrowsFileNotFoundException()
        {
            string invalidPath = Path.Combine(
                this.basePath,
                "does-not-exist");

            FileNotFoundException? exception = Assert.Throws<FileNotFoundException>(
                () => new Packager(invalidPath));

            Assert.That(exception, Is.Not.Null);
            Assert.That(exception!.Message, Does.Contain("Invalid BasePath"));
        }

        [Test]
        public void PackAsync_WithMissingSourceImage_ThrowsFileNotFoundException()
        {
            Packager sut = new(this.basePath);

            string invalidImagePath = Path.Combine(
                this.basePath,
                "does-not-exist.jpg");

            FileNotFoundException? exception =
                Assert.ThrowsAsync<FileNotFoundException>(async () =>
                    await sut.PackAsync(
                        invalidImagePath,
                        ["B012345678"],
                        "MAIN",
                        50,
                        false,
                        CancellationToken.None));

            Assert.That(exception, Is.Not.Null);
            Assert.That(exception!.Message, Does.Contain("Invalid Picturepath"));
        }

        [Test]
        public async Task PackAsync_WithSingleAsin_CreatesZipFile()
        {
            Packager sut = new(this.basePath);

            bool result = await sut.PackAsync(
                this.sourceImagePath,
                ["B012345678"],
                "MAIN",
                50,
                false,
                CancellationToken.None);

            string zipPath = Path.Combine(
                this.basePath,
                "output",
                "pack001.zip");

            Assert.That(result, Is.True);
            Assert.That(File.Exists(zipPath), Is.True);
        }

        [Test]
        public async Task PackAsync_CreatesCorrectAmazonImageFilename()
        {
            Packager sut = new(this.basePath);

            await sut.PackAsync(
                this.sourceImagePath,
                ["B012345678"],
                "MAIN",
                50,
                false,
                CancellationToken.None);

            string zipPath = Path.Combine(
                this.basePath,
                "output",
                "pack001.zip");

            using ZipArchive archive = ZipFile.OpenRead(zipPath);

            Assert.That(archive.Entries, Has.Count.EqualTo(1));
            Assert.That(
                archive.Entries[0].Name,
                Is.EqualTo("B012345678.MAIN.jpg"));
        }

        [Test]
        public async Task PackAsync_ConvertsAsinToUppercase()
        {
            Packager sut = new(this.basePath);

            await sut.PackAsync(
                this.sourceImagePath,
                ["b012345678"],
                "MAIN",
                50,
                false,
                CancellationToken.None);

            string zipPath = Path.Combine(
                this.basePath,
                "output",
                "pack001.zip");

            using ZipArchive archive = ZipFile.OpenRead(zipPath);

            Assert.That(
                archive.Entries[0].Name,
                Is.EqualTo("B012345678.MAIN.jpg"));
        }

        [Test]
        public async Task PackAsync_RemovesNewLineCharactersFromAsin()
        {
            Packager sut = new(this.basePath);

            await sut.PackAsync(
                this.sourceImagePath,
                ["B012345678\r\n"],
                "MAIN",
                50,
                false,
                CancellationToken.None);

            string zipPath = Path.Combine(
                this.basePath,
                "output",
                "pack001.zip");

            using ZipArchive archive = ZipFile.OpenRead(zipPath);

            Assert.That(
                archive.Entries[0].Name,
                Is.EqualTo("B012345678.MAIN.jpg"));
        }

        [Test]
        public async Task PackAsync_WithMultipleAsins_CreatesEntryForEveryAsin()
        {
            Packager sut = new(this.basePath);

            string[] asins =
            [
                "B012345678",
            "B012345679",
            "B012345680"
            ];

            await sut.PackAsync(
                this.sourceImagePath,
                asins,
                "MAIN",
                50,
                false,
                CancellationToken.None);

            string zipPath = Path.Combine(
                this.basePath,
                "output",
                "pack001.zip");

            using ZipArchive archive = ZipFile.OpenRead(zipPath);

            Assert.That(archive.Entries, Has.Count.EqualTo(3));

            Assert.That(
                archive.Entries.Select(x => x.Name),
                Is.EquivalentTo(new[]
                {
                "B012345678.MAIN.jpg",
                "B012345679.MAIN.jpg",
                "B012345680.MAIN.jpg"
                }));
        }

        [Test]
        public async Task PackAsync_WithFilesPerZip_CreatesMultipleZipFiles()
        {
            Packager sut = new(this.basePath);

            string[] asins =
            [
                "B012345670",
            "B012345671",
            "B012345672",
            "B012345673",
            "B012345674"
            ];

            await sut.PackAsync(
                this.sourceImagePath,
                asins,
                "MAIN",
                2,
                false,
                CancellationToken.None);

            string outputPath = Path.Combine(
                this.basePath,
                "output");

            string[] zipFiles = Directory.GetFiles(
                outputPath,
                "*.zip");

            Assert.That(zipFiles, Has.Length.EqualTo(3));
        }

        [Test]
        public async Task PackAsync_WithFilesPerZip_SplitsEntriesCorrectly()
        {
            Packager sut = new(this.basePath);

            string[] asins =
            [
                "B012345670",
            "B012345671",
            "B012345672",
            "B012345673",
            "B012345674"
            ];

            await sut.PackAsync(
                this.sourceImagePath,
                asins,
                "MAIN",
                2,
                false,
                CancellationToken.None);

            string outputPath = Path.Combine(
                this.basePath,
                "output");

            using ZipArchive first =
                ZipFile.OpenRead(Path.Combine(outputPath, "pack001.zip"));

            using ZipArchive second =
                ZipFile.OpenRead(Path.Combine(outputPath, "pack002.zip"));

            using ZipArchive third =
                ZipFile.OpenRead(Path.Combine(outputPath, "pack003.zip"));

            Assert.That(first.Entries, Has.Count.EqualTo(2));
            Assert.That(second.Entries, Has.Count.EqualTo(2));
            Assert.That(third.Entries, Has.Count.EqualTo(1));
        }

        [Test]
        public async Task PackAsync_RaisesProgressEventForEveryPackedFile()
        {
            Packager sut = new(this.basePath);

            List<int> progressValues = [];

            sut.CurrentPackedFilesCountChanged += (_, value) =>
                progressValues.Add(value);

            string[] asins =
            [
                "B012345670",
            "B012345671",
            "B012345672"
            ];

            await sut.PackAsync(
                this.sourceImagePath,
                asins,
                "MAIN",
                50,
                false,
                CancellationToken.None);

            Assert.That(
                progressValues,
                Is.EqualTo(new[] { 1, 2, 3 }));
        }

        [Test]
        public async Task PackAsync_AfterSuccessfulPacking_RemovesTempDirectory()
        {
            Packager sut = new(this.basePath);

            await sut.PackAsync(
                this.sourceImagePath,
                ["B012345678"],
                "MAIN",
                50,
                false,
                CancellationToken.None);

            string tempPath = Path.Combine(
                this.basePath,
                "temp");

            Assert.That(Directory.Exists(tempPath), Is.False);
        }

        [Test]
        public async Task PackAsync_WhenOutputZipAlreadyExists_UsesNextAvailableFilename()
        {
            string outputPath = Path.Combine(
                this.basePath,
                "output");

            Directory.CreateDirectory(outputPath);

            string existingZip = Path.Combine(
                outputPath,
                "pack001.zip");

            using (ZipArchive _ = ZipFile.Open(
                       existingZip,
                       ZipArchiveMode.Create))
            {
            }

            Packager sut = new(this.basePath);

            await sut.PackAsync(
                this.sourceImagePath,
                ["B012345678"],
                "MAIN",
                50,
                false,
                CancellationToken.None);

            Assert.That(
                File.Exists(Path.Combine(outputPath, "pack001.zip")),
                Is.True);

            Assert.That(
                File.Exists(Path.Combine(outputPath, "pack002.zip")),
                Is.True);
        }

        [TestCase("MAIN")]
        [TestCase("PT01")]
        [TestCase("PT02")]
        [TestCase("SWCH")]
        public async Task PackAsync_UsesSpecifiedImageVariant(string imageVariant)
        {
            Packager sut = new(this.basePath);

            await sut.PackAsync(
                this.sourceImagePath,
                ["B012345678"],
                imageVariant,
                50,
                false,
                CancellationToken.None);

            string zipPath = Path.Combine(
                this.basePath,
                "output",
                "pack001.zip");

            using ZipArchive archive = ZipFile.OpenRead(zipPath);

            Assert.That(
                archive.Entries[0].Name,
                Is.EqualTo($"B012345678.{imageVariant}.jpg"));
        }

        [TestCase(".jpg")]
        [TestCase(".png")]
        [TestCase(".webp")]
        public async Task PackAsync_PreservesSourceFileExtension(string extension)
        {
            string imagePath = Path.Combine(
                this.basePath,
                $"source{extension}");

            File.WriteAllBytes(
                imagePath,
                [0x01, 0x02, 0x03]);

            Packager sut = new(this.basePath);

            await sut.PackAsync(
                imagePath,
                ["B012345678"],
                "MAIN",
                50,
                false,
                CancellationToken.None);

            string zipPath = Path.Combine(
                this.basePath,
                "output",
                "pack001.zip");

            using ZipArchive archive = ZipFile.OpenRead(zipPath);

            Assert.That(
                archive.Entries[0].Name,
                Is.EqualTo($"B012345678.MAIN{extension}"));
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(this.basePath))
            {
                Directory.Delete(this.basePath, true);
            }
        }
    }
}
