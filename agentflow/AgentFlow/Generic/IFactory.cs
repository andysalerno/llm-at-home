namespace AgentFlow.Generic;

public interface IFactory<out T>
{
    T Create();
}
