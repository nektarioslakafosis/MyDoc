namespace MyDoc.Services
{
    public class AppointmentReminderHostedService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<AppointmentReminderHostedService> _logger;

        public AppointmentReminderHostedService(
            IServiceScopeFactory scopeFactory,
            ILogger<AppointmentReminderHostedService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(
            CancellationToken stoppingToken)
        {
            await Task.Delay(
                TimeSpan.FromSeconds(30),
                stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope =
                        _scopeFactory.CreateScope();

                    
                    var lifecycleService =
                        scope.ServiceProvider
                            .GetRequiredService<
                                AppointmentLifecycleService>();

                    await lifecycleService
                        .ExpirePastAppointmentsAsync();

                    
                    var reminderService =
                        scope.ServiceProvider
                            .GetRequiredService<
                                AppointmentReminderService>();

                    await reminderService
                        .SendDueRemindersAsync();
                }
                catch (TaskCanceledException)
                {
                    // Application is shutting down.
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "Appointment background service failed.");
                }

                await Task.Delay(
                    TimeSpan.FromMinutes(1),
                    stoppingToken);
            }
        }
    }
}