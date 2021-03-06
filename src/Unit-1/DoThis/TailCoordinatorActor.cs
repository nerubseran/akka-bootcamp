﻿using Akka.Actor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WinTail
{
    public class TailCoordinatorActor : UntypedActor
    {
        #region MessageTypes

        /// <summary>
        /// Start tailing the file at user-specified path.
        /// </summary>
        public class StartTail
        {

            public StartTail(string filePath, IActorRef reporterActor)
            {
                this.FilePath = filePath;
                this.ReporterActor = reporterActor;
            }

            public string FilePath { get; private set; }
            public IActorRef ReporterActor { get; private set; }
        }


        /// <summary>
        /// Stop tailing the file at user-specified path.
        /// </summary>
        public class StopTail
        {
            public StopTail(string filePath)
            {
                this.FilePath = filePath;
            }

            public string FilePath { get; private set; }
        }

        #endregion

        protected override void OnReceive(object message)
        {
            var msg = message as StartTail;
            IActorRef tailActor = Context.ActorOf(Props.Create(() => new TailActor(msg.ReporterActor, msg.FilePath)));
        }

        protected override SupervisorStrategy SupervisorStrategy()
        {
            return new OneForOneStrategy(10, TimeSpan.FromSeconds(30),
            x =>
            {
                if (x is ArithmeticException)
                {
                    return Directive.Resume;
                }
                else if (x is NotSupportedException)
                {
                    return Directive.Stop;
                }
                else
                {
                    return Directive.Restart;
                }
            });

        }
    }
}
