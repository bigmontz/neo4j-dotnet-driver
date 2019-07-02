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
using System.Diagnostics;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;

namespace Neo4j.Driver.Internal
{
    internal class InternalRxSession : IRxSession
    {
        private readonly IReactiveSession _session;
        private readonly IRxRetryLogic _retryLogic;

        public InternalRxSession(IReactiveSession session, IRxRetryLogic retryLogic)
        {
            _session = session;
            _retryLogic = retryLogic;
        }

        public String LastBookmark => _session.LastBookmark;

        #region Run Methods

        public IRxResult Run(string statement)
        {
            return Run(statement, null);
        }

        public IRxResult Run(string statement, object parameters)
        {
            return Run(new Statement(statement, parameters.ToDictionary()), null);
        }

        public IRxResult Run(Statement statement)
        {
            return Run(statement, null);
        }

        public IRxResult Run(string statement, TransactionConfig txConfig)
        {
            return Run(new Statement(statement), txConfig);
        }

        public IRxResult Run(string statement, object parameters, TransactionConfig txConfig)
        {
            return Run(new Statement(statement, parameters.ToDictionary()), txConfig);
        }

        public IRxResult Run(Statement statement, TransactionConfig txConfig)
        {
            return new InternalRxResult(Observable.FromAsync(() => _session.RunAsync(statement, txConfig))
                .Cast<IReactiveStatementResultCursor>());
        }

        #endregion

        #region BeginTransaction Methods

        public IObservable<IRxTransaction> BeginTransaction()
        {
            return BeginTransaction(null);
        }

        public IObservable<IRxTransaction> BeginTransaction(TransactionConfig txConfig)
        {
            return Observable.FromAsync(() => _session.BeginTransactionAsync(txConfig))
                .Select(tx =>
                    new InternalRxTransaction(tx));
        }

        private IObservable<IRxTransaction> BeginTransaction(AccessMode mode, TransactionConfig txConfig)
        {
            return Observable.FromAsync(() => _session.BeginTransactionAsync(mode, txConfig))
                .Select(tx =>
                    new InternalRxTransaction(tx));
        }

        #endregion

        #region Transaction Functions

        public IObservable<T> ReadTransaction<T>(Func<IRxTransaction, IObservable<T>> work)
        {
            return ReadTransaction(work, TransactionConfig.Empty);
        }

        public IObservable<T> ReadTransaction<T>(Func<IRxTransaction, IObservable<T>> work,
            TransactionConfig txConfig)
        {
            return RunTransaction(AccessMode.Read, work, txConfig);
        }

        public IObservable<T> WriteTransaction<T>(Func<IRxTransaction, IObservable<T>> work)
        {
            return WriteTransaction(work, TransactionConfig.Empty);
        }

        public IObservable<T> WriteTransaction<T>(Func<IRxTransaction, IObservable<T>> work,
            TransactionConfig txConfig)
        {
            return RunTransaction(AccessMode.Write, work, txConfig);
        }

        internal IObservable<T> RunTransaction<T>(AccessMode mode,
            Func<IRxTransaction, IObservable<T>> work,
            TransactionConfig txConfig)
        {
            return _retryLogic.Retry(
                BeginTransaction(mode, txConfig)
                    .SelectMany(txc =>
                        Observable.Defer(() =>
                        {
                            try
                            {
                                return work(txc);
                            }
                            catch (Exception exc)
                            {
                                return Observable.Throw<T>(exc);
                            }
                        }).CatchAndThrow(exc => txc.Rollback<T>()).Concat(txc.Commit<T>()))
            );
        }

        #endregion

        #region Cleanup

        public IObservable<T> Close<T>()
        {
            return Observable.FromAsync(() => _session.CloseAsync()).SelectMany(x => Observable.Empty<T>());
        }

        #endregion
    }
}