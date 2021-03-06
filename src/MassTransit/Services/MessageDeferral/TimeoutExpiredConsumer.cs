// Copyright 2007-2008 The Apache Software Foundation.
//  
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use 
// this file except in compliance with the License. You may obtain a copy of the 
// License at 
// 
//     http://www.apache.org/licenses/LICENSE-2.0 
// 
// Unless required by applicable law or agreed to in writing, software distributed 
// under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR 
// CONDITIONS OF ANY KIND, either express or implied. See the License for the 
// specific language governing permissions and limitations under the License.
namespace MassTransit.Services.MessageDeferral
{
	using Magnum.Reflection;
    using Messages;
    using Timeout.Messages;

    public class TimeoutExpiredConsumer :
        Consumes<TimeoutExpired>.Selected
    {
        private readonly IServiceBus _bus;
        private readonly IDeferredMessageRepository _repository;

        public TimeoutExpiredConsumer(IServiceBus bus, IDeferredMessageRepository repository)
        {
            _repository = repository;
            _bus = bus;
        }

        public void Consume(TimeoutExpired message)
        {
            DeferredMessage deferredMessage = _repository.Get(message.CorrelationId);

            RepublishMessage(deferredMessage.GetMessage());

            _repository.Remove(message.CorrelationId);
            _bus.Publish(new DeferedMessageRepublished(deferredMessage.Id));
        }

        public bool Accept(TimeoutExpired message)
        {
            return _repository.Contains(message.CorrelationId);
        }

        private void RepublishMessage(object message)
        {
        	_bus.FastInvoke(x => x.Publish(message), message);
        }
    }
}