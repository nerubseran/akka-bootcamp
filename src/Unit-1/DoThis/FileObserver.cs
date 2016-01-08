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
    /// Turns <see cref="FileSystemWatcher"/> events about a specific file into
    /// messages for <see cref="TailActor"/>.
    /// </summary>
    public class FileObserver : IDisposable
    {
        private readonly IActorRef tailActor;
        private readonly string absoluteFilePath;
        private FileSystemWatcher watcher;
        private readonly string fileDir;
        private readonly string fileNameOnly;

        public FileObserver(IActorRef tailActor, string absoluteFilePath)
        {
            this.tailActor = tailActor;
            this.absoluteFilePath = absoluteFilePath;
            this.fileDir = Path.GetDirectoryName(absoluteFilePath);
            this.fileNameOnly = Path.GetFileName(absoluteFilePath);
        }

        
        /// <summary>
        /// Begin monitoring file.
        /// </summary>
        public void Start()
        {
            // make watcher to observe our specific file
            watcher = new FileSystemWatcher(fileDir, fileNameOnly);

            // watch our file for changes to the file name, or new messages being written to file
            watcher.NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite;
            
            // assign callbacks for event types
            //FileSystemEventHandler(object sender, FileSystemEventArgs e)
            watcher.Changed += OnFileChanged;
            watcher.Error += OnWatcherError;

            // start watching
            watcher.EnableRaisingEvents = true;
        }

        /// <summary>
        /// Stop monitoring file.
        /// </summary>
        public void Dispose()
        {
            watcher.Dispose();
        }

        /// <summary>
        /// Callback for <see cref="FileSystemWatcher"/> file error events.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnWatcherError(object sender, ErrorEventArgs e)
        {
            tailActor.Tell(new TailActor.FileError(fileNameOnly, e.GetException().Message), ActorRefs.NoSender);
        }

        /// <summary>
        /// Callback for <see cref="FileSystemWatcher"/> file change events.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnFileChanged(object sender, FileSystemEventArgs e)
        {
            // here we use a special ActorRefs.NoSender
            // since this event can happen many times,
            // this is a little microoptimization
            tailActor.Tell(new TailActor.FileWrite(fileNameOnly), ActorRefs.NoSender);
        }

    }
}
