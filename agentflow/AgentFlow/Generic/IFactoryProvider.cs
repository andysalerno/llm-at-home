namespace AgentFlow.Generic;

public interface IFactoryProvider<out TOut, TKey>
{
    IFactory<TOut> GetFactory(TKey name);
}
