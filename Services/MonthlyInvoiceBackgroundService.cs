namespace DoAnSE104.Services
{
    public class MonthlyInvoiceBackgroundService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IConfiguration _configuration;
        private readonly ILogger<MonthlyInvoiceBackgroundService> _logger;
        private string? _lastProcessedPeriod;

        public MonthlyInvoiceBackgroundService(
            IServiceScopeFactory scopeFactory,
            IConfiguration configuration,
            ILogger<MonthlyInvoiceBackgroundService> logger)
        {
            _scopeFactory = scopeFactory;
            _configuration = configuration;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (!(_configuration.GetValue<bool?>("MonthlyInvoiceAuto:Enabled") ?? true))
            {
                _logger.LogInformation("Monthly invoice background service is disabled.");
                return;
            }

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var now = DateTime.Now;
                    if (ShouldRun(now))
                    {
                        await RunOnce(now.Date, stoppingToken);
                    }

                    await Task.Delay(GetDelayUntilNextRun(DateTime.Now), stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Loi chay monthly invoice background service.");
                    await Task.Delay(TimeSpan.FromMinutes(30), stoppingToken);
                }
            }
        }

        private bool ShouldRun(DateTime now)
        {
            var today = now.Date;
            var lastDayOfMonth = DateTime.DaysInMonth(today.Year, today.Month);
            if (today.Day != lastDayOfMonth)
                return false;

            var runAt = GetRunAt();
            if (now.TimeOfDay < runAt)
                return false;

            var period = today.ToString("yyyy-MM");
            return _lastProcessedPeriod != period;
        }

        private async Task RunOnce(DateTime today, CancellationToken stoppingToken)
        {
            var period = today.ToString("yyyy-MM");

            using var scope = _scopeFactory.CreateScope();
            var resetService = scope.ServiceProvider.GetRequiredService<IRentalPeriodResetService>();
            var invoiceService = scope.ServiceProvider.GetRequiredService<IMonthlyInvoiceService>();

            _logger.LogInformation("Bat dau tu dong tao hoa don hang thang ky {Period}", period);

            await resetService.ChotKyThueAsync(mocThoiGian: today);
            stoppingToken.ThrowIfCancellationRequested();

            var result = await invoiceService.TaoHoaDonHangThangAsync(period, ngayLap: today);
            _lastProcessedPeriod = period;

            _logger.LogInformation(
                "Hoan tat tao hoa don hang thang ky {Period}: tao {Created}, bo qua {Skipped}, canh bao {Warnings}",
                period,
                result.SoHoaDonDaTao,
                result.SoHoaDonBoQua,
                result.CanhBao.Count);
        }

        private TimeSpan GetRunAt()
        {
            var runAt = _configuration["MonthlyInvoiceAuto:RunAt"] ?? "23:55";
            return TimeSpan.TryParse(runAt, out var timeOfDay)
                ? timeOfDay
                : new TimeSpan(23, 55, 0);
        }

        private TimeSpan GetDelayUntilNextRun(DateTime now)
        {
            var nextRun = GetNextRunTime(now);
            var delay = nextRun - now;
            return delay <= TimeSpan.Zero ? TimeSpan.FromMinutes(5) : delay;
        }

        private DateTime GetNextRunTime(DateTime now)
        {
            var runAt = GetRunAt();
            var today = now.Date;
            var lastDayOfCurrentMonth = DateTime.DaysInMonth(today.Year, today.Month);
            var currentMonthRun = new DateTime(today.Year, today.Month, lastDayOfCurrentMonth).Add(runAt);

            if (currentMonthRun > now)
                return currentMonthRun;

            var nextMonth = today.AddMonths(1);
            var lastDayOfNextMonth = DateTime.DaysInMonth(nextMonth.Year, nextMonth.Month);
            return new DateTime(nextMonth.Year, nextMonth.Month, lastDayOfNextMonth).Add(runAt);
        }
    }
}
