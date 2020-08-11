﻿// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Numerics;
using System.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Health.Abstractions.Features.Transactions;
using Microsoft.Health.Fhir.Core.Configs;
using Microsoft.Health.Fhir.Core.Features.Definition;
using Microsoft.Health.Fhir.Core.Features.Operations;
using Microsoft.Health.Fhir.Core.Features.Persistence;
using Microsoft.Health.Fhir.Core.Features.Search.Registry;
using Microsoft.Health.Fhir.Core.Models;
using Microsoft.Health.Fhir.SqlServer.Features.Schema;
using Microsoft.Health.Fhir.SqlServer.Features.Schema.Model;
using Microsoft.Health.Fhir.SqlServer.Features.Storage;
using Microsoft.Health.Fhir.SqlServer.Features.Storage.Registry;
using Microsoft.Health.Fhir.Tests.Common.Extensions;
using Microsoft.Health.SqlServer.Configs;
using Microsoft.Health.SqlServer.Features.Client;
using Microsoft.Health.SqlServer.Features.Schema;
using Microsoft.Health.SqlServer.Features.Storage;
using Xunit;
using Task = System.Threading.Tasks.Task;

namespace Microsoft.Health.Fhir.Tests.Integration.Persistence
{
    public class SqlServerFhirStorageTestsFixture : IServiceProvider, IAsyncLifetime
    {
        private const string LocalConnectionString = "server=(local);Integrated Security=true";

        private readonly string _databaseName;
        private readonly IFhirDataStore _fhirDataStore;
        private readonly IFhirOperationDataStore _fhirOperationDataStore;
        private readonly SqlServerFhirStorageTestHelper _testHelper;
        private readonly SchemaInitializer _schemaInitializer;
        private readonly FilebasedSearchParameterStatusDataStore _filebasedSearchParameterStatusDataStore;

        public SqlServerFhirStorageTestsFixture()
        {
            var initialConnectionString = Environment.GetEnvironmentVariable("SqlServer:ConnectionString") ?? LocalConnectionString;

            _databaseName = $"FHIRINTEGRATIONTEST_{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}_{BigInteger.Abs(new BigInteger(Guid.NewGuid().ToByteArray()))}";
            string masterConnectionString = new SqlConnectionStringBuilder(initialConnectionString) { InitialCatalog = "master" }.ToString();
            TestConnectionString = new SqlConnectionStringBuilder(initialConnectionString) { InitialCatalog = _databaseName }.ToString();

            var schemaOptions = new SqlServerSchemaOptions { AutomaticUpdatesEnabled = true };
            var config = new SqlServerDataStoreConfiguration { ConnectionString = TestConnectionString, Initialize = true, SchemaOptions = schemaOptions };

            var schemaInformation = new SchemaInformation(SchemaVersionConstants.Min, SchemaVersionConstants.Max);
            var scriptProvider = new ScriptProvider<SchemaVersion>();
            var schemaUpgradeRunner = new SchemaUpgradeRunner(scriptProvider, config, NullLogger<SchemaUpgradeRunner>.Instance);

            _schemaInitializer = new SchemaInitializer(config, schemaUpgradeRunner, schemaInformation, NullLogger<SchemaInitializer>.Instance);

            var searchParameterDefinitionManager = new SearchParameterDefinitionManager(ModelInfoProvider.Instance);
            _filebasedSearchParameterStatusDataStore = new FilebasedSearchParameterStatusDataStore(searchParameterDefinitionManager, ModelInfoProvider.Instance);

            var securityConfiguration = new SecurityConfiguration { PrincipalClaims = { "oid" } };

            var sqlServerFhirModel = new SqlServerFhirModel(config, _schemaInitializer, searchParameterDefinitionManager, () => _filebasedSearchParameterStatusDataStore, Options.Create(securityConfiguration), NullLogger<SqlServerFhirModel>.Instance);

            var serviceCollection = new ServiceCollection();
            serviceCollection.AddSqlServerTableRowParameterGenerators();
            serviceCollection.AddSingleton(sqlServerFhirModel);

            ServiceProvider serviceProvider = serviceCollection.BuildServiceProvider();

            var upsertResourceTvpGenerator = serviceProvider.GetRequiredService<VLatest.UpsertResourceTvpGenerator<ResourceMetadata>>();
            var upsertSearchParamsTvpGenerator = serviceProvider.GetRequiredService<VLatest.UpsertSearchParamsTvpGenerator<List<ResourceSearchParameterStatus>>>();

            var searchParameterToSearchValueTypeMap = new SearchParameterToSearchValueTypeMap(new SupportedSearchParameterDefinitionManager(searchParameterDefinitionManager));

            SqlTransactionHandler = new SqlTransactionHandler();
            SqlConnectionWrapperFactory = new SqlConnectionWrapperFactory(config, SqlTransactionHandler, new SqlCommandWrapperFactory());

            SqlServerSearchParameterStatusDataStore = new SqlServerSearchParameterStatusDataStore(
                () => SqlConnectionWrapperFactory.CreateMockScope(),
                upsertSearchParamsTvpGenerator);

            _fhirDataStore = new SqlServerFhirDataStore(config, sqlServerFhirModel, searchParameterToSearchValueTypeMap, upsertResourceTvpGenerator, Options.Create(new CoreFeatureConfiguration()), SqlConnectionWrapperFactory, NullLogger<SqlServerFhirDataStore>.Instance);

            _fhirOperationDataStore = new SqlServerFhirOperationDataStore(SqlConnectionWrapperFactory, NullLogger<SqlServerFhirOperationDataStore>.Instance);

            _testHelper = new SqlServerFhirStorageTestHelper(TestConnectionString, initialConnectionString, masterConnectionString, sqlServerFhirModel);
        }

        public string TestConnectionString { get; }

        internal SqlTransactionHandler SqlTransactionHandler { get; }

        internal SqlConnectionWrapperFactory SqlConnectionWrapperFactory { get; }

        internal SqlServerSearchParameterStatusDataStore SqlServerSearchParameterStatusDataStore { get; }

        public async Task InitializeAsync()
        {
            await _testHelper.CreateAndInitializeDatabase(_databaseName, forceIncrementalSchemaUpgrade: false, _schemaInitializer, CancellationToken.None);
        }

        public async Task DisposeAsync()
        {
            await _testHelper.DeleteDatabase(_databaseName, CancellationToken.None);
        }

        object IServiceProvider.GetService(Type serviceType)
        {
            if (serviceType == typeof(IFhirDataStore))
            {
                return _fhirDataStore;
            }

            if (serviceType == typeof(IFhirOperationDataStore))
            {
                return _fhirOperationDataStore;
            }

            if (serviceType == typeof(IFhirStorageTestHelper))
            {
                return _testHelper;
            }

            if (serviceType.IsInstanceOfType(this))
            {
                return this;
            }

            if (serviceType == typeof(ITransactionHandler))
            {
                return SqlTransactionHandler;
            }

            if (serviceType == typeof(ISearchParameterStatusDataStore))
            {
                return SqlServerSearchParameterStatusDataStore;
            }

            if (serviceType == typeof(FilebasedSearchParameterStatusDataStore))
            {
                return _filebasedSearchParameterStatusDataStore;
            }

            return null;
        }
    }
}
