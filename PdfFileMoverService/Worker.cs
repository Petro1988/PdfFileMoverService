namespace PdfFileMoverService
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private FileSystemWatcher _watcher;

        private readonly string watchFolder = @"C:\rechnung";
        private readonly string targetFolder = @"C:\programm";
        private readonly string processedFolder = @"C:\verarbeitet";

        public Worker(ILogger<Worker> logger)
        {
            _logger = logger;
        }

        public override Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Dienst gestartet. Überwache Ordner {watchFolder}", watchFolder);

            _watcher = new FileSystemWatcher(watchFolder, "*.pdf");
            _watcher.Created += OnCreated;
            _watcher.IncludeSubdirectories = false;
            _watcher.EnableRaisingEvents = true;

            return base.StartAsync(cancellationToken);
        }

        private void OnCreated(object sender, FileSystemEventArgs e)
        {
            Task.Run(() =>
            {
                try
                {
                    string fileName = Path.GetFileName(e.FullPath);
                    string targetPath = Path.Combine(targetFolder, fileName);
                    string processedPath = Path.Combine(processedFolder, fileName);

                    _logger.LogInformation("Neue PDF erkannt: {file}", fileName);

                    // Warte, bis Datei bereit ist
                    while (!FileIsReady(e.FullPath))
                    {
                        Thread.Sleep(500);
                    }

                    File.Move(e.FullPath, targetPath);
                    _logger.LogInformation("Verschoben nach: {target}", targetPath);

                    File.Copy(targetPath, processedPath, overwrite: true);
                    _logger.LogInformation("Kopiert nach: {processed}", processedPath);
                }
                catch (Exception ex)
                {
                    _logger.LogError("Fehler bei der Dateibearbeitung: {msg}", ex.Message);
                }
            });
        }

        private bool FileIsReady(string path)
        {
            try
            {
                using (var stream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.None))
                    return true;
            }
            catch
            {
                return false;
            }
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            _watcher.Dispose();
            _logger.LogInformation("Dienst gestoppt");
            return base.StopAsync(cancellationToken);
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            return Task.CompletedTask;
        }
    }
}
