﻿// Copyright (c) 2002-2019 "Neo4j,"
// Neo4j Sweden AB [http://neo4j.com]
// 
// This file is part of Neo4j.
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Neo4j.Driver;
using System.Threading.Tasks;
using Neo4j.Driver.Internal.Metrics;

namespace Neo4j.Driver.Internal
{
    internal class Driver : IDriver
    {
        private int _closedMarker = 0;

        private readonly IConnectionProvider _connectionProvider;
        private readonly IRetryLogic _retryLogic;
        private readonly IDriverLogger _logger;
        private readonly IMetrics _metrics;
        private readonly SyncExecutor _syncExecutor;

        public Uri Uri { get; }

        private const AccessMode DefaultAccessMode = AccessMode.Write;
        private const string NullBookmark = null;

        internal Driver(Uri uri, IConnectionProvider connectionProvider, IRetryLogic retryLogic, IDriverLogger logger,
            SyncExecutor syncExecutor, IMetrics metrics = null)
        {
            Throw.ArgumentNullException.IfNull(connectionProvider, nameof(connectionProvider));
            Throw.ArgumentNullException.IfNull(syncExecutor, nameof(syncExecutor));

            Uri = uri;
            _logger = logger;
            _connectionProvider = connectionProvider;
            _retryLogic = retryLogic;
            _metrics = metrics;
            _syncExecutor = syncExecutor;
        }

        private bool IsClosed => _closedMarker > 0;

        public ISession Session()
        {
            return Session(DefaultAccessMode);
        }

        public ISession Session(AccessMode defaultMode)
        {
            return Session(defaultMode, NullBookmark);
        }

        public ISession Session(string bookmark)
        {
            return Session(DefaultAccessMode, bookmark);
        }


        public ISession Session(AccessMode defaultMode, string bookmark)
        {
            return Session(defaultMode, string.IsNullOrEmpty(bookmark) ? Enumerable.Empty<string>() : new[] {bookmark},
                false);
        }


        public ISession Session(AccessMode defaultMode, IEnumerable<string> bookmarks)
        {
            return Session(defaultMode, bookmarks, false);
        }

        public ISession Session(IEnumerable<string> bookmarks)
        {
            return Session(AccessMode.Write, bookmarks);
        }

        internal ISession Session(AccessMode defaultMode, IEnumerable<string> bookmarks, bool reactive)
        {
            if (IsClosed)
            {
                ThrowDriverClosedException();
            }

            var session = new Session(_connectionProvider, _logger, _syncExecutor, _retryLogic, defaultMode,
                Bookmark.From(bookmarks), reactive);

            if (IsClosed)
            {
                session.Dispose();
                ThrowDriverClosedException();
            }

            return session;
        }

        public void Close()
        {
            _syncExecutor.RunSync(CloseAsync);
        }

        public Task CloseAsync()
        {
            if (Interlocked.CompareExchange(ref _closedMarker, 1, 0) == 0)
            {
                return _connectionProvider.CloseAsync();
            }

            return TaskHelper.GetCompletedTask();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (IsClosed)
                return;

            if (disposing)
            {
                Close();
            }
        }

        private void ThrowDriverClosedException()
        {
            throw new ObjectDisposedException(GetType().Name,
                "Cannot open a new session on a driver that is already disposed.");
        }

        internal IMetrics GetMetrics()
        {
            if (_metrics == null)
            {
                throw new InvalidOperationException(
                    "Cannot access driver metrics if it is not enabled when creating this driver.");
            }

            return _metrics;
        }
    }
}