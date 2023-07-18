using System.Reflection;
using BenchmarkDotNet.Attributes;
using MemoryMQ.Dispatcher;
using MemoryMQ.Messages;
using MemoryMQ.Publisher;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;

namespace MemoryMQ.Benchmark;

public class StorageBenchmark
{
    private string _body;

    private ServiceProvider _sp;

    private ServiceProvider _sp1;

    private IMessagePublisher _publisher1;

    private ServiceProvider _sp2;

    private IMessagePublisher _publisher2;

    private List<IMessage> _msgs;

    [GlobalSetup]
    public void Setup()
    {
        var litedbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"litedb.db");
        var dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"sqlitedb.db");
        
        
        var json = File.ReadAllText("compress_data.json");
        var spotify = JsonConvert.DeserializeObject<SpotifyAlbumArray>(json);
        // _body = "aaaaaaaaaaaaaaaaaaaaaaaaa";//JsonConvert.SerializeObject(spotify);
        _body = JsonConvert.SerializeObject(spotify);

        #region LiteDB

        var serviceCollection = new ServiceCollection();
        serviceCollection.AddMemoryMQ(options =>
        {
            options.AddLiteDBStorage();
            options.DbConnectionString = litedbPath;
            options.EnablePersistence = true;
        });

        serviceCollection.AddLogging();
        serviceCollection.AddScoped<TestConsumer>();
        _sp = serviceCollection.BuildServiceProvider();

        var dispatcher = _sp.GetService<IMessageDispatcher>();
        dispatcher.StartDispatchAsync(default).Wait();
        _publisher1 = _sp.GetService<IMessagePublisher>();

        #endregion
        
        #region SQLite
        IServiceCollection serviceCollection2 = new ServiceCollection();

        serviceCollection2.AddMemoryMQ(config =>
        {
            config.ConsumerAssemblies = new Assembly[]
            {
                typeof(TestConsumer).Assembly
            };

            config.EnablePersistence = true;

            config.RetryInterval = TimeSpan.FromMilliseconds(100);

            config.EnableCompression = false;
            
            config.AddSqliteStorage();
        });

        serviceCollection2.AddLogging();
        serviceCollection2.AddScoped<TestConsumer>();
        _sp2 = serviceCollection2.BuildServiceProvider();

        var dispatcher2 = _sp2.GetService<IMessageDispatcher>();
        dispatcher2.StartDispatchAsync(default).Wait();
        _publisher2 = _sp2.GetService<IMessagePublisher>();
        #endregion
       
    }
    
    [Benchmark]
    public async Task PublishLiteDB()
    {
        _msgs = new List<IMessage>();
        for (int i = 0; i < 100; i++)
        {
            _msgs.Add(new Message("topic",_body));
        }
        await _publisher1.PublishAsync(_msgs);
    }
    [Benchmark]
    public async Task PublishSqlite()
    {
        _msgs = new List<IMessage>();
        for (int i = 0; i < 100; i++)
        {
            _msgs.Add(new Message("topic",_body));
        }
        await _publisher2.PublishAsync(_msgs);
    }
}