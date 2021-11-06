﻿using System.Reflection;

namespace Deployf.Botf;

public abstract class BotControllerMap<T> : Dictionary<T, MethodInfo> where T : notnull
{
    public BotControllerMap(IDictionary<T, MethodInfo> data) : base(data)
    {
    }

    public IEnumerable<Type?> ControllerTypes()
    {
        return Values
            .Select(c => c.DeclaringType)
            .Distinct();
    }
}

public class BotControllerRoutes : BotControllerMap<string>
{
    public BotControllerRoutes(IDictionary<string, MethodInfo> data) :base(data)
    {
    }

    public (string? template, MethodInfo? method) FindTemplate(string controller, string action)
    {
        foreach(var item in this)
        {
            if(item.Value.Name == action && item.Value.DeclaringType?.Name == controller)
            {
                return (item.Key, item.Value);
            }
        }

        return (null, null);
    }
}

public class BotControllerStates : BotControllerMap<Type>
{
    public BotControllerStates(IDictionary<Type, MethodInfo> data) : base(data)
    {
    }
}

public class BotControllerHandlers : BotControllerMap<Handle>
{
    public BotControllerHandlers(IDictionary<Handle, MethodInfo> data) : base(data)
    {
    }
}