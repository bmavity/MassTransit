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
namespace MassTransit.Pipeline.Configuration
{
	using MassTransit;
	using Inspectors;
	using Sinks;

	public class CorrelatedMessageRouterConfiguratorScope<TMessage, TKey> : 
		PipelineInspectorBase<CorrelatedMessageRouterConfiguratorScope<TMessage, TKey>>
		where TMessage : class, CorrelatedBy<TKey>
	{
		public CorrelatedMessageRouter<TMessage, TKey> Router { get; private set; }

		public bool Inspect<TRoutedMessage, TRoutedKey>(CorrelatedMessageRouter<TRoutedMessage, TRoutedKey> element)
			where TRoutedMessage : class, CorrelatedBy<TRoutedKey>
		{
			if (typeof (TRoutedMessage) == typeof (TMessage))
			{
				Router = element.TranslateTo<CorrelatedMessageRouter<TMessage, TKey>>();

				return false;
			}

			return true;
		}
	}
}