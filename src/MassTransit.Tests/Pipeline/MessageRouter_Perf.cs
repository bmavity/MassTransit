namespace MassTransit.Tests.Pipeline
{
	using System.Diagnostics;
	using System.Threading;
	using MassTransit.Pipeline;
	using MassTransit.Pipeline.Sinks;
	using Messages;
	using NUnit.Framework;
	using Rhino.Mocks;

	[TestFixture]
	public class MessageRouter_Perf
	{
		private static PerformantConsumer<PingMessage> _consumer;

		internal class PerformantConsumer<T> :
			Consumes<T>.All
			where T : class
		{
			private readonly long _limit;
			private long _count;
			private readonly ManualResetEvent _complete = new ManualResetEvent(false);

			public ManualResetEvent Complete
			{
				get { return _complete; }
			}

			public PerformantConsumer(long limit)
			{
				_limit = limit;
			}

			public void Consume(T message)
			{
				long value = Interlocked.Increment(ref _count);
				if (value == _limit)
					_complete.Set();
			}
		}


		[Test, Explicit]
		public void Router_performance_measurement()
		{
			IPipelineSink<PingMessage> router = SetupRouterOnly();

			const int primeLoopCount = 10;
			SendMessages(router, primeLoopCount);

			Stopwatch timer = Stopwatch.StartNew();

			const int loopCount = 1500000;
			SendMessages(router, loopCount);

			timer.Stop();

			Trace.WriteLine("Elapsed Time: " + timer.ElapsedMilliseconds + "ms");
			Trace.WriteLine("Messages Per Second: " + loopCount*1000/timer.ElapsedMilliseconds);
		}

		[Test, Explicit]
		public void Nested_router_performance_measurement()
		{
			IPipelineSink<object> router = SetupTwoRoutersOnly();

			const int primeLoopCount = 10;
			SendMessages(router, primeLoopCount);

			Stopwatch timer = Stopwatch.StartNew();

			const int loopCount = 1500000;
			SendMessages(router, loopCount);

			timer.Stop();

			Trace.WriteLine("Elapsed Time: " + timer.ElapsedMilliseconds + "ms");
			Trace.WriteLine("Messages Per Second: " + loopCount*1000/timer.ElapsedMilliseconds);
		}

		private static IPipelineSink<PingMessage> SetupRouterOnly()
		{
			_consumer = new PerformantConsumer<PingMessage>(1500000);

			var messageSink = new InstanceMessageSink<PingMessage>(message => _consumer.Consume);

			var router = new MessageRouter<PingMessage>();
			router.Connect(messageSink);
			return router;
		}

		private static IPipelineSink<object> SetupTwoRoutersOnly()
		{
			_consumer = new PerformantConsumer<PingMessage>(1500000);

			var messageSink = new InstanceMessageSink<PingMessage>(message => _consumer.Consume);

			var router = new MessageRouter<PingMessage>();
			router.Connect(messageSink);

			var translater = new MessageTranslator<object, PingMessage>(router);

			var nextRouter = new MessageRouter<object>();
			nextRouter.Connect(translater);

			return nextRouter;
		}

		private static void SendMessages(IPipelineSink<PingMessage> sink, int primeLoopCount)
		{
			for (int i = 0; i < primeLoopCount; i++)
			{
				var message = new PingMessage();
				foreach (var item in sink.Enumerate(message))
				{
					item(message);
				}
			}
		}

		private static void SendMessages(IPipelineSink<object> sink, int primeLoopCount)
		{
			for (int i = 0; i < primeLoopCount; i++)
			{
				var message = new PingMessage();
				foreach (var item in sink.Enumerate(message))
				{
					item(message);
				}
			}
		}
	}

    [TestFixture]
    public class Throughput_Specs
    {
        [Test, Explicit]
        public void How_many_messages_can_the_pipe_send_per_second()
        {
            long count = 0;
            long count2 = 0;
            long limit = 2500000;

            var messageSink = new InstanceMessageSink<ClaimModified>(m => msg => { count++; });
            var messageSink2 = new InstanceMessageSink<ClaimModified>(m => msg => { count2++; });
            var router = new MessageRouter<ClaimModified>();
            router.Connect(messageSink);
            router.Connect(messageSink2);


            var translater = new MessageTranslator<object, ClaimModified>(router);
            var objectRouter = new MessageRouter<object>();
            objectRouter.Connect(translater);
            var pipeline = new MessagePipeline(objectRouter, MockRepository.GenerateMock<IPipelineConfigurator>());

            var message = new ClaimModified();

            for (int i = 0; i < 100; i++)
            {
                pipeline.Dispatch(message);
            }

            count = 0;
            count2 = 0;

            Stopwatch timer = Stopwatch.StartNew();

            for (int i = 0; i < limit; i++)
            {
                pipeline.Dispatch(message);
            }

            timer.Stop();

            Trace.WriteLine("Received: " + (count+count2) + ", expected " + limit * 2);
            Trace.WriteLine("Elapsed Time: " + timer.ElapsedMilliseconds + "ms");
            Trace.WriteLine("Messages Per Second: " + limit*1000/timer.ElapsedMilliseconds);
        }
    }

    public class ClaimModified
    {
        
    }
}