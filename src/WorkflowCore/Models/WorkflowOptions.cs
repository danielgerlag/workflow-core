using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WorkflowCore.Interface;
using WorkflowCore.Services;

namespace WorkflowCore.Models
{
    public class WorkflowOptions
    {

        internal Func<IServiceProvider, IPersistenceProvider> persistanceFactory;
        internal Func<IServiceProvider, IConcurrencyProvider> concurrencyFactory;
        internal int threadCount;
        internal TimeSpan pollInterval;
        internal TimeSpan idleTime;
        internal TimeSpan errorRetryInterval;

        public WorkflowOptions()
        {
            //set defaults
            threadCount = 1; // Environment.ProcessorCount;
            pollInterval = TimeSpan.FromSeconds(10);
            idleTime = TimeSpan.FromMilliseconds(500);
            errorRetryInterval = TimeSpan.FromSeconds(10);

            concurrencyFactory = new Func<IServiceProvider, IConcurrencyProvider>(sp => new SingleNodeConcurrencyProvider());
        }

        public void UsePersistence(Func<IServiceProvider, IPersistenceProvider> factory)
        {
            persistanceFactory = factory;
        }

        public void UseThreads(int count)
        {
            threadCount = count;
        }

        public void UsePollInterval(TimeSpan interval)
        {
            pollInterval = interval;
        }

    }
}
