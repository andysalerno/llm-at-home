namespace AgentFlow.Generic;

public interface IFactory<out T>
{
    T Create();
}

public interface ISelfFactory<out T> : IFactory<T>
{
#pragma warning disable CA1033 // Interface methods should be callable by child types
    T IFactory<T>.Create()
#pragma warning restore CA1033 // Interface methods should be callable by child types
    {
        if (this is T self)
        {
            return self;
        }

        throw new InvalidOperationException("Expected a ISelfFactory to be of type self");
    }
}
