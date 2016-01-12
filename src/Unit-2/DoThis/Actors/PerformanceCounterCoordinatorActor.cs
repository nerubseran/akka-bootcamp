using Akka.Actor;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms.DataVisualization.Charting;

namespace ChartApp.Actors
{
    /// <summary>
    /// Actor responsible for translating UI calls into ActorSystem messages
    /// </summary>
    public class PerformanceCounterCoordinatorActor : ReceiveActor
    {
        #region Message Types

        /// <summary>
        /// Subscribe the <see cref="ChartingActor"/> to updates for <see cref="Counter"/>.
        /// </summary>
        public class Watch
        {
            public Watch(CounterType counter)
            {
                this.Counter = counter;
            }

            public CounterType Counter { get; private set; }
        }

        public class UnWatch
        {

            public UnWatch(CounterType counter)
            {
                this.Counter = counter;
            }

            public CounterType Counter { get; private set; }
        }

        #endregion

        /// <summary>
        /// Methods for generating new instances of all <see cref="PerformanceCounter"/>s
        /// we want to monitor
        /// </summary>
        private static readonly Dictionary<CounterType, Func<PerformanceCounter>> CounterGenerators
            = new Dictionary<CounterType, Func<PerformanceCounter>>()
                    {
                        {CounterType.Cpu, () => new PerformanceCounter("Processor", "% Processor Time", "_Total", true)}
                        ,{CounterType.Memory, () => new PerformanceCounter("Memory", "% Committed Bytes In Use", true)}
                        ,{CounterType.Disk, () => new PerformanceCounter("LogicalDisk", "% Disk Time", "_Total", true)}
                    };

        /// <summary>
        /// Methods for creating new <see cref="Series"/> with distinct colors and names
        /// corresponding to each <see cref="PerformanceCounter"/>
        /// </summary>
        private static readonly Dictionary<CounterType, Func<Series>> CounterSeries
            = new Dictionary<CounterType, Func<Series>>()
                    {
                          {CounterType.Cpu, () =>
                            new Series(CounterType.Cpu.ToString()){ ChartType = SeriesChartType.SplineArea,
                             Color = Color.DarkGreen}},
                            {CounterType.Memory, () =>
                            new Series(CounterType.Memory.ToString()){ ChartType = SeriesChartType.FastLine,
                            Color = Color.MediumBlue}},
                            {CounterType.Disk, () =>
                            new Series(CounterType.Disk.ToString()){ ChartType = SeriesChartType.SplineArea,
                            Color = Color.DarkRed}},
                    };

        private Dictionary<CounterType, IActorRef> counterActors;

        private IActorRef chartingActor;

        public PerformanceCounterCoordinatorActor(IActorRef chartingActor)
            : this(chartingActor, new Dictionary<CounterType, IActorRef>())
        {

        }

        public PerformanceCounterCoordinatorActor(IActorRef chartingActor,
           Dictionary<CounterType, IActorRef> counterActors)
        {
            this.chartingActor = chartingActor;
            this.counterActors = counterActors;

            Receive<Watch>(watch =>
            {
                if (!counterActors.ContainsKey(watch.Counter))
                {
                    // create a child actor to monitor this counter if
                    // one doesn't exist already
                    IActorRef counterActor = Context.ActorOf(Props.Create(() => new PerformanceCounterActor(watch.Counter.ToString(), CounterGenerators[watch.Counter])));
                    // add this counter actor to our index
                    counterActors[watch.Counter] = counterActor;
                }
                // register this series with the ChartingActor
                chartingActor.Tell(new ChartingActor.AddSeries(CounterSeries[watch.Counter]()));

                // tell the counter actor to begin publishing its
                // statistics to the _chartingActor
                counterActors[watch.Counter].Tell(new SubscribeCounter(watch.Counter, chartingActor));

            });

            Receive<UnWatch>(unWatch =>
            {
                if (!counterActors.ContainsKey(unWatch.Counter))
                {
                    return;
                }

                // unsubscribe the ChartingActor from receiving any more updates
                counterActors[unWatch.Counter].Tell(new UnsubscribeCounter(
                    unWatch.Counter, chartingActor));

                // remove this series from the ChartingActor
                chartingActor.Tell(new ChartingActor.RemoveSeries(
                    unWatch.Counter.ToString()));
            });
        }
    }
}
