namespace AgentFlow.Generic;

public interface IFactory<out T>
{
    T Create();
}

public interface ISelfFactory<out T> : IFactory<T>
{
    T IFactory<T>.Create()
    {
        if (this is T self)
        {
            return self;
        }

        throw new InvalidOperationException("Expected a ISelfFactory to be of type self");
    }

}