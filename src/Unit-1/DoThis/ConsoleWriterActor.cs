using System;
using Akka.Actor;
using System.Threading;

namespace WinTail
{
    /// <summary>
    /// Actor responsible for serializing message writes to the console.
    /// (write one message at a time, champ :)
    /// </summary>
    class ConsoleWriterActor : UntypedActor
    {
        protected override void OnReceive(object message)
        {
            if (message is Messages.InputError)
            {
                var msg = message as Messages.InputError;
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(msg.Reason);//+ DateTime.Now.ToString("yyyy:MM:dd hh:mm:ss fff")
            }
            else if (message is Messages.InputSuccess)
            {
                var msg = message as Messages.InputSuccess;
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine(msg.Reason);//+ DateTime.Now.ToString("yyyy:MM:dd hh:mm:ss fff")
            }
            else
            {
                Console.WriteLine(message);
            }

            Console.ResetColor();

        }
    }
}
