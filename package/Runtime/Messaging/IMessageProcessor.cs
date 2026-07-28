namespace Soso.Net.Messaging
{
	internal interface IMessageProcessor
	{
		void Process(IUserConnection source, object message, long messageNum, long recvTime, int channel);
	}
}
