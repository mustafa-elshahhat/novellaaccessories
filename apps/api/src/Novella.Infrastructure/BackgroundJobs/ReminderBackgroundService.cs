using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Novella.Application.Reminders;

namespace Novella.Infrastructure.BackgroundJobs;

/// <summary>
/// Best-effort hosted reminder runner. On shared hosting the app pool may recycle, so this is a
/// convenience only — the admin-protected job endpoint is the reliable trigger. No Redis/queues.
/// The reminder service itself honors enable flags and dedupe, so running this is always safe.
/// </summary>
public sealed class ReminderBackgroundService : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromHours(1);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ReminderBackgroundService> _logger;

    public ReminderBackgroundService(IServiceScopeFactory scopeFactory, ILogger<ReminderBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Small initial delay so startup/migration completes first.
        try { await Task.Delay(TimeSpan.FromMinutes(2), stoppingToken); }
        catch (OperationCanceledException) { return; }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var service = scope.ServiceProvider.GetRequiredService<ReminderService>();
                var result = await service.RunAsync(stoppingToken);
                if (result.AbandonedSent + result.InactiveSent > 0)
                    _logger.LogInformation("Reminder run: abandoned={Abandoned} inactive={Inactive} skipped={Skipped}",
                        result.AbandonedSent, result.InactiveSent, result.Skipped);
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Reminder background run failed");
            }

            try { await Task.Delay(Interval, stoppingToken); }
            catch (OperationCanceledException) { break; }
        }
    }
}
