using System.Diagnostics;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
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
using Newtonsoft.Json;

Summary summary = BenchmarkRunner.Run<StorageBenchmark>();

 Console.WriteLine(summary);
//
//
// var p=new PublishTest();
// p.Setup();
// Stopwatch sw = new();
// sw.Start();
// p.PublishWithPersist();
// Console.WriteLine(sw.ElapsedMilliseconds);

[SimpleJob(RuntimeMoniker.Net60)]
[RPlotExporter]
public class PublishTest
{
    private ServiceProvider _sp;

    private List<IMessage> _msgs;

    private IMessagePublisher publisher1;

    private IMessagePublisher publisher2;

    private string _body;

    private IMessagePublisher publisher3;

    private IMessagePublisher publisher4;

    [GlobalSetup]
    public void Setup()
    {
        Directory.GetFiles(AppDomain.CurrentDomain.BaseDirectory, "*.db").ToList().ForEach(File.Delete);
        
        #region Metho1

        IServiceCollection serviceCollection = new ServiceCollection();

        serviceCollection.AddMemoryMQ(config =>
        {
            config.ConsumerAssemblies = new Assembly[]
            {
                typeof(TestConsumer).Assembly
            };

            config.EnablePersistence = true;

            config.RetryInterval = TimeSpan.FromMilliseconds(100);

            config.EnableCompression = false;
        });

        serviceCollection.AddLogging();
        serviceCollection.AddScoped<TestConsumer>();
        _sp = serviceCollection.BuildServiceProvider();

        var dispatcher = _sp.GetService<IMessageDispatcher>();
        dispatcher.StartDispatchAsync(default).Wait();
        publisher1 = _sp.GetService<IMessagePublisher>();

        #endregion
        
        #region Method2
        
        IServiceCollection serviceCollection2 = new ServiceCollection();
        
        serviceCollection2.AddMemoryMQ(config =>
        {
            config.ConsumerAssemblies = new Assembly[]
            {
                typeof(TestConsumer).Assembly
            };
        
            config.EnablePersistence = false;
            config.DbConnectionString = "DataSource=memorymq2.db";
            config.RetryInterval = TimeSpan.FromMilliseconds(100);
            
            config.EnableCompression = false;
        
        });
        
        serviceCollection2.AddLogging();
        serviceCollection2.AddScoped<TestConsumer>();
        var _sp2 = serviceCollection2.BuildServiceProvider();
        
        var dispatcher2 = _sp2.GetService<IMessageDispatcher>();
        dispatcher2.StartDispatchAsync(default).Wait();
        publisher2 = _sp2.GetService<IMessagePublisher>();
        
        #endregion
        
        #region Method3
        
        IServiceCollection serviceCollection3 = new ServiceCollection();
        
        serviceCollection3.AddMemoryMQ(config =>
        {
            config.ConsumerAssemblies = new Assembly[]
            {
                typeof(TestConsumer).Assembly
            };
        
            config.EnablePersistence = false;
            config.DbConnectionString = "DataSource=memorymq3.db";
            config.RetryInterval = TimeSpan.FromMilliseconds(100);
            config.EnableCompression=true;
        });
        
        serviceCollection3.AddLogging();
        serviceCollection3.AddScoped<TestConsumer>();
        var _sp3 = serviceCollection3.BuildServiceProvider();
        
        var dispatcher3 = _sp3.GetService<IMessageDispatcher>();
        dispatcher3.StartDispatchAsync(default).Wait();
        publisher3 = _sp3.GetService<IMessagePublisher>();
        
        #endregion
        
        #region Method4
        
        IServiceCollection serviceCollection4 = new ServiceCollection();
        
        serviceCollection4.AddMemoryMQ(config =>
        {
            config.ConsumerAssemblies = new Assembly[]
            {
                typeof(TestConsumer).Assembly
            };
        
            config.EnablePersistence = true;
            config.DbConnectionString = "DataSource=memorymq4.db";
            config.RetryInterval = TimeSpan.FromMilliseconds(100);
            config.EnableCompression = true;
        });
        
        serviceCollection4.AddLogging();
        serviceCollection4.AddScoped<TestConsumer>();
        var _sp4 = serviceCollection4.BuildServiceProvider();
        
        var dispatcher4 = _sp4.GetService<IMessageDispatcher>();
        dispatcher4.StartDispatchAsync(default).Wait();
        publisher4 = _sp4.GetService<IMessagePublisher>();
        
        #endregion

    

        var json = File.ReadAllText("compress_data.json");
        var spotify = JsonConvert.DeserializeObject<SpotifyAlbumArray>(json);
        // _body = "aaaaaaaaaaaaaaaaaaaaaaaaa";//JsonConvert.SerializeObject(spotify);
        _body = JsonConvert.SerializeObject(spotify);
    }


