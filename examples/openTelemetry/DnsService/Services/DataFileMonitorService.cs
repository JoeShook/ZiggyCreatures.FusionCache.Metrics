using FusionCache.Example.Domain.Model;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Primitives;
using Services;

namespace DnsService.Services
{
    public class DataFileMonitorService : BackgroundService
    {
        private static IFileProvider _fileProvider;
        private IDataManager _dataManager;
        private ILogger<DataFileMonitorService> _logger;
        private byte[] _emailsFileHash;

        public DataFileMonitorService(IDataManager dataManager, ILogger<DataFileMonitorService> logger)
        {
            _dataManager = dataManager;
            _logger = logger;
            // 
            // Some file systems, such as Docker containers and network shares, may not reliably send
            // change notifications. Set the DOTNET_USE_POLLING_FILE_WATCHER environment variable to
            // 1 or true to poll the file system for changes every four seconds (not configurable).
            //
            _fileProvider = new PhysicalFileProvider(Directory.GetCurrentDirectory());
            _emailsFileHash = FileHelpers.ComputeHash(dataManager.EmailToIpDataPath);
        }

        /// <summary>
        /// This method is called when the <see cref="T:Microsoft.Extensions.Hosting.IHostedService" /> starts.
        /// The implementation should return a task that represents
        /// the lifetime of the long running operation(s) being performed.
        /// </summary>
        /// <param name="stoppingToken">Triggered when <see cref="M:Microsoft.Extensions.Hosting.IHostedService.StopAsync(System.Threading.CancellationToken)" /> is called.</param>
        /// <returns>A <see cref="T:System.Threading.Tasks.Task" /> that represents the long running operations.</returns>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await DataChange();
                    await Task.Delay(100, stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Call Back Error");
                }
            }
        }

        private async Task DataChange()
        {
            IChangeToken emailChangeToken = _fileProvider.Watch(_dataManager.EmailToIpDataPath);
            var tcs = new TaskCompletionSource<object>(_dataManager.EmailToIpDataPath);
            emailChangeToken.RegisterChangeCallback(ReloadData, tcs);
            await tcs.Task.ConfigureAwait(false);            
        }

        public void ReloadData(object state)
        {
            var asyncState = ((TaskCompletionSource<object>)state).Task.AsyncState;
            var path = asyncState as string;
            ((TaskCompletionSource<object>)state).TrySetResult(null);

            if (path == null)
            {
                _logger.LogInformation("File not found: {fileName}", path);
                return;
            }

            var emailsFileHash = FileHelpers.ComputeHash(path);
            if (!_emailsFileHash.SequenceEqual(emailsFileHash))
            {
                _logger.LogInformation("{fileName} File Changed", path);
                _emailsFileHash = emailsFileHash;
                _dataManager.LoadData<EmailToIpData>(path);
            }
        }
    }
}
