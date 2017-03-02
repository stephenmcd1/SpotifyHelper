using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using SpotifyAPI.Local;
using Timer = System.Windows.Forms.Timer;

namespace SpotifyHelper
{
    /// <summary>
    /// The main driver of our application.
    /// </summary>
    public class AppContext : ApplicationContext
    {
        /// <summary>
        /// The list of modules that are currently running.  This will contain long-running and transient ones if Spotify is running
        /// otherwise just the long-running ones.
        /// </summary>
        private readonly List<ISimpleModule> _runningModules;

        /// <summary>
        /// The <see cref="Type"/>s of transient modules
        /// </summary>
        private readonly List<Type> _transientModuleTypes;

        /// <summary>
        /// The current instance we found (if any)
        /// </summary>
        private SpotifyLocalAPI _currentInstance;

        /// <summary>
        /// A WinForms timer used to poll for new Spotify instances
        /// </summary>
        private readonly Timer _timer;

        public AppContext()
        {
            //Look for all the modules in our assembly
            var moduleTypes = Assembly.GetCallingAssembly()
                .GetAvailableTypes()
                .Where(t => t.IsClass && !t.IsAbstract && typeof(ISimpleModule).IsAssignableFrom(t))
                .ToList();

            //Filter the modules to find the transient ones
            _transientModuleTypes = moduleTypes.Where(t => !typeof(ILongRunningModule).IsAssignableFrom(t)).ToList();

            //For the rest (long-running ones), go ahead and create them right away
            //TODO: Should these be created later in case any of the constructors here do things that need a message pump setup?
            _runningModules = moduleTypes
                .Except(_transientModuleTypes)
                .Select(t => (ISimpleModule)Activator.CreateInstance(t))
                .ToList();

            //Before we exit, do some cleanup
            Application.ApplicationExit += (sender, args) =>
            {
                foreach (var module in _runningModules)
                {
                    module.Dispose();
                }
                _timer.Dispose();
            };

            //Setup our timer
            _timer = new Timer {Interval = 500};
            _timer.Tick += (sender, args) =>
            {
                Tick();
            };
            _timer.Enabled = true;
        }

        private void Tick()
        {
            //If we already have an instance, don't bother doing anything
            if (_currentInstance != null)
                return;

            //Look for a new instance
            var instance = SpotifyInstance.TryFind(out _currentInstance);

            //If we didn't find one, just bail and wait for the next tick of our timer
            if (instance == null)
                return;

            //No point in looking for more instances since we found it
            _timer.Enabled = false;

            //Setup the event for when Spotify exits
            var proc = instance.Process;
            proc.EnableRaisingEvents = true;
            proc.Exited += (sender, args) =>
            {
                //Notify all the modules
                foreach (var service in _runningModules.ToList())
                {
                    //Long running instances just get told of the closed instance but stick around
                    var longRunning = service as ILongRunningModule;
                    if (longRunning != null)
                    {
                        longRunning.InstanceClosed(instance);
                    }
                    else
                    {
                        //Transient ones are cleaned up and thrown away
                        service.Dispose();
                        _runningModules.Remove(service);
                    }
                }

                //Then finally cleanup the API object and restart our timer
                _currentInstance.Dispose();
                _currentInstance = null;
                _timer.Enabled = true;
            };

            //Forward Play State Changed events to the relevant modules
            _currentInstance.OnPlayStateChange += (sender, args) =>
            {
                foreach (var service in _runningModules.OfType<IPlayStateChanged>())
                {
                    service.OnPlayStateChanged(args);
                }
            };

            //Forward Track Changed events to the relevant modules
            _currentInstance.OnTrackChange += (sender, args) =>
            {
                foreach (var service in _runningModules.OfType<ITrackChanged>())
                {
                    service.OnTrackChanged(args.NewTrack);
                }
            };

            //Notify the long running modules that a new instance has been found
            foreach (var service in _runningModules.Cast<ILongRunningModule>())
            {
                service.SpotifyAvailable(instance);
            }

            //Create new instances of the transient modules
            foreach (var type in _transientModuleTypes)
            {
                _runningModules.Add((ISimpleModule)Activator.CreateInstance(type, instance));
            }

            //Grab the current/initial status and pass that along to the modules
            var status = _currentInstance.GetStatus();

            foreach (var service in _runningModules.OfType<IPlayStateChanged>())
            {
                service.OnPlayStateChanged(new PlayStateEventArgs { Playing = status?.Playing ?? false });
            }

            foreach (var service in _runningModules.OfType<ITrackChanged>())
            {
                service.OnTrackChanged(status?.Track);
            }
        }
    }
}
