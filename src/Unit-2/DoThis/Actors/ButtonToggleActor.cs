using Akka.Actor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ChartApp.Actors
{    
    /// <summary>
    /// Actor responsible for managing button toggles
    /// </summary>
    public class ButtonToggleActor : UntypedActor
    {
        #region Message types

        /// <summary>
        /// Toggles this button on or off and sends an appropriate messages
        /// to the <see cref="PerformanceCounterCoordinatorActor"/>
        /// </summary>
        public class Toggle { }

        #endregion

        private readonly CounterType myCounterType;
        private bool isToggledOn;
        private readonly Button myButton;
        private readonly IActorRef coordinatorActor;

        public ButtonToggleActor(IActorRef coordinatorActor, Button myButton, CounterType myCounterType, bool isToggledOn = false)
        {

            this.coordinatorActor = coordinatorActor;
            this.myButton = myButton;
            this.myCounterType = myCounterType;
            this.isToggledOn = isToggledOn;
        }

        protected override void OnReceive(object message)
        {
            if (message is Toggle && this.isToggledOn)
            {
                // toggle is currently on
                // stop watching this counter
                coordinatorActor.Tell(new PerformanceCounterCoordinatorActor.UnWatch(myCounterType));
                FlipToggle();

            }
            else if (message is Toggle && !this.isToggledOn)
            {
                // toggle is currently off
                // start watching this counter
                coordinatorActor.Tell(new PerformanceCounterCoordinatorActor.Watch(myCounterType));
                FlipToggle();
            }
            else
            {
                Unhandled(message);
            }
        }

        private void FlipToggle()
        {
            // flip the toggle
            this.isToggledOn = !this.isToggledOn;

            // change the text of the button
            myButton.Text = string.Format("{0} ({1})",
                myCounterType.ToString().ToUpperInvariant(),
                isToggledOn ? "ON" : "OFF");
        }
    }
}
