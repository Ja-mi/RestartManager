﻿// <copyright file="RestartManagerSession.cs" company="Heath Stewart">
// Copyright (c) 2017 Heath Stewart
// See the LICENSE file in the project root for more information.
// </copyright>

namespace RestartManager
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Diagnostics.Contracts;
    using System.Linq;

    /// <summary>
    /// A Windows Restart Manager session.
    /// </summary>
    public class RestartManagerSession : IDisposable
    {
        private readonly IServiceProvider services;
        private IRestartManagerService restartManagerService = null;

        /// <summary>
        /// Initializes a new instance of the <see cref="RestartManagerSession"/> class.
        /// </summary>
        /// <param name="services">Services to provide this object.</param>
        /// <param name="sessionId">The session identity.</param>
        /// <param name="sessionKey">The session key.</param>
        /// <exception cref="ArgumentNullException"><paramref name="services"/> is null.</exception>
        private RestartManagerSession(IServiceProvider services, int sessionId, string sessionKey)
        {
            Contract.Requires(services != null);
            this.services = services;

            SessionId = sessionId;
            SessionKey = sessionKey;
        }

        /// <summary>
        /// Finalizes an instance of the <see cref="RestartManagerSession"/> class.
        /// </summary>
        ~RestartManagerSession()
        {
            Dispose(false);
        }

        /// <summary>
        /// Gets the session identity.
        /// </summary>
        public int SessionId { get; }

        /// <summary>
        /// Gets the session key.
        /// </summary>
        public string SessionKey { get; }

        /// <summary>
        /// Gets a value indicating whether this object is disposed.
        /// </summary>
        internal bool IsDisposed { get; private set; }

        /// <summary>
        /// Gets a value indicating whether any processes are registered.
        /// </summary>
        internal bool IsRegistered { get; private set; }

        private IRestartManagerService RestartManagerService => services.GetService(ref restartManagerService, throwIfNotDefined: true);

        /// <inheritdoc/>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Creates and starts a new <see cref="RestartManagerSession"/>.
        /// </summary>
        /// <param name="services">Services to create and start a new <see cref="RestartManagerSession"/>.</param>
        /// <returns>A new <see cref="RestartManagerSession"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="services"/> is null.</exception>
        /// <exception cref="NotImplementedException">No implementation of the <see cref="IRestartManagerService"/> service interface was found.</exception>
        internal static RestartManagerSession Create(IServiceProvider services)
        {
            Validate.NotNull(services, nameof(services));

            var restartManagerService = services.GetService<IRestartManagerService>() ?? WindowsRestartManagerService.Default;
            var error = restartManagerService.StartSession(out var sessionId, out var sessionKey);
            ThrowOnError(error);

            return new RestartManagerSession(services, sessionId, sessionKey);
        }

        /// <summary>
        /// Throws a <see cref="Win32Exception"/> if <paramref name="error"/> is an error.
        /// </summary>
        /// <param name="error">A Win32 error code.</param>
        /// <exception cref="Win32Exception"><paramref name="error"/> is an error.</exception>
        internal static void ThrowOnError(int error)
        {
            if (error == NativeMethods.ERROR_OUTOFMEMORY)
            {
                throw new OutOfMemoryException();
            }
            else if (error != NativeMethods.ERROR_SUCCESS)
            {
                throw new Win32Exception(error);
            }
        }

        /// <summary>
        /// Registers resources with the Restart Manager.
        /// </summary>
        /// <param name="files">Optional collection of file paths.</param>
        /// <param name="processes">Optional collection of processes.</param>
        /// <param name="services">Optional collection of service names.</param>
        /// <exception cref="ObjectDisposedException">This object has already been disposed.</exception>
        /// <exception cref="Win32Exception">An error occured.</exception>
        internal void Register(IEnumerable<string> files = null, IEnumerable<Process> processes = null, IEnumerable<string> services = null)
        {
            ThrowIfDisposed();

            if (!files.NullOrEmpty() || !processes.NullOrEmpty() || !services.NullOrEmpty())
            {
                IsRegistered = true;

                var uniqueProcesses = processes?.Cast<RM_UNIQUE_PROCESS>();
                var error = RestartManagerService.Register(SessionId, files, uniqueProcesses, services);
                ThrowOnError(error);
            }
        }

        /// <summary>
        /// Throws an <see cref="ObjectDisposedException"/> if this object is disposed.
        /// </summary>
        /// <exception cref="ObjectDisposedException"><see cref="IsDisposed"/> is true.</exception>
        internal void ThrowIfDisposed()
        {
            if (IsDisposed)
            {
                throw new ObjectDisposedException(nameof(RestartManagerSession));
            }
        }

        private void Dispose(bool disposing)
        {
            if (IsDisposed)
            {
                return;
            }

            RestartManagerService.EndSession(SessionId);
            IsDisposed = true;
        }
    }
}