using AmazonSellerCentralImagePackager.Logic;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using neXn.Lib;
using neXn.Ui.Animation;
using Serilog.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace AmazonSellerCentralImagePackager.ViewModels
{
    public partial class MainWindowViewModel : ViewModelBase
    {
        private readonly TextWaitingAnimation textWaitingAnimation;
        private readonly List<string> asins = [];
        private CancellationTokenSource busyCts;

        public IReadOnlyList<int> ImageCounts { get; } =
        [
            25,
            50,
            100,
            200,
            250,
            500
        ];

        [ObservableProperty]
        public partial string TitleName { get; set; } = Globals.Assembly.GetCustomAttribute<AssemblyTitleAttribute>()?.Title;

        [ObservableProperty]
        public partial string TitleVersion { get; set; } = $"v{Globals.Assembly.GetName().Version.ToNiceString()}";

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(GeneratePackagesCommand))]
        public partial string OriginalPicturePath { get; set; } = null;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(ClearAsinListCommand))]
        public partial string AsinList { get; set; } = null;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(GeneratePackagesCommand))]
        public partial int AsinCount { get; set; }

        [ObservableProperty]
        public partial ObservableCollection<string> AmazonImageCodes { get; set; } = [.. Constants.amazonImageCodes];

        [ObservableProperty]
        public partial string SelectedAmazonPictureCode { get; set; }

        [ObservableProperty]
        public partial string Status { get; set; } = "Ready";

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(GeneratePackagesCommand))]
        [NotifyCanExecuteChangedFor(nameof(AbortCommand))]
        public partial bool IsBusy { get; set; }

        [ObservableProperty]
        public partial int ProgressBarValue { get; set; } = 100;

        [ObservableProperty]
        public partial int ProgressBarMaximum { get; set; } = 100;

        [ObservableProperty]
        public partial int ImagesPerZip { get; set; } = 200;

        [ObservableProperty]
        public partial bool OpenOutputFolderWhenDone { get; set; }

        partial void OnOpenOutputFolderWhenDoneChanged(bool value)
        {
            Globals.UserConfig.RuntimeConfiguration.OpenOutputFolderWhenDone = value;
            Task.Run(() => Globals.UserConfig.SaveAsync());
        }

        partial void OnSelectedAmazonPictureCodeChanged(string value)
        {
            Globals.UserConfig.RuntimeConfiguration.LastUsedImageCode = value;
            Task.Run(() => Globals.UserConfig.SaveAsync());
        }

        partial void OnImagesPerZipChanged(int value)
        {
            Globals.UserConfig.RuntimeConfiguration.LastUsedImagesPerZip = value;
            Task.Run(() => Globals.UserConfig.SaveAsync());
        }

        partial void OnAsinListChanged(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                this.asins.Clear();
                this.AsinCount = default;
                return;
            }

            this.asins.Clear();
            this.asins.AddRange(value
                .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(x => x.Length == 10 && x.All(char.IsLetterOrDigit))
                .Select(x => x.ToUpperInvariant())
                .Distinct());
            this.AsinCount = this.asins.Count;

            _logger.LogTrace("ASIN list changed, total valid & distinct ASINs: {AsinCount}", this.AsinCount);
        }

        #region Ctor
        public MainWindowViewModel(bool loadUserConfig = true) : base(nameof(MainWindowViewModel))
        {
            Globals.AppStatus.StatusChanged += (s, e) =>
            {
                this.IsBusy = Globals.AppStatus.IsBusy;
            };

            this.textWaitingAnimation = new()
            {
                UseBrackets = true,
                AnimationType = TextWaitingAnimation.AnimationTypes.BlockChars,
                Interval = 400
            };

            if (!Design.IsDesignMode && loadUserConfig)
            {
                this.LoadUserConfigSettingsToViewModel();
            }
            else
            {
                this.IsBusy = true;
                this.ProgressBarValue = 20;
                this.ProgressBarMaximum = 25;
            }

            this.textWaitingAnimation.AnimationChanged += this.TextWaitingAnimation_AnimationChanged;
            this.textWaitingAnimation.Start();
        }
        #endregion

        private void LoadUserConfigSettingsToViewModel()
        {
            this.SelectedAmazonPictureCode = Constants.amazonImageCodes.IndexOf(Globals.UserConfig.RuntimeConfiguration.LastUsedImageCode) != -1 ? Globals.UserConfig.RuntimeConfiguration.LastUsedImageCode : this.AmazonImageCodes[0];
            this.ImagesPerZip = Globals.UserConfig.RuntimeConfiguration.LastUsedImagesPerZip;
            this.OpenOutputFolderWhenDone = Globals.UserConfig.RuntimeConfiguration.OpenOutputFolderWhenDone;
        }

        private void TextWaitingAnimation_AnimationChanged(object sender, string e)
        {
            this.Status = $"{e} {Globals.AppStatus.Status}";
        }

        private bool CanExecuteGeneratePackages()
        {
            if (this.IsBusy)
            {
                return false;
            }

            if (string.IsNullOrEmpty(this.OriginalPicturePath) || !File.Exists(this.OriginalPicturePath))
            {
                return false;
            }

            if (this.asins.Count <= 0)
            {
                return false;
            }

            return true;
        }

        [RelayCommand]
        private async Task Browse()
        {
            IReadOnlyList<IStorageFile> result = await this.Instance.StorageProvider.OpenFilePickerAsync(new()
            {
                Title = "Choose an image",
                AllowMultiple = false,
                SuggestedFileName = "image.png",
                FileTypeFilter = [FilePickerFileTypes.ImageAll]
            });

            if (result.Count <= 0)
            {
                return;
            }

            string filepath = result[0].Path.LocalPath;

            if (!File.Exists(filepath))
            {
                return;
            }

            this.OriginalPicturePath = filepath;
        }

        [RelayCommand(CanExecute = nameof(CanExecuteAbort))]
        private void Abort()
        {
            this.busyCts?.Cancel();
        }
        private bool CanExecuteAbort()
        {
            return this.IsBusy;
        }

        [RelayCommand(CanExecute = nameof(CanExecuteClearAsinList))]
        private void ClearAsinList()
        {
            this.AsinList = null;
        }

        private bool CanExecuteClearAsinList()
        {
            return !string.IsNullOrEmpty(this.AsinList);
        }

        [RelayCommand(CanExecute = nameof(CanExecuteGeneratePackages))]
        private async Task GeneratePackages()
        {
            this.busyCts = new();

            Globals.AppStatus.Change("Processing...", true);
            this.ProgressBarValue = 0;
            this.ProgressBarMaximum = this.asins.Count;

            ILogger l = new SerilogLoggerProvider().CreateLogger("Processor.Packager");

            Processor.Packager p = new(Program.AppLocalBasePath, l);

            p.CurrentPackedFilesCountChanged += (s, e) =>
            {
                this.ProgressBarValue = e;
            };

            if (!await p.PackAsync(this.OriginalPicturePath, this.AsinList.Split('\n'), this.SelectedAmazonPictureCode, this.ImagesPerZip, this.OpenOutputFolderWhenDone, this.busyCts.Token))
            {
                Globals.AppStatus.Change("Aborted", false, true);
                return;
            }

            Globals.AppStatus.Change($"Ready - processed {this.AsinCount} files.", true);

            Globals.AppStatus.SetDefaultStatus();
            this.busyCts?.Dispose();
        }
    }
}
