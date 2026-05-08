namespace DoAnSE104.Services
{
    public class EmailReminderBackgroundService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailReminderBackgroundService> _logger;

        public EmailReminderBackgroundService(
            IServiceScopeFactory scopeFactory,
            IConfiguration configuration,
            ILogger<EmailReminderBackgroundService> logger)
        {
            _scopeFactory = scopeFactory;
            _configuration = configuration;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (!(_configuration.GetValue<bool?>("EmailReminder:Enabled") ?? true))
            {
                _logger.LogInformation("Email reminder background service is disabled.");
                return;
            }

            await RunOnce(stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(GetDelayUntilNextRun(), stoppingToken);
                    await RunOnce(stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Lỗi chạy email reminder background service.");
                    await Task.Delay(TimeSpan.FromMinutes(30), stoppingToken);
                }
            }
        }

        private async Task RunOnce(CancellationToken stoppingToken)
        {
            using var scope = _scopeFactory.CreateScope();
            var service = scope.ServiceProvider.GetRequiredService<INotificationEmailService>();
            var today = DateTime.Today;

            _logger.LogInformation("Bắt đầu quét email nhắc việc ngày {Date}", today);
            await service.GuiNhacHoaDonChuaThanhToanAsync(today);
            await service.GuiNhacHopDongSapHetHanAsync(today);
            await service.GuiNhacDichVuSapResetAsync(today);
            _logger.LogInformation("Hoàn tất quét email nhắc việc ngày {Date}", today);
        }

        private TimeSpan GetDelayUntilNextRun()
        {
            var runAt = _configuration["EmailReminder:DailyRunAt"] ?? "08:00";
            if (!TimeSpan.TryParse(runAt, out var timeOfDay))
                timeOfDay = new TimeSpan(8, 0, 0);

            var now = DateTime.Now;
            var next = now.Date.Add(timeOfDay);
            if (next <= now)
                next = next.AddDays(1);

            return next - now;
        }
    }
}