    [Benchmark]
    public async Task PublishWithPersist()
    {
        _msgs = new List<IMessage>();
        for (int i = 0; i < 100; i++)
        {
            _msgs.Add(new Message("topic",_body));
        }
        await publisher1.PublishAsync(_msgs);
    }
    
    [Benchmark]
    public async Task PublishInMemory()
    {
        _msgs = new List<IMessage>();
        for (int i = 0; i < 100; i++)
        {
            _msgs.Add(new Message("topic",_body));
        }
        await publisher2.PublishAsync(_msgs);
    }
    
    [Benchmark]
    public async Task PublishWithCompress()
    {
        _msgs = new List<IMessage>();
        for (int i = 0; i < 100; i++)
        {
            _msgs.Add(new Message("topic",_body));
        }
        await publisher3.PublishAsync(_msgs);
    }
    
    [Benchmark]
    public async Task PublishWithPersistAndCompress()
    {
        _msgs = new List<IMessage>();
        for (int i = 0; i < 100; i++)
        {
            _msgs.Add(new Message("topic",_body));
        }
        await publisher4.PublishAsync(_msgs);
    }
}


[Serializable]
[DataContract]
internal class SpotifyAlbumArray
{
    [DataMember(Order = 1)]
    [JsonProperty("spotify_albums")]
    public SpotifyAlbum[] SpotifyAlbums { get; set; }
}
[Serializable]
[DataContract]
internal class SpotifyAlbum
{
    [DataMember(Order = 1)]
    [JsonProperty("album_type")]
    public string AlbumType { get; set; }

    [DataMember(Order = 2)]
    [JsonProperty("artists")]
    public ArtistDto[] Artists { get; set; }

    [DataMember(Order = 4)]
    [JsonProperty("available_markets")]
    public string[] AvailableMarkets { get; set; }

    [DataMember(Order = 5)]
    [JsonProperty("copyrights")]
    public CopyrightDto[] Copyrights { get; set; }

    [DataMember(Order = 6)]
    [JsonProperty("external_ids")]
    public ExternalIdsDto ExternalIds { get; set; }

    [DataMember(Order = 7)]
    [JsonProperty("external_urls")]
    public ExternalUrlsDto ExternalUrls { get; set; }

    //[Key(7)]
    //[DataMember(Order = 8)]
    //[JsonProperty("genres")]
    //public object[] Genres { get; set; }

    [DataMember(Order = 9)]
    [JsonProperty("href")]
    public string Href { get; set; }

    [DataMember(Order = 10)]
    [JsonProperty("id")]
    public string Id { get; set; }

    [DataMember(Order = 11)]
    [JsonProperty("images")]
    public ImageDto[] Images { get; set; }

    [DataMember(Order = 12)]
    [JsonProperty("name")]
    public string Name { get; set; }

    [DataMember(Order = 13)]
    [JsonProperty("popularity")]
    public long Popularity { get; set; }

    [DataMember(Order = 14)]
    [JsonProperty("release_date")]
    public string ReleaseDate { get; set; }

    [DataMember(Order = 15)]
    [JsonProperty("release_date_precision")]
    public string ReleaseDatePrecision { get; set; }

    [DataMember(Order = 16)]
    [JsonProperty("tracks")]
    public TracksDto Tracks { get; set; }

    [DataMember(Order = 17)]
    [JsonProperty("type")]
    public string Type { get; set; }

    [DataMember(Order = 18)]
    [JsonProperty("uri")]
    public string Uri { get; set; }
}

