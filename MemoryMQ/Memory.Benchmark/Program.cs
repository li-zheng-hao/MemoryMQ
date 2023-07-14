using System.Reflection;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;
using MemoryMQ;
using MemoryMQ.Benchmark;
using MemoryMQ.Dispatcher;
using MemoryMQ.Messages;
using MemoryMQ.Publisher;
using Microsoft.Extensions.DependencyInjection;

Summary summary = BenchmarkRunner.Run<PublishTest>();

 Console.WriteLine(summary);

// var p=new PublishTest();
// p.Setup();
// p.Publish();


[SimpleJob(RuntimeMoniker.Net60)]
[RPlotExporter]
public class PublishTest
{
    private ServiceProvider _sp;

    private List<IMessage> _msgs;

    private IMessagePublisher _persisitPublisher;

    private IMessagePublisher _memoryPublisher;

    [GlobalSetup]
    public void Setup()
    {
        IServiceCollection serviceCollection = new ServiceCollection();
        serviceCollection.AddMemoryMQ(config =>
        {
            config.ConsumerAssemblies = new Assembly[]
            {
                typeof(TestConsumer).Assembly
            };

            config.EnablePersistent = true;

            config.RetryInterval = TimeSpan.FromMilliseconds(100);
        });
        serviceCollection.AddLogging();
        serviceCollection.AddScoped<TestConsumer>();
        _sp=serviceCollection.BuildServiceProvider();

        var dispatcher = _sp.GetService<IMessageDispatcher>();
         dispatcher.StartDispatchAsync(default).Wait();
         _persisitPublisher = _sp.GetService<IMessagePublisher>();
         
         
         IServiceCollection serviceCollection2 = new ServiceCollection();
         serviceCollection2.AddMemoryMQ(config =>
         {
             config.ConsumerAssemblies = new Assembly[]
             {
                 typeof(TestConsumer).Assembly
             };

             config.EnablePersistent = false;
             config.DbConnectionString = "DataSource=memorymq2.db";
             config.RetryInterval = TimeSpan.FromMilliseconds(100);
         });
         serviceCollection2.AddLogging();
         serviceCollection2.AddScoped<TestConsumer>();
         _sp=serviceCollection2.BuildServiceProvider();

         var dispatcher2 = _sp.GetService<IMessageDispatcher>();
         dispatcher2.StartDispatchAsync(default).Wait();
         _memoryPublisher = _sp.GetService<IMessagePublisher>();

        
    }


    [Benchmark]
    public async Task PublishWithPersist()
    {
        _msgs = new List<IMessage>();
        for (int i = 0; i < 100; i++)
        {
            _msgs.Add(new Message("topic","hello"));
        }
        await _persisitPublisher.PublishAsync(_msgs);
    }
    
    [Benchmark]
    public async Task PublishWithoutPersist()
    {
        _msgs = new List<IMessage>();
        for (int i = 0; i < 1000; i++)
        {
            _msgs.Add(new Message("topic","hello"));
        }
        await _memoryPublisher.PublishAsync(_msgs);
    }
}