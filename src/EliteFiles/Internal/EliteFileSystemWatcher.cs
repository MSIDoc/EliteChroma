﻿using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;

namespace EliteFiles.Internal
{
    internal sealed class EliteFileSystemWatcher : IDisposable
    {
        private readonly FileSystemWatcher _watcher;
        private readonly object _lock = new object();

        private bool _started;
        private bool _disposed;

        public EliteFileSystemWatcher(string path)
            : this(path, "*.*")
        {
        }

        public EliteFileSystemWatcher(FileInfo file)
            : this(file.DirectoryName, file.Name)
        {
        }

        public EliteFileSystemWatcher(string path, string filter)
        {
            _watcher = new FileSystemWatcher(path, filter)
            {
                NotifyFilter = NotifyFilters.LastWrite,
                InternalBufferSize = 4096,
                EnableRaisingEvents = false,
            };

            _watcher.Changed += Watcher_Changed;
        }

        public event EventHandler<FileSystemEventArgs> Changed;

        public string Filter
        {
            get { return _watcher.Filter; }
            set { _watcher.Filter = value; }
        }

        public void Start()
        {
            if (_started)
            {
                return;
            }

            lock (_lock)
            {
                _watcher.EnableRaisingEvents = true;
                _started = true;
            }
        }

        public void Stop()
        {
            if (!_started)
            {
                return;
            }

            lock (_lock)
            {
                _watcher.EnableRaisingEvents = false;
                _started = false;
            }
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            Stop();
            _watcher.Dispose();
            _disposed = true;
        }

        private void Watcher_Changed(object sender, FileSystemEventArgs e)
        {
            // Reference: https://stackoverflow.com/questions/12940516/why-does-filesystemwatcher-fire-twice/12940774#12940774
            lock (_lock)
            {
                _watcher.EnableRaisingEvents = false;
            }

            try
            {
                OnChanged(e);
            }
            finally
            {
                lock (_lock)
                {
                    if (_started)
                    {
                        _watcher.EnableRaisingEvents = true;
                    }
                }
            }
        }

        [ExcludeFromCodeCoverage]
        private void OnChanged(FileSystemEventArgs e)
        {
            Changed?.Invoke(this, e);
        }
    }
}