[Serializable]
[DataContract]
public class TracksDto
{
    [DataMember(Order = 1)]
    [JsonProperty("href")]
    public string Href { get; set; }

    [DataMember(Order = 2)]
    [JsonProperty("items")]
    public ItemDto[] Items { get; set; }

    [DataMember(Order = 3)]
    [JsonProperty("limit")]
    public long Limit { get; set; }

    //[DataMember(Order = 4)]
    //[JsonProperty("next")]
    //public object Next { get; set; }

    [DataMember(Order = 5)]
    [JsonProperty("offset")]
    public long Offset { get; set; }

    //[DataMember(Order = 6)]
    //[JsonProperty("previous")]
    //public object Previous { get; set; }

    [DataMember(Order = 7)]
    [JsonProperty("total")]
    public long Total { get; set; }
}

[Serializable]
[DataContract]
public class ItemDto
{
    [DataMember(Order = 1)]
    [JsonProperty("artists")]
    public ArtistDto[] Artists { get; set; }

    [DataMember(Order = 2)]
    [JsonProperty("available_markets")]
    public string[] AvailableMarkets { get; set; }

    [DataMember(Order = 3)]
    [JsonProperty("disc_number")]
    public long DiscNumber { get; set; }

    [DataMember(Order = 4)]
    [JsonProperty("duration_ms")]
    public long DurationMs { get; set; }

    [DataMember(Order = 5)]
    [JsonProperty("explicit")]
    public bool Explicit { get; set; }

    [DataMember(Order = 6)]
    [JsonProperty("external_urls")]
    public ExternalUrlsDto ExternalUrls { get; set; }

    [DataMember(Order = 7)]
    [JsonProperty("href")]
    public string Href { get; set; }

    [DataMember(Order = 8)]
    [JsonProperty("id")]
    public string Id { get; set; }

    [DataMember(Order = 9)]
    [JsonProperty("name")]
    public string Name { get; set; }

    [DataMember(Order = 10)]
    [JsonProperty("preview_url")]
    public string PreviewUrl { get; set; }

    [DataMember(Order = 11)]
    [JsonProperty("track_number")]
    public long TrackNumber { get; set; }

    [DataMember(Order = 12)]
    [JsonProperty("type")]
    public string Type { get; set; }

    [DataMember(Order = 13)]
    [JsonProperty("uri")]
    public string Uri { get; set; }
}

[Serializable]
[DataContract]
public class ImageDto
{
    [DataMember(Order = 1)]
    [JsonProperty("height")]
    public long Height { get; set; }

    [DataMember(Order = 2)]
    [JsonProperty("url")]
    public string Url { get; set; }

    [DataMember(Order = 3)]
    [JsonProperty("width")]
    public long Width { get; set; }
}

[Serializable]
[DataContract]
public class ExternalIdsDto
{
    [DataMember(Order = 1)]
    [JsonProperty("upc")]
    public string Upc { get; set; }
}

[Serializable]
[DataContract]
public class CopyrightDto
{
    [DataMember(Order = 1)]
    [JsonProperty("text")]
    public string Text { get; set; }

    [DataMember(Order = 2)]
    [JsonProperty("type")]
    public string Type { get; set; }
}

[Serializable]
[DataContract]
public class ArtistDto
{
    [DataMember(Order = 1)]
    [JsonProperty("external_urls")]
    public ExternalUrlsDto ExternalUrls { get; set; }

    [DataMember(Order = 2)]
    [JsonProperty("href")]
    public string Href { get; set; }

    [DataMember(Order = 3)]
    [JsonProperty("id")]
    public string Id { get; set; }

    [DataMember(Order = 4)]
    [JsonProperty("name")]
    public string Name { get; set; }

    [DataMember(Order = 5)]
    [JsonProperty("type")]
    public string Type { get; set; }

    [DataMember(Order = 6)]
    [JsonProperty("uri")]
    public string Uri { get; set; }
}

[Serializable]
[DataContract]
public class ExternalUrlsDto
{
    [DataMember(Order = 1)]
    [JsonProperty("spotify")]
    public string Spotify { get; set; }
}