using Soso.Net.Tests.Networkers;
using System.Net;

namespace Soso.Net.Tests
{
	// TODO: Tests
	public class NetworkTests
	{
		[Test]
		public async Task ConnectionTest()
		{
			var ep = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 5504);
			
			try
			{
				var listener = SosoNetwork.CreateListener<TestListener>(ep);

				var id = 9999UL;
				var client = await SosoNetwork.ConnectAsync<TestClient>(id, ep);

				Assert.That(id, Is.EqualTo(client.Connection.Id));
				Assert.NotNull(listener.GetConnection(id));
				
				listener.Shutdown();
			}
			catch (Exception ex)
			{
				Assert.Fail("Exception thrown: " + ex.Message);
			}
		}
	}
}
