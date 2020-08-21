namespace Arriba.Caching
{
    public interface IObjectCacheFactory
    {
        IObjectCache CreateCache(string name);
    }
}
