﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace DtronixMessageQueue.Tests
{
    public class ActionProcessorTests
    {
        private readonly ITestOutputHelper _output;
        private ManualResetEventSlim completeEvent = new ManualResetEventSlim(false);

        public ActionProcessorTests(ITestOutputHelper output)
        {
            _output = output;
        }

        private ActionProcessor<Guid> CreateProcessor(int threads, bool start, Action<ActionProcessor<Guid>> complete = null, int rebalanceTime = 10000)
        {
            var processor = new ActionProcessor<Guid>(new ActionProcessor<Guid>.Config
            {
                ThreadName = "test",
                StartThreads = threads,
                RebalanceLoadPeriod = rebalanceTime
            }, complete);

            if (start)
                processor.Start();

            return processor;
        }

        [Fact]
        public void Processor_adds_threads()
        {
            var processor = CreateProcessor(1, false);

            Assert.Equal(processor.ThreadCount, 1);
            processor.AddThread(2);

            Assert.Equal(processor.ThreadCount, 3);
        }

        [Fact]
        public void Processor_removes_thread()
        {
            var processor = CreateProcessor(2, true);

            Assert.Equal(processor.ThreadCount, 2);
            processor.RemoveThread(1);

            Assert.Equal(processor.ThreadCount, 1);
        }

        [Fact]
        public void Processor_throws_on_non_started_processor()
        {
            var processor = CreateProcessor(1, false);
            var firstId = Guid.NewGuid();

            Assert.Throws<InvalidOperationException>(() => processor.Register(firstId, () => Thread.Sleep(50)));
            //processor.Queue(firstId);

        }

        [Fact]
        public void Processor_throws_on_too_few_threads()
        {
            var processor = CreateProcessor(1, true);

            Assert.Throws<InvalidOperationException>(() => processor.RemoveThread(1));
        }

        private ActionProcessor<Guid>.RegisteredAction RegisterQueueGet(ActionProcessor<Guid> processor, Action action)
        {
            var id = Guid.NewGuid();
            processor.Register(id, action);
            processor.Queue(id);

            return processor.GetActionById(id);
        }

        private ActionProcessor<Guid>.RegisteredAction RegisterGet(ActionProcessor<Guid> processor, Action action)
        {
            var id = Guid.NewGuid();
            processor.Register(id, action);

            return processor.GetActionById(id);
        }

        [Fact]
        public void Processor_balances_on_registration()
        {
            var processor = CreateProcessor(3, true);

            var firstRegisteredAction = RegisterQueueGet(processor, () => Thread.Sleep(50));
            var secondRegisteredAction = RegisterQueueGet(processor, () => Thread.Sleep(50));
            var thirdRegisteredAction = RegisterQueueGet(processor, () => Thread.Sleep(50));


            Assert.NotEqual(firstRegisteredAction.ProcessorThread, secondRegisteredAction.ProcessorThread);
            Assert.NotEqual(firstRegisteredAction.ProcessorThread, secondRegisteredAction.ProcessorThread);
            Assert.NotEqual(secondRegisteredAction.ProcessorThread, thirdRegisteredAction.ProcessorThread);
            Assert.NotEqual(firstRegisteredAction.ProcessorThread, thirdRegisteredAction.ProcessorThread);

        }


        [Fact]
        public void Processor_adds_to_least_used_thread()
        {
            var processor = CreateProcessor(2, true);

            var firstRegisteredAction = RegisterQueueGet(processor, () => Thread.Sleep(500));
            var secondRegisteredAction = RegisterQueueGet(processor, () => Thread.Sleep(10));
            var thirdRegisteredAction = RegisterGet(processor, () => Thread.Sleep(50));


            Assert.NotEqual(firstRegisteredAction.ProcessorThread, secondRegisteredAction.ProcessorThread);
            Assert.NotEqual(firstRegisteredAction.ProcessorThread, secondRegisteredAction.ProcessorThread);

            Assert.Equal(firstRegisteredAction.ProcessorThread, thirdRegisteredAction.ProcessorThread);
        }

        [Fact]
        public void Processor_queues_once()
        {
            var processor = CreateProcessor(1, true);

            var firstRegisteredAction = RegisterGet(processor, () => Thread.Sleep(5000));
            processor.QueueOnce(firstRegisteredAction.Id);
            processor.QueueOnce(firstRegisteredAction.Id);
            processor.QueueOnce(firstRegisteredAction.Id);

            Assert.Equal(1, firstRegisteredAction.ProcessorThread.Queued);
        }

        [Fact]
        public void Processor_queues_multiple()
        {
            var processor = CreateProcessor(1, true);
            var firstRegisteredAction = RegisterGet(processor, () => Thread.Sleep(5000));

            processor.Queue(firstRegisteredAction.Id);
            processor.Queue(firstRegisteredAction.Id);
            processor.Queue(firstRegisteredAction.Id);


            // Wait a period of time for the processor to pickup the call.
            Thread.Sleep(50);

            Assert.Equal(2, firstRegisteredAction.ProcessorThread.Queued);
        }

        [Fact]
        public void Processor_balances_on_removed_thread()
        {
            Action<ActionProcessor<Guid>> complete = ap => completeEvent.Set();
            var processor = CreateProcessor(2, true, complete);
            
            var interlockedInt = 0;

            var action = (Action) (() =>
            {
                Interlocked.Increment(ref interlockedInt);
                Thread.Sleep(1);
            });

            var firstRegisteredAction = RegisterGet(processor, action);
            var secondRegisteredAction = RegisterGet(processor, action);
            var thirdRegisteredAction = RegisterGet(processor, () => processor.RemoveThread(1));

            const int totalLoops = 10;

            for (int i = 0; i < totalLoops; i++)
            {
                processor.Queue(firstRegisteredAction.Id);
                processor.Queue(secondRegisteredAction.Id);

                // Half way through the loop, remove a thread.
                if (i == totalLoops / 2)
                    processor.Queue(thirdRegisteredAction.Id);
            }

            Assert.True(completeEvent.Wait(2000));

            Assert.Equal(totalLoops * 2, interlockedInt);

            Assert.Equal(3, thirdRegisteredAction.ProcessorThread.RegisteredActionsCount);
        }

        [Fact]
        public void Processor_balances_on_added_thread()
        {
            Action<ActionProcessor<Guid>> complete = ap => completeEvent.Set();
            var processor = CreateProcessor(2, true, complete);

            var interlockedInt = 0;

            var action = (Action)(() =>
            {
                Interlocked.Increment(ref interlockedInt);
                Thread.Sleep(1);
            });

            var firstRegisteredAction = RegisterGet(processor, action);
            var secondRegisteredAction = RegisterGet(processor, action);
            var thirdRegisteredAction = RegisterGet(processor, () => processor.AddThread(1));

            const int totalLoops = 100;

            for (int i = 0; i < totalLoops; i++)
            {
                processor.Queue(firstRegisteredAction.Id);
                processor.Queue(secondRegisteredAction.Id);

                // Half way through the loop, remove a thread.
                if (i == totalLoops / 2)
                    processor.Queue(thirdRegisteredAction.Id);
            }

            Assert.True(completeEvent.Wait(2000));

            Assert.Equal(totalLoops * 2, interlockedInt);

            Assert.Equal(1, firstRegisteredAction.ProcessorThread.RegisteredActionsCount);
            Assert.Equal(1, secondRegisteredAction.ProcessorThread.RegisteredActionsCount);
            Assert.Equal(1, thirdRegisteredAction.ProcessorThread.RegisteredActionsCount);
        }
    }
}
