using Soso.Net.Logging;
using Soso.Net.Transports.TCP;
using System.Diagnostics;
using System.Net;
using Soso.Utils.Logging;

namespace Soso.Net.Benchmarks;

class Program
{
    static async Task Main(string[] args)
    {
        NetworkLogger.Level = LOG_LEVEL.Debug;
        // RunSerializationBenchmarks(1_000_000);
        await RunNetBenchmarks(true);
        // await RunInteractiveNet(true);
    }


    private static async Task RunNetBenchmarks(bool compression)
    {
        SosoNetwork.UseCompression = compression;
        try
        {
            // await RunInteractiveNet();
            await RunConcurrentSends(100_000);
        }
        catch (Exception e)
        {
            NetworkLogger.Error(NetworkLogger.CHANNEL.Default, "{e}", e);
        }
    }
	
    private static async Task RunConcurrentSends(ulong count)
    {
        var ep = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 5505);
        BenchListener p1 = SosoNetwork.CreateListener<BenchListener>(ep);

        BenchClient p2 = await SosoNetwork.ConnectAsync<BenchClient>(999, ep);
        p1.LogMessage = p2.LogMessage = false;


        Stopwatch sw = new Stopwatch();
        Console.WriteLine("Starting process....");
        ulong sent = 0;
        sw.Start();
        while (p2.ReceiveCount < count)
        {
            if (sent <= count)
            {
                sent++;
                p1.Broadcast($"Ping {sent}.... {LOREM_IPSUM}", 0);
                p2.Send($"Test {sent}.... {LOREM_IPSUM}", 0);
            }
            SosoNetwork.Process();
        }
        sw.Stop();
		
        Console.WriteLine($"Total time for {count} sends: {sw.ElapsedMilliseconds}ms");
        p2.Shutdown();
        p1.Shutdown();
    }
	
    private static async Task RunInteractiveNet(bool compression)
    {
        SosoNetwork.UseCompression = compression;
        NetworkLogger.Level = LOG_LEVEL.Debug;
		
        var ep = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 5505);
        BenchListener p1 = SosoNetwork.CreateListener<BenchListener>(ep);

        BenchClient p2 = await SosoNetwork.ConnectAsync<BenchClient>(999, ep);
        p1.LogMessage = p2.LogMessage = true;

        bool shutdown = false;
        while (p1.ConnectionCount > 0)
        {
            SosoNetwork.Process();
            if (Console.KeyAvailable)
            {
                var key = Console.ReadKey();
                switch (key.Key)
                {
                    case ConsoleKey.Q:
                        p2.Shutdown();
                        shutdown = true;
                        break;
                    case ConsoleKey.C:
                        p2.Send($"Hello? {DateTime.Now};", 1);
                        break;
                    case ConsoleKey.S:
                        p1.Broadcast("Ping....", 0);
                        break;
                }
            }
        }
		
        // p2.Shutdown();
        p1.Shutdown();
    }
	
	
	
    const string LOREM_IPSUM =
        "Lorem ipsum dolor sit amet, consectetur adipiscing elit. Donec ligula nisl, accumsan a nibh sit amet, ultrices dictum ligula. " +
        "Vestibulum varius rutrum purus, eget tempus purus aliquet in. Nam faucibus ullamcorper metus vitae aliquam. Sed posuere turpis " +
        "in ante maximus pulvinar ac nec urna. Fusce vel diam odio. Quisque in augue eu ligula gravida cursus. Vivamus eu varius lorem, " +
        "sagittis porta leo.\n\nAliquam erat volutpat. Nulla elementum ut purus eget malesuada. Vestibulum vel ante interdum, mattis sem " +
        "sit amet, efficitur augue. Nunc lacinia rhoncus erat, ut consectetur lorem bibendum vel. In nec volutpat mi. Vestibulum facilisis " +
        "enim ex, quis interdum metus rutrum nec. Nunc hendrerit egestas ex at varius. In orci sem, feugiat mattis nulla ut, scelerisque fau" +
        "cibus magna. Sed suscipit sapien ac dui pulvinar, eu aliquet urna eleifend. Phasellus mattis metus vel quam bibendum, sit amet luctus e" +
        "nim volutpat. Aenean at luctus lorem. Nullam ullamcorper nisi at neque aliquam, volutpat commodo mi hendrerit. Donec porta leo vel urna " +
        "mollis molestie. Aliquam erat volutpat. Praesent suscipit sem ut turpis mattis pharetra.\n\nSed efficitur mollis felis sit amet suscipit. " +
        "Praesent sit amet efficitur elit. Suspendisse ligula mi, congue maximus odio ac, vestibulum egestas odio. Aenean consequat justo ac augue " +
        "venenatis, id faucibus purus egestas. Vivamus ultrices cursus ultricies. Vestibulum ac libero in est dignissim aliquet aliquet ac risus. N" +
        "ulla consequat luctus porttitor. Suspendisse eu tellus tincidunt, luctus magna id, luctus ipsum.\n\nDuis et convallis nunc, a tristique er" +
        "os. Quisque vestibulum cursus laoreet. Integer faucibus dui eros, ac dignissim sapien placerat non. Duis tincidunt nibh ligula, id gravida" +
        " velit luctus vitae. Ut id elit tortor. Morbi non suscipit tortor. Sed viverra tincidunt gravida.\n\nDonec placerat urna vel est ultrices ul" +
        "lamcorper. Nulla malesuada id ligula eget auctor. Aenean blandit, turpis id aliquam consequat, velit est interdum augue, eget iaculis velit e" +
        "st ut odio. Vestibulum ante ipsum primis in faucibus orci luctus et ultrices posuere cubilia curae; Pellentesque tristique magna sed quam venen" +
        "atis sagittis at eu velit. Nulla facilisi. Class aptent taciti sociosqu ad litora torquent per conubia nostra, per inceptos himenaeos. Cras" +
        " lacinia nisl venenatis purus maximus ullamcorper. Cras rutrum arcu orci, non mollis nunc aliquam id. Integer vulputate eros eu nisi conse" +
        "quat, at ultrices dui placerat. Proin id volutpat massa. Vestibulum ante ipsum primis in faucibus orci luctus et ultrices posuere cubili" +
        "a curae; Pellentesque pellentesque ipsum massa, et lobortis augue blandit sit amet. Nam non dignissim risus. Praesent ut suscipit risus.";
}