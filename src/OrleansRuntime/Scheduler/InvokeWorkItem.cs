using System;
using System.Threading.Tasks;

namespace Orleans.Runtime.Scheduler
{
    internal class InvokeWorkItem : WorkItemBase
    {
        private static readonly Logger logger = LogManager.GetLogger("InvokeWorkItem", LoggerType.Runtime);
        private readonly ActivationData activation;
        private readonly Message message;
        private readonly Dispatcher dispatcher;

        public InvokeWorkItem(ActivationData activation, Message message, Dispatcher dispatcher)
        {
            if (activation?.GrainInstance == null)
            {
                var str = string.Format("Creating InvokeWorkItem with bad activation: {0}. Message: {1}", activation, message);
                logger.Warn(ErrorCode.SchedulerNullActivation, str);
                throw new ArgumentException(str);
            }

            this.activation = activation;
            this.message = message;
            this.dispatcher = dispatcher;
            this.SchedulingContext = activation.SchedulingContext;
            activation.IncrementInFlightCount();
        }

        #region Implementation of IWorkItem

        public override WorkItemType ItemType
        {
            get { return WorkItemType.Invoke; }
        }

        public override string Name
        {
            get { return String.Format("InvokeWorkItem:Id={0} {1}", message.Id, message.DebugContext); }
        }

        public override void Execute()
        {
            var b = new TimeTracker("Invoke").Track();
            try
            {
                var grain = activation.GrainInstance;
                var runtimeClient = (ISiloRuntimeClient)grain.GrainReference.RuntimeClient;
                Action onTaskFinish = () =>
                {
                    activation.DecrementInFlightCount();
                    this.dispatcher.OnActivationCompletedRequest(activation, message);
                };

                runtimeClient.Invoke(grain, this.activation, this.message, onTaskFinish);
                   // .ContinueWithOptimized(onTaskFinish, onTaskFinish, onTaskFinish);

                //task.ContinueWithOptimized(onTaskFinish, onTaskFinish, onTaskFinish);
            }
            catch (Exception exc)
            {
                logger.Warn(ErrorCode.InvokeWorkItem_UnhandledExceptionInInvoke, 
                    String.Format("Exception trying to invoke request {0} on activation {1}.", message, activation), exc);

                activation.DecrementInFlightCount();
                this.dispatcher.OnActivationCompletedRequest(activation, message);
            }
            b.StopTrack();
        }

        #endregion

        public override string ToString()
        {
            return String.Format("{0} for activation={1} Message={2}", base.ToString(), activation, message);
        }
    }
}
