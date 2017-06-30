using System;
using System.Collections;
using System.Collections.Generic;
using Orleans.Runtime.Configuration;

namespace Orleans.Runtime
{
    internal sealed class RequestInvocationInfoAccessor
    {
        private ClusterConfiguration Config { get; }

        public RequestInvocationInfoAccessor(ClusterConfiguration config)
        {
            Config = config;
        }

        public void AddInvokationInfo(Message message)
        {
            if (message.TargetGrain.IsSystemTarget)
            {
                return;
            }

            // assumes deadlock information was already loaded into RequestContext from the message
            var thisInvocation = Config.Globals.PerformDeadlockDetection ? new RequestInvocationHistory(message) : new RequestInvocationInfo(message);
            var obj = RequestContext.Get(RequestContext.CALL_CHAIN_REQUEST_CONTEXT_HEADER);
            if (obj != null)
            {
                var list = obj as IList;
                IList prevChain;
                if (list != null)
                {
                    prevChain = list;

                    // append this call to the end of the call chain. Update in place.
                    prevChain.Add(thisInvocation);
                }
                else
                {
                    prevChain = new List<object>();
                    prevChain.Add(obj);
                    prevChain.Add(thisInvocation);
                    RequestContext.Set(RequestContext.CALL_CHAIN_REQUEST_CONTEXT_HEADER, prevChain);
                }
            }
            else
            {
                // for 1 object there's no need in list allocation;
                // has to be always in sync with retrieving part
                RequestContext.Set(RequestContext.CALL_CHAIN_REQUEST_CONTEXT_HEADER, thisInvocation);
            }
        }

        public bool TryGetInvocationInfoList(Message message, out IList invokationInfoList)
        {
            invokationInfoList = null;
            var requestContext = message.RequestContextData;
            object obj;
            if (requestContext == null ||
                !requestContext.TryGetValue(RequestContext.CALL_CHAIN_REQUEST_CONTEXT_HEADER, out obj) ||
                obj == null) return false; // first call in a chain

            var list = obj as IList;
            if (list != null)
            {
                invokationInfoList = list;
            }
            else
            {
                invokationInfoList = new List<RequestInvocationInfo>
                {
                    (RequestInvocationInfo)obj
                };
            }

            return true;
        }
    }
}
