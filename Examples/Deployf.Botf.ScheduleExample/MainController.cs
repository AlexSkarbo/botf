﻿using SQLite;
using Telegram.Bot.Types.Enums;

namespace Deployf.Botf.ScheduleExample;

class MainController : BotControllerBase
{
    readonly TableQuery<User> _users;
    readonly SQLiteConnection _db;
    readonly ILogger<MainController> _logger;
    readonly BotfOptions _options;

    public MainController(TableQuery<User> users, SQLiteConnection db, ILogger<MainController> logger, BotfOptions options)
    {
        _users = users;
        _db = db;
        _logger = logger;
        _options = options;
    }


    [Action("/start", "start the bot")]
    public void Start()
    {
        var model = new { link = $"https://t.me/{_options.Username}?start={FromId.Base64()}" };
        View("view.html", model);
    }


    // if user sent unknown action, say it to them
    [On(Handle.Unknown)]
    public void Unknown()
    {
        Push("Unknown command. Or use /start command");
    }

    // handle all messages before botf has processed it
    // and yes, action methods can be void
    [On(Handle.BeforeAll)]
    public void PreHandle()
    {
        // if user has never contacted with the bot we add them to our db at first time
        if(!_users.Any(c => c.Id == FromId))
        {
            var user = new User
            {
                Id = FromId,
                FullName = Context!.GetUserFullName(),
                Username = Context!.GetUsername()!,
                Roles = UserRole.scheduler
            };
            _db.Insert(user);
            _logger.LogInformation("Added user {tgId} at first time", user.Id);
        }
    }

    // handle all errors while message are processing
    [On(Handle.Exception)]
    public async Task OnException(Exception e)
    {
        _logger.LogError(e, "Unhandled exception");
        if (Context.Update.Type == UpdateType.CallbackQuery)
        {
            await AnswerCallback("Error");
        }
        else if (Context.Update.Type == UpdateType.Message)
        {
            Push("Error");
        }
    }

    // we'll handle auth error if user without roles try use action marked with [Authorize("policy")]
    [On(Handle.Unauthorized)]
    public void Forbidden()
    {
        Push("Forbidden!");
    }
}