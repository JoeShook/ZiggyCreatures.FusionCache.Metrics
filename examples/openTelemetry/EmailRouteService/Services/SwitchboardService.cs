using System.Diagnostics;
using System.Text.Json;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Primitives;
using Services;
using Services.Model;

namespace DomainService.Services
{
    public class SwitchboardService : BackgroundService
    {
        private static IFileProvider _fileProvider;
        private string _switchboardFile;
        private string _switchboardFullName;
        private EmailRouteServiceConfig? _emailRouteServiceConfig;
        private ILogger<SwitchboardService>? _logger;
        private byte[] _switchboardFileHash;

        public SwitchboardService(string switchboardPath, EmailRouteServiceConfig emailRouteServiceConfig, ILogger<SwitchboardService>? logger)
        {
            if (!File.Exists(switchboardPath))
            {
                throw new ArgumentException("File does not exist {path}", switchboardPath);
            }

            _emailRouteServiceConfig = emailRouteServiceConfig;
            
            var fileInfo = new FileInfo(switchboardPath);
            _switchboardFullName = fileInfo.FullName;
            _switchboardFile = fileInfo.Name;
            var directory = fileInfo.Directory?.FullName;
            _emailRouteServiceConfig = LoadData();
            _logger = logger;

            // 
            // Some file systems, such as Docker containers and network shares, may not reliably send
            // change notifications. Set the DOTNET_USE_POLLING_FILE_WATCHER environment variable to
            // 1 or true to poll the file system for changes every four seconds (not configurable).
            //
            
            _fileProvider = new PhysicalFileProvider(directory);

            _switchboardFileHash = FileHelpers.ComputeHash(switchboardPath);
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
                    _logger?.LogError(ex, "Call Back Error");
                }
            }
        }

        private async Task DataChange()
        {
            IChangeToken domainChangeToken = _fileProvider.Watch(_switchboardFile);
            var tcs = new TaskCompletionSource<object>(_switchboardFullName);
            domainChangeToken.RegisterChangeCallback(ReloadData, tcs);
            await tcs.Task.ConfigureAwait(false);            
        }

        public void ReloadData(object state)
        {
            var asyncState = ((TaskCompletionSource<object>)state).Task.AsyncState;
            var path = asyncState as string;
            ((TaskCompletionSource<object>)state).TrySetResult(null);

            if (path == null)
            {
                _logger?.LogInformation("File not found: {fileName}", path);
                return;
            }

            var switchboardFileHash = FileHelpers.ComputeHash(path);
            if (!_switchboardFileHash.SequenceEqual(switchboardFileHash))
            {
                _logger?.LogInformation("{fileName} File Changed", path);
                _switchboardFileHash = switchboardFileHash;
                _emailRouteServiceConfig = LoadData();
            }
        }

        private EmailRouteServiceConfig? LoadData()
        {
            var switchBoard = JsonSerializer.Deserialize<Switchboard>(File.ReadAllText(_switchboardFullName));
            return switchBoard?.EmailRouteServiceConfig;
        }
    }
}
