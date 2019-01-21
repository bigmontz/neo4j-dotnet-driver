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
using Neo4j.Driver.IntegrationTests.Internals;
using Neo4j.Driver.IntegrationTests.Shared;
using Neo4j.Driver.Internal;
using Neo4j.Driver;
using Xunit;
using Xunit.Abstractions;

namespace Neo4j.Driver.IntegrationTests
{
    [Collection(CCIntegrationCollection.CollectionName)]
    public abstract class RoutingDriverTestBase : IDisposable
    {
        protected ITestOutputHelper Output { get; }
        protected CausalCluster Cluster { get; }
        protected IAuthToken AuthToken { get; }

        protected string RoutingServer => Cluster.AnyCore().BoltRoutingUri.ToString();
        protected string WrongServer => "bolt+routing://localhost:1234";
        protected IDriver Driver { get; }

        public RoutingDriverTestBase(ITestOutputHelper output, CausalClusterIntegrationTestFixture fixture)
        {
            Output = output;
            Cluster = fixture.Cluster;
            AuthToken = Cluster.AuthToken;

            var config = new Config
            {
                DriverLogger = new TestDriverLogger(output)
            };
            Driver = GraphDatabase.Driver(RoutingServer, AuthToken, config);
        }

        public virtual void Dispose()
        {
            Driver.Close();
            // put some code that you want to run after each unit test
        }
    }
}
