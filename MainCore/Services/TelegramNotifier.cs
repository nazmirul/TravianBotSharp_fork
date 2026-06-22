using MainCore.Notifications;
using Microsoft.Extensions.DependencyInjection;
using System.Net.Http;
using System.Text.Json;

namespace MainCore.Services
{
    /// <summary>
    /// Sends a Telegram message when an account gets paused (ban/crash/stuck), so the bot can run
    /// unattended. Configured via telegram.json next to the exe: { "Enabled": true, "Token": "...",
    /// "ChatId": "..." }. Disabled (and a template written) when the file is missing.
    /// </summary>
    [RegisterSingleton<TelegramNotifier>]
    public sealed class TelegramNotifier
    {
        private static readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(15) };

        private readonly IRxQueue _rxQueue;
        private readonly ICustomServiceScopeFactory _scopeFactory;
        private readonly ILogger _logger;

        private bool _enabled;
        private string _token = "";
        private string _chatId = "";

        public TelegramNotifier(IRxQueue rxQueue, ICustomServiceScopeFactory scopeFactory, ILogger logger)
        {
            _rxQueue = rxQueue;
            _scopeFactory = scopeFactory;
            _logger = logger.ForContext<TelegramNotifier>();
        }

        public void Activate()
        {
            LoadConfig();
            if (!_enabled || string.IsNullOrWhiteSpace(_token) || string.IsNullOrWhiteSpace(_chatId)) return;

            _rxQueue.RegisterHandler<StatusModified>(OnStatusModified);
            _logger.Information("Telegram notifier active");
        }

        private void LoadConfig()
        {
            var path = Path.Combine(AppContext.BaseDirectory, "telegram.json");
            try
            {
                if (!File.Exists(path))
                {
                    File.WriteAllText(path, JsonSerializer.Serialize(new TelegramConfig(), new JsonSerializerOptions { WriteIndented = true }));
                    return;
                }
                var cfg = JsonSerializer.Deserialize<TelegramConfig>(File.ReadAllText(path));
                if (cfg is null) return;
                _enabled = cfg.Enabled;
                _token = cfg.Token;
                _chatId = cfg.ChatId;
            }
            catch
            {
                // Malformed config - stay disabled.
            }
        }

        private void OnStatusModified(StatusModified notification)
        {
            if (notification.Status != StatusEnums.Paused) return;
            var name = GetAccountName(notification.AccountId);
            _ = Send($"⚠️ TravianBotSharp: account [{name}] is PAUSED - needs attention.");
        }

        private string GetAccountName(AccountId accountId)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                return context.Accounts
                    .Where(x => x.Id == accountId.Value)
                    .Select(x => $"{x.Username} - {x.Server}")
                    .FirstOrDefault() ?? accountId.Value.ToString();
            }
            catch
            {
                return accountId.Value.ToString();
            }
        }

        private async Task Send(string text)
        {
            try
            {
                var url = $"https://api.telegram.org/bot{_token}/sendMessage?chat_id={Uri.EscapeDataString(_chatId)}&text={Uri.EscapeDataString(text)}";
                await _http.GetAsync(url);
            }
            catch (Exception ex)
            {
                _logger.Warning("Telegram send failed: {Message}", ex.Message);
            }
        }

        private sealed class TelegramConfig
        {
            public bool Enabled { get; set; }
            public string Token { get; set; } = "";
            public string ChatId { get; set; } = "";
        }
    }
}
