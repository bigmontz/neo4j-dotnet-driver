// Copyright (c) 2002-2019 "Neo4j,"
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

namespace Neo4j.Driver
{
    /// <summary>
    /// The interface that defines options applicable to session constructions. It could either be populated by a POCO
    /// approach or by the provided builder-style methods.
    /// </summary>
    public class SessionConfig
    {
        private AccessMode _defaultAccessMode;
        private string _database;
        private IEnumerable<Bookmark> _bookmarks;

        internal SessionConfig()
        {
            _defaultAccessMode = AccessMode.Write;
            _database = null;
            _bookmarks = null;
        }

        /// <summary>
        /// The database that the constructed session will connect to.
        ///
        /// <remarks>
        /// When used against servers supporting multi-databases, it is recommended that this value to be set explicitly
        /// either through this property or <see cref="ForDatabase"/> method. If not, then the session will connect to the
        /// default database configured on the server side.
        ///
        /// When used against servers that don't support multi-databases, this property should be left unset.
        /// </remarks>
        /// </summary>
        /// <exception cref="set_Database">throws <see cref="System.ArgumentNullException"/> when provided database name
        /// is null or an empty string.</exception>
        public string Database
        {
            get => _database;
            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    throw new ArgumentNullException();
                }

                _database = value;
            }
        }

        /// <summary>
        /// Set the type of access required by the constructed session.
        ///
        /// This is used to route the requests originating from this session instance to the correct server in a clustered
        /// environment.
        ///
        /// <remarks>The default access mode set is overriden when transaction functions (i.e.
        /// <see cref="IAsyncSession.ReadTransactionAsync{T}(System.Func{Neo4j.Driver.IAsyncTransaction,System.Threading.Tasks.Task{T}})"/> and
        /// <see cref="IAsyncSession.WriteTransactionAsync{T}(System.Func{Neo4j.Driver.IAsyncTransaction,System.Threading.Tasks.Task{T}})"/> is
        /// used (with corresponding access modes derived from invoked method name).
        /// </remarks>
        /// </summary>
        public AccessMode DefaultAccessMode
        {
            get => _defaultAccessMode;
            set => _defaultAccessMode = value;
        }

        /// <summary>
        /// Set the initial bookmarks to be used by the constructed session.
        ///
        /// The first transaction (either auto-commit or explicit) will ensure that the executing server is at least
        /// up to date to the point identified by the latest of the provided initial bookmarks. The bookmarks can be
        /// obtained from <see cref="IAsyncSession.LastBookmark"/> (and corresponding properties in other types of
        /// sessions, i.e. IRxSession or ISession.
        /// </summary>
        public IEnumerable<Bookmark> Bookmarks
        {
            get => _bookmarks;
            set => _bookmarks = value ?? throw new ArgumentNullException();
        }

        /// <summary>
        /// Sets the database the constructed session will connect to.
        /// </summary>
        /// <param name="database">the database name</param>
        /// <returns>this ISessionOptions instance</returns>
        /// <seealso cref="Database"/>
        public SessionConfig ForDatabase(string database)
        {
            Database = database;
            return this;
        }

        /// <summary>
        /// Sets the type of access required by the constructed session.
        /// </summary>
        /// <param name="defaultAccessMode">the access mode</param>
        /// <returns>this ISessionOptions instance</returns>
        /// <seealso cref="DefaultAccessMode"/>
        public SessionConfig WithDefaultAccessMode(AccessMode defaultAccessMode)
        {
            DefaultAccessMode = defaultAccessMode;
            return this;
        }

        /// <summary>
        /// Sets the initial bookmarks to be used by the constructed session.
        /// </summary>
        /// <param name="bookmarks">the initial bookmarks</param>
        /// <returns>this ISessionOptions instance</returns>
        /// <seealso cref="Bookmarks"/>
        public SessionConfig WithBookmarks(params Bookmark[] bookmarks)
        {
            Bookmarks = bookmarks;
            return this;
        }
    }
}