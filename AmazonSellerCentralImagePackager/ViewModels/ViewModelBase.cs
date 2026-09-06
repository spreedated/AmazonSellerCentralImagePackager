#pragma warning disable S4487

using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;
using Serilog.Extensions.Logging;

namespace AmazonSellerCentralImagePackager.ViewModels
{
    public abstract partial class ViewModelBase : ObservableObject
    {
        protected readonly ILogger _logger;

        [ObservableProperty]
        public partial Window Instance { get; set; }

        #region Ctor
        protected ViewModelBase(string loggerName)
        {
            _logger = new SerilogLoggerProvider().CreateLogger(loggerName);
        }
        #endregion
    }
}
