namespace BOTGC.API.Services.EventBus;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class DetectorScheduleAttribute(string cron, bool runOnStartup = false) : Attribute
{
    public string Cron { get; } = cron ?? throw new ArgumentNullException(nameof(cron));
    public bool RunOnStartup { get; } = runOnStartup;
}

[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class EventTypeAttribute : Attribute
{
    public EventTypeAttribute(string name, int version = 1)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Event name must be provided.", nameof(name));
        }

        if (version < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(version), "Version must be >= 1.");
        }

        Name = name;
        Version = version;
    }

    public string Name { get; }
    public int Version { get; }
}

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class SubscriberNameAttribute(string name) : Attribute
{
    public string Name { get; } = name ?? throw new ArgumentNullException(nameof(name));
}
