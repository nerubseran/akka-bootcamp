using Akka.Actor;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WinTail
{
    /// <summary>
    /// Monitors the file at <see cref="_filePath"/> for changes and sends
    /// file updates to console.
    /// </summary>
    public class TailActor : UntypedActor
    {
        #region MessageTypes

        /// <summary>
        /// Signal that the file has changed, and we need to read the next line of the file.
        /// </summary>
        public class FileWrite
        {

            public FileWrite(string fileName)
            {
                this.FileName = fileName;
            }

            public string FileName { get; private set; }
        }
        /// <summary>
        /// Signal that the OS had an error accessing the file.
        /// </summary>
        public class FileError
        {

            public FileError(string fileName, string reason)
            {
                this.FileName = fileName;
                this.Reason = reason;
            }

            public string FileName { get; private set; }
            public string Reason { get; private set; }
        }

        /// <summary>
        /// Signal to read the initial contents of the file at actor startup.
        /// </summary>
        public class InitialRead
        {

            public InitialRead(string fileName, string text)
            {
                this.FileName = fileName;
                this.Text = text;
            }

            public string FileName { get; private set; }
            public string Text { get; private set; }
        }

        #endregion

        private readonly string filePath;
        private readonly IActorRef reporterActor;
        private readonly FileObserver observer;
        private readonly Stream fileStream;
        private readonly StreamReader fileStreamReader;

        public TailActor(IActorRef reporterActor, string filePath)
        {
            this.reporterActor = reporterActor;
            this.filePath = filePath;

            // start watching file for changes
            observer = new FileObserver(Self, Path.GetFullPath(filePath));
            observer.Start();

            // open the file stream with shared read/write permissions
            // (so file can be written to while open)
            fileStream = new FileStream(Path.GetFullPath(filePath), FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            fileStreamReader = new StreamReader(fileStream, Encoding.UTF8);

            // read the initial contents of the file and send it to console as first msg
            string text = fileStreamReader.ReadToEnd();
            Self.Tell(new InitialRead(filePath, text));
        }

        protected override void OnReceive(object message)
        {
            if (message is FileWrite)
            {
                string text = fileStreamReader.ReadToEnd();
                if (!String.IsNullOrEmpty(text))
                {
                    reporterActor.Tell(text);
                }
            }
            else if (message is FileError)
            {
                FileError msg = message as FileError;
                reporterActor.Tell(string.Format("Tail error: {0}", msg.Reason));
            }
            else if (message is InitialRead)
            {
                InitialRead msg = message as InitialRead;
                Self.Tell(msg);
            }
        }
    }
}
