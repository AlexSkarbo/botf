﻿using Deployf.Botf;
using SQLite;

public class ScheduleFilter : PageFilter
{
    public State? State { get; set; }
    public int? OwnerId { get; set; }
    public int? UserId { get; set; }
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
}

public record CreateScheduleParams(DateTime From, DateTime To, int SlotLength);

public class ScheduleService
{
    readonly TableQuery<Schedule> _repo;
    readonly TableQuery<User> _users;
    readonly SQLiteConnection _db;
    readonly PagingService _paging;
    readonly MessageSender _sender;

    public ScheduleService(TableQuery<Schedule> repo, SQLiteConnection db, PagingService paging, MessageSender sender, TableQuery<User> users)
    {
        _repo = repo;
        _db = db;
        _paging = paging;
        _sender = sender;
        _users = users;
    }

    public Paging<Schedule> Get(ScheduleFilter filter)
    {
        var query = _repo.AsQueryable();

        if(filter.State != null)
        {
            query = query.Where(c => c.State == filter.State);
        }

        if(filter.OwnerId != null)
        {
            query = query.Where(c => c.OwnerId == filter.OwnerId);
        }

        if (filter.UserId != null)
        {
            query = query.Where(c => c.UserId == filter.UserId);
        }

        if(filter.From != null)
        {
            query = query.Where(c => c.From > filter.From);
        }

        if (filter.To != null)
        {
            query = query.Where(c => c.To < filter.To);
        }

        return _paging.Paging(query, filter);
    }

    public Schedule Get(int id)
    {
        return _repo.FirstOrDefault(c => c.Id == id);
    }

    public async ValueTask Update(Schedule schedule)
    {
        _db.Update(schedule);

        if (schedule.UserId != null)
        {
            var msg = new MessageBuilder()
                .SetChatId(schedule.UserId.Value)
                .Push("Schedule has changed"); //todo: replace text
            await _sender.Send(msg);
        }
    }

    public async ValueTask<IEnumerable<Schedule>> Add(long ownerId, CreateScheduleParams args)
    {
        var models = new List<Schedule>();

        for(var start = args.From; start <= args.To; start = start.AddMinutes(args.SlotLength))
        {
            models.Add(new Schedule
            {
                From = start,
                To = start.AddMinutes(args.SlotLength),
                OwnerId = ownerId,
                State = State.Free
            });
        }

        _db.InsertAll(models);

        return models;
    }

    public async ValueTask<Schedule> Book(int scheduleId)
    {
        var model = _repo.First(c => c.Id == scheduleId);
        model.State = State.Requested;
        _db.Update(model);

        if (model.OwnerId != null)
        {
            var msg = new MessageBuilder()
                .SetChatId(model.OwnerId)
                .Push("Book requested"); //todo: replace text
            await _sender.Send(msg);
        }

        return model;
    }

    public async ValueTask<Schedule> Reject(int scheduleId)
    {
        var model = _repo.First(c => c.Id == scheduleId);
        model.State = State.Free;
        _db.Update(model);

        if (model.UserId != null)
        {
            var msg = new MessageBuilder()
                .SetChatId(model.UserId.Value)
                .Push("Your booking was rejected"); //todo: replace text
            await _sender.Send(msg);
        }

        return model;
    }

    public async ValueTask<Schedule> Approve(int scheduleId)
    {
        var model = _repo.First(c => c.Id == scheduleId);
        model.State = State.Booked;
        _db.Update(model);

        if (model.UserId != null)
        {
            var msg = new MessageBuilder()
                .SetChatId(model.UserId.Value)
                .Push("Your booking was approved"); //todo: replace text
            await _sender.Send(msg);
        }

        return model;
    }

    public async ValueTask<Schedule> Cancel(int scheduleId)
    {
        var model = _repo.First(c => c.Id == scheduleId);
        model.State = State.Canceled;
        _db.Update(model);

        if (model.UserId != null)
        {
            var msg = new MessageBuilder()
                .SetChatId(model.UserId.Value)
                .Push("Your booking was canceled"); //todo: replace text
            await _sender.Send(msg);
        }
        return model;
    }

    public async ValueTask<Paging<Schedule>> GetFreeSlots(long userId, DateTime day, PageFilter page)
    {
        var query = _repo.Where(c => c.OwnerId == userId && c.From >= day && c.State == State.Free)
            .AsQueryable();
        return _paging.Paging(query, page);
    }

    public async ValueTask<Paging<DateTime>> GetFreeDays(long userId, DateTime day, PageFilter page)
    {
        var query = _repo.Where(c => c.OwnerId == userId && c.From >= day && c.State == State.Free)
            .DistinctBy(c => c.From.Date)
            .Select(c => c.From.Date)
            .AsQueryable();
        return _paging.Paging(query, page);
    }

    public async ValueTask<Paging<User>> GetSchedulers(PageFilter filer)
    {
        var query = _users.Where(c => (c.Roles & UserRole.scheduler) == UserRole.scheduler).AsQueryable();
        return _paging.Paging(query, filer);
    }
}