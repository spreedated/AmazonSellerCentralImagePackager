using AmazonSellerCentralImagePackager.ViewModels;
using NUnit.Framework.Internal;

namespace UnitTests
{
    [TestFixture]
    public class MainWindowViewModelTests
    {
        private string tempDirectory = null!;
        private string imagePath = null!;
        private MainWindowViewModel sut = null!;

        [SetUp]
        public void SetUp()
        {
            this.tempDirectory = Path.Combine(
                Path.GetTempPath(),
                $"AmazonSellerCentralImagePackager_{Guid.NewGuid():N}");

            Directory.CreateDirectory(this.tempDirectory);

            this.imagePath = Path.Combine(
                this.tempDirectory,
                "test.jpg");

            File.WriteAllBytes(
                this.imagePath,
                [0x01, 0x02, 0x03]);

            this.sut = new(false)
            {
                AsinList = null,
                OriginalPicturePath = null,
                IsBusy = false
            };
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(this.tempDirectory))
            {
                Directory.Delete(this.tempDirectory, true);
            }
        }

        [Test]
        public void AsinList_WhenNull_SetsAsinCountToZero()
        {
            this.sut.AsinList = null;

            Assert.That(this.sut.AsinCount, Is.Zero);
        }

        [Test]
        public void AsinList_WhenEmpty_SetsAsinCountToZero()
        {
            this.sut.AsinList = string.Empty;

            Assert.That(this.sut.AsinCount, Is.Zero);
        }

        [Test]
        public void AsinList_WithSingleValidAsin_SetsCountToOne()
        {
            this.sut.AsinList = "B012345678";

            Assert.That(this.sut.AsinCount, Is.EqualTo(1));
        }

        [Test]
        public void AsinList_WithMultipleValidAsins_SetsCorrectCount()
        {
            this.sut.AsinList = """
            B012345678
            B012345679
            B012345680
            """;

            Assert.That(this.sut.AsinCount, Is.EqualTo(3));
        }

        [Test]
        public void AsinList_IgnoresEmptyLines()
        {
            this.sut.AsinList = """
            B012345678


            B012345679

            """;

            Assert.That(this.sut.AsinCount, Is.EqualTo(2));
        }

        [Test]
        public void AsinList_TrimsWhitespace()
        {
            this.sut.AsinList = """
               B012345678
            B012345679
            """;

            Assert.That(this.sut.AsinCount, Is.EqualTo(2));
        }

        [Test]
        public void AsinList_IgnoresValuesShorterThanTenCharacters()
        {
            this.sut.AsinList = """
            B01234567
            B012345678
            """;

            Assert.That(this.sut.AsinCount, Is.EqualTo(1));
        }

        [Test]
        public void AsinList_IgnoresValuesLongerThanTenCharacters()
        {
            this.sut.AsinList = """
            B0123456789
            B012345678
            """;

            Assert.That(this.sut.AsinCount, Is.EqualTo(1));
        }

        [Test]
        public void AsinList_IgnoresNonAlphaNumericValues()
        {
            this.sut.AsinList = """
            B012345678
            B01234-678
            B01234_678
            """;

            Assert.That(this.sut.AsinCount, Is.EqualTo(1));
        }

        [Test]
        public void AsinList_RemovesDuplicates()
        {
            this.sut.AsinList = """
            B012345678
            B012345678
            B012345678
            """;

            Assert.That(this.sut.AsinCount, Is.EqualTo(1));
        }

        [Test]
        public void AsinList_RemovesDuplicatesIgnoringCase()
        {
            this.sut.AsinList = """
            B012345678
            b012345678
            """;

            Assert.That(this.sut.AsinCount, Is.EqualTo(1));
        }

        [Test]
        public void AsinList_WithMixedValidAndInvalidValues_CountsOnlyValidDistinctAsins()
        {
            this.sut.AsinList = """
            B012345678
            invalid
            B012345679
            B012345678
            B01234-680
            b012345680
            """;

            Assert.That(this.sut.AsinCount, Is.EqualTo(3));
        }

        [Test]
        public void ClearAsinListCommand_WhenListIsEmpty_CannotExecute()
        {
            this.sut.AsinList = null;

            bool result =
                this.sut.ClearAsinListCommand.CanExecute(null);

            Assert.That(result, Is.False);
        }

        [Test]
        public void ClearAsinListCommand_WhenListContainsText_CanExecute()
        {
            this.sut.AsinList = "B012345678";

            bool result =
                this.sut.ClearAsinListCommand.CanExecute(null);

            Assert.That(result, Is.True);
        }

        [Test]
        public void ClearAsinListCommand_WhenExecuted_ClearsList()
        {
            this.sut.AsinList = """
            B012345678
            B012345679
            """;

            this.sut.ClearAsinListCommand.Execute(null);

            Assert.That(this.sut.AsinList, Is.Null);
            Assert.That(this.sut.AsinCount, Is.Zero);
        }

        [Test]
        public void GeneratePackagesCommand_WithNoPicture_CannotExecute()
        {
            this.sut.AsinList = "B012345678";
            this.sut.OriginalPicturePath = null;

            bool result =
                this.sut.GeneratePackagesCommand.CanExecute(null);

            Assert.That(result, Is.False);
        }

        [Test]
        public void GeneratePackagesCommand_WithNonExistingPicture_CannotExecute()
        {
            this.sut.AsinList = "B012345678";

            this.sut.OriginalPicturePath =
                Path.Combine(this.tempDirectory, "missing.jpg");

            bool result =
                this.sut.GeneratePackagesCommand.CanExecute(null);

            Assert.That(result, Is.False);
        }

        [Test]
        public void GeneratePackagesCommand_WithNoAsins_CannotExecute()
        {
            this.sut.OriginalPicturePath = this.imagePath;
            this.sut.AsinList = null;

            bool result =
                this.sut.GeneratePackagesCommand.CanExecute(null);

            Assert.That(result, Is.False);
        }

        [Test]
        public void GeneratePackagesCommand_WithInvalidAsins_CannotExecute()
        {
            this.sut.OriginalPicturePath = this.imagePath;

            this.sut.AsinList = """
            invalid
            too-short
            B01234-678
            """;

            bool result =
                this.sut.GeneratePackagesCommand.CanExecute(null);

            Assert.That(result, Is.False);
        }

        [Test]
        public void GeneratePackagesCommand_WithValidPictureAndAsins_CanExecute()
        {
            this.sut.OriginalPicturePath = this.imagePath;
            this.sut.AsinList = "B012345678";

            bool result =
                this.sut.GeneratePackagesCommand.CanExecute(null);

            Assert.That(result, Is.True);
        }

        [Test]
        public void GeneratePackagesCommand_WhenBusy_CannotExecute()
        {
            this.sut.OriginalPicturePath = this.imagePath;
            this.sut.AsinList = "B012345678";
            this.sut.IsBusy = true;

            bool result =
                this.sut.GeneratePackagesCommand.CanExecute(null);

            Assert.That(result, Is.False);
        }

        [Test]
        public void AbortCommand_WhenNotBusy_CannotExecute()
        {
            this.sut.IsBusy = false;

            bool result =
                this.sut.AbortCommand.CanExecute(null);

            Assert.That(result, Is.False);
        }

        [Test]
        public void AbortCommand_WhenBusy_CanExecute()
        {
            this.sut.IsBusy = true;

            bool result =
                this.sut.AbortCommand.CanExecute(null);

            Assert.That(result, Is.True);
        }
    }
}
