using EfCoreRepository.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ScholarNotify.Models;
using ScholarNotify.Services;
using ScholarNotify.ViewModels;

namespace ScholarNotify.Controllers;

public sealed class HomeController(
    IBasicCrud<Monitor> monitors,
    IBasicCrud<Activity> activities,
    ScholarService scholarService,
    MonitorService monitorService,
    SmsProxyHubService smsService,
    PhoneNumberService phoneNumberService) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index() => View(await CreateViewModelAsync(new MonitorInput()));

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Add([Bind(Prefix = "Input")] MonitorInput input, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View("Index", await CreateViewModelAsync(input));
        }

        try
        {
            input.PhoneNumber = phoneNumberService.NormalizeToE164(input.PhoneNumber);
        }
        catch (ArgumentException exception)
        {
            ModelState.AddModelError("Input.PhoneNumber", exception.Message);
            return View("Index", await CreateViewModelAsync(input));
        }

        try
        {
            var snapshot = await scholarService.FetchProfileAsync(input.ScholarUrl, cancellationToken);
            var now = DateTimeOffset.UtcNow;
            var monitor = new Monitor
            {
                ScholarUrl = snapshot.CanonicalUrl,
                ScholarName = snapshot.Name,
                PhoneNumber = input.PhoneNumber,
                CurrentCitations = snapshot.Citations,
                NotifiedCitations = snapshot.Citations,
                UtcOffsetHours = input.UtcOffsetHours,
                NotificationStartHour = input.NotificationStartHour,
                NotificationEndHour = input.NotificationEndHour,
                Enabled = true,
                LastCheckedAt = now,
                NextCheckAt = now,
                CreatedAt = now
            };
            monitor.NextCheckAt = NotificationWindow.GetNextCheck(monitor, now);
            monitor = await monitors.Save(monitor);
            await activities.Save(new Activity
            {
                MonitorId = monitor.Id,
                Kind = "baseline",
                Message = $"Monitoring started at {snapshot.Citations:N0} citations.",
                CreatedAt = now
            });
            SetFlash($"{snapshot.Name} is now being monitored.", "success");
            return RedirectToAction(nameof(Index));
        }
        catch (DbUpdateException exception) when (exception.InnerException?.Message.Contains("UNIQUE constraint failed", StringComparison.OrdinalIgnoreCase) == true)
        {
            ModelState.AddModelError(string.Empty, "That Scholar profile is already being monitored.");
        }
        catch (Exception exception)
        {
            ModelState.AddModelError(string.Empty, exception.Message);
        }

        return View("Index", await CreateViewModelAsync(input));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Check(long id, CancellationToken cancellationToken)
    {
        try
        {
            await monitorService.CheckAsync(id, cancellationToken);
            SetFlash("Profile checked successfully.", "success");
        }
        catch (Exception exception)
        {
            SetFlash(exception.Message, "error");
        }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Toggle(long id, bool enabled)
    {
        try
        {
            if (await monitors.Update(id, entity => entity.Enabled = enabled) is null)
            {
                throw new InvalidOperationException("Monitor not found.");
            }

            SetFlash(enabled ? "Monitoring resumed." : "Monitoring paused.", "success");
        }
        catch (Exception exception)
        {
            SetFlash(exception.Message, "error");
        }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(long id)
    {
        try
        {
            if (await monitors.Delete(id) is null)
            {
                throw new InvalidOperationException("Monitor not found.");
            }

            SetFlash("Monitor deleted.", "success");
        }
        catch (Exception exception)
        {
            SetFlash(exception.Message, "error");
        }
        return RedirectToAction(nameof(Index));
    }

    private async Task<DashboardViewModel> CreateViewModelAsync(MonitorInput input) => new()
    {
        Monitors = (await monitors.NoTracking().GetAll()).OrderByDescending(item => item.CreatedAt).ToArray(),
        Activities = (await activities.NoTracking().GetAll()).OrderByDescending(item => item.CreatedAt).Take(50).ToArray(),
        SmsConfigured = smsService.IsConfigured,
        Input = input
    };

    private void SetFlash(string message, string kind)
    {
        TempData["FlashMessage"] = message;
        TempData["FlashKind"] = kind;
    }
}