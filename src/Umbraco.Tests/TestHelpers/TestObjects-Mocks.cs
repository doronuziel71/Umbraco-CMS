﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Web;
using LightInject;
using Moq;
using Umbraco.Core;
using Umbraco.Core.Configuration;
using Umbraco.Core.Configuration.UmbracoSettings;
using Umbraco.Core.Events;
using Umbraco.Core.Logging;
using Umbraco.Core.Models;
using Umbraco.Core.Persistence;
using Umbraco.Core.Services;
using Umbraco.Tests.TestHelpers.Stubs;
using Umbraco.Tests.Testing.Objects.Accessors;
using Umbraco.Web;
using Umbraco.Web.PublishedCache;
using Umbraco.Web.Routing;
using Umbraco.Web.Security;

namespace Umbraco.Tests.TestHelpers
{
    /// <summary>
    /// Provides objects for tests.
    /// </summary>
    internal partial class TestObjects
    {
        /// <summary>
        /// Gets a mocked IUmbracoDatabaseFactory.
        /// </summary>
        /// <returns>An IUmbracoDatabaseFactory.</returns>
        /// <param name="configured">A value indicating whether the factory is configured.</param>
        /// <param name="canConnect">A value indicating whether the factory can connect to the database.</param>
        /// <remarks>This is just a void factory that has no actual database.</remarks>
        public IUmbracoDatabaseFactory GetDatabaseFactoryMock(bool configured = true, bool canConnect = true)
        {
            var databaseFactoryMock = new Mock<IUmbracoDatabaseFactory>();
            databaseFactoryMock.Setup(x => x.Configured).Returns(configured);
            databaseFactoryMock.Setup(x => x.CanConnect).Returns(canConnect);
            databaseFactoryMock.Setup(x => x.SqlContext).Returns(Mock.Of<ISqlContext>());

            // can create a database - but don't try to use it!
            if (configured && canConnect)
                databaseFactoryMock.Setup(x => x.CreateDatabase()).Returns(GetUmbracoSqlCeDatabase(Mock.Of<ILogger>()));

            return databaseFactoryMock.Object;
        }

        /// <summary>
        /// Gets a mocked service context built with mocked services.
        /// </summary>
        /// <returns>A ServiceContext.</returns>
        public ServiceContext GetServiceContextMock(IServiceFactory container = null)
        {
            return new ServiceContext(
                MockService<IContentService>(),
                MockService<IMediaService>(),
                MockService<IContentTypeService>(),
                MockService<IMediaTypeService>(),
                MockService<IDataTypeService>(),
                MockService<IFileService>(),
                MockService<ILocalizationService>(),
                MockService<IPackagingService>(),
                MockService<IEntityService>(),
                MockService<IRelationService>(),
                MockService<IMemberGroupService>(),
                MockService<IMemberTypeService>(),
                MockService<IMemberService>(),
                MockService<IUserService>(),
                MockService<ISectionService>(),
                MockService<IApplicationTreeService>(),
                MockService<ITagService>(),
                MockService<INotificationService>(),
                MockService<ILocalizedTextService>(),
                MockService<IAuditService>(),
                MockService<IDomainService>(),
                MockService<IMacroService>());
        }

        private T MockService<T>(IServiceFactory container = null)
            where T : class
        {
            return container?.TryGetInstance<T>() ?? new Mock<T>().Object;
        }

        /// <summary>
        /// Gets an opened database connection that can begin a transaction.
        /// </summary>
        /// <returns>A DbConnection.</returns>
        /// <remarks>This is because NPoco wants a DbConnection, NOT an IDbConnection,
        /// and DbConnection is hard to mock so we create our own class here.</remarks>
        public DbConnection GetDbConnection()
        {
            return new MockDbConnection();
        }

        /// <summary>
        /// Gets an Umbraco context.
        /// </summary>
        /// <returns>An Umbraco context.</returns>
        /// <remarks>This should be the minimum Umbraco context.</remarks>
        public UmbracoContext GetUmbracoContextMock(IUmbracoContextAccessor accessor = null)
        {
            var httpContext = Mock.Of<HttpContextBase>();

            var publishedSnapshotMock = new Mock<IPublishedSnapshot>();
            publishedSnapshotMock.Setup(x => x.Members).Returns(Mock.Of<IPublishedMemberCache>());
            var publishedSnapshot = publishedSnapshotMock.Object;
            var publishedSnapshotServiceMock = new Mock<IPublishedSnapshotService>();
            publishedSnapshotServiceMock.Setup(x => x.CreatePublishedSnapshot(It.IsAny<string>())).Returns(publishedSnapshot);
            var publishedSnapshotService = publishedSnapshotServiceMock.Object;

            var umbracoSettings = GetUmbracoSettings();
            var globalSettings = GetGlobalSettings();
            var webSecurity = new Mock<WebSecurity>(null, null, globalSettings).Object;
            var urlProviders = Enumerable.Empty<IUrlProvider>();

            if (accessor == null) accessor = new TestUmbracoContextAccessor();
            return UmbracoContext.EnsureContext(accessor, httpContext, publishedSnapshotService, webSecurity, umbracoSettings, urlProviders, globalSettings, new TestVariationContextAccessor(), true);
        }

        public IUmbracoSettingsSection GetUmbracoSettings()
        {
            //fixme Why not use the SettingsForTest.GenerateMock ... ?
            //fixme Shouldn't we use the default ones so they are the same instance for each test?

            var umbracoSettingsMock = new Mock<IUmbracoSettingsSection>();
            var webRoutingSectionMock = new Mock<IWebRoutingSection>();
            webRoutingSectionMock.Setup(x => x.UrlProviderMode).Returns(UrlProviderMode.Auto.ToString());
            umbracoSettingsMock.Setup(x => x.WebRouting).Returns(webRoutingSectionMock.Object);
            return umbracoSettingsMock.Object;
        }

        public IGlobalSettings GetGlobalSettings()
        {
            return SettingsForTests.GetDefaultGlobalSettings();
        }

        #region Inner classes

        private class MockDbConnection : DbConnection
        {
            protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel)
            {
                return Mock.Of<DbTransaction>(); // enough here
            }

            public override void Close()
            {
                throw new NotImplementedException();
            }

            public override void ChangeDatabase(string databaseName)
            {
                throw new NotImplementedException();
            }

            public override void Open()
            {
                throw new NotImplementedException();
            }

            public override string ConnectionString { get; set; }

            protected override DbCommand CreateDbCommand()
            {
                throw new NotImplementedException();
            }

            public override string Database { get; }
            public override string DataSource { get; }
            public override string ServerVersion { get; }
            public override ConnectionState State => ConnectionState.Open; // else NPoco reopens
        }

        public class TestDataTypeService : IDataTypeService
        {
            public TestDataTypeService()
            {
                DataTypes = new Dictionary<int, IDataType>();
            }

            public TestDataTypeService(params IDataType[] dataTypes)
            {
                DataTypes = dataTypes.ToDictionary(x => x.Id, x => x);
            }

            public TestDataTypeService(IEnumerable<IDataType> dataTypes)
            {
                DataTypes = dataTypes.ToDictionary(x => x.Id, x => x);
            }

            public Dictionary<int, IDataType> DataTypes { get; }

            public Attempt<OperationResult<OperationResultType, EntityContainer>> CreateContainer(int parentId, string name, int userId = -1)
            {
                throw new NotImplementedException();
            }

            public Attempt<OperationResult> SaveContainer(EntityContainer container, int userId = -1)
            {
                throw new NotImplementedException();
            }

            public EntityContainer GetContainer(int containerId)
            {
                throw new NotImplementedException();
            }

            public EntityContainer GetContainer(Guid containerId)
            {
                throw new NotImplementedException();
            }

            public IEnumerable<EntityContainer> GetContainers(string folderName, int level)
            {
                throw new NotImplementedException();
            }

            public IEnumerable<EntityContainer> GetContainers(IDataType dataType)
            {
                throw new NotImplementedException();
            }

            public IEnumerable<EntityContainer> GetContainers(int[] containerIds)
            {
                throw new NotImplementedException();
            }

            public Attempt<OperationResult> DeleteContainer(int containerId, int userId = -1)
            {
                throw new NotImplementedException();
            }

            public Attempt<OperationResult<OperationResultType, EntityContainer>> RenameContainer(int id, string name, int userId = -1)
            {
                throw new NotImplementedException();
            }

            public IDataType GetDataType(string name)
            {
                throw new NotImplementedException();
            }

            public IDataType GetDataType(int id)
            {
                DataTypes.TryGetValue(id, out var dataType);
                return dataType;
            }

            public IDataType GetDataType(Guid id)
            {
                throw new NotImplementedException();
            }

            public IEnumerable<IDataType> GetAll(params int[] ids)
            {
                if (ids.Length == 0) return DataTypes.Values;
                return ids.Select(x => DataTypes.TryGetValue(x, out var dataType) ? dataType : null).WhereNotNull();
            }

            public void Save(IDataType dataType, int userId = -1)
            {
                throw new NotImplementedException();
            }

            public void Save(IEnumerable<IDataType> dataTypeDefinitions, int userId = -1)
            {
                throw new NotImplementedException();
            }

            public void Save(IEnumerable<IDataType> dataTypeDefinitions, int userId, bool raiseEvents)
            {
                throw new NotImplementedException();
            }

            public void Delete(IDataType dataType, int userId = -1)
            {
                throw new NotImplementedException();
            }

            public IEnumerable<IDataType> GetByEditorAlias(string propertyEditorAlias)
            {
                throw new NotImplementedException();
            }

            public Attempt<OperationResult<MoveOperationStatusType>> Move(IDataType toMove, int parentId)
            {
                throw new NotImplementedException();
            }
        }

        #endregion
    }
}
