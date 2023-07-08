namespace MemoryMQ;

public enum RetryMode
{
    /// <summary>
    /// 递增 例如设置了2秒，则每次重试的间隔为2秒、4秒、6秒、8秒、10秒
    /// </summary>
    Incremental,
    /// <summary>
    /// 固定 例如设置了2秒，则每次重试的间隔为2秒、2秒、2秒、2秒、2秒
    /// </summary>
    Fixed,
    /// <summary>
    /// 指数 例如设置了2秒，则每次重试的间隔为2秒、4秒、8秒、16秒、32秒
    /// </summary>
    Exponential
}