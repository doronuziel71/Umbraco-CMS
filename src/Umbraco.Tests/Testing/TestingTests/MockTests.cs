﻿using System;
using System.Globalization;
using System.Linq;
using System.Web.Security;
using Moq;
using NUnit.Framework;
using Umbraco.Core;
using Umbraco.Core.Cache;
using Umbraco.Core.Dictionary;
using Umbraco.Core.Models;
using Umbraco.Core.Models.PublishedContent;
using Umbraco.Core.Services;
using Umbraco.Tests.TestHelpers;
using Umbraco.Tests.TestHelpers.Stubs;
using Umbraco.Tests.Testing.Objects.Accessors;
using Umbraco.Web;
using Umbraco.Web.Routing;
using Umbraco.Web.Security;
using Current = Umbraco.Web.Composing.Current;

namespace Umbraco.Tests.Testing.TestingTests
{
    [TestFixture]
    public class MockTests : UmbracoTestBase
    {
        public override void SetUp()
        {
            base.SetUp();

            Current.UmbracoContextAccessor = new TestUmbracoContextAccessor();
        }

        [Test]
        public void Can_Mock_Service_Context()
        {
            // ReSharper disable once UnusedVariable
            var serviceContext = TestObjects.GetServiceContextMock();
            Assert.Pass();
        }

        [Test]
        public void Can_Mock_Umbraco_Context()
        {
            var umbracoContext = TestObjects.GetUmbracoContextMock(Current.UmbracoContextAccessor);
            Assert.AreEqual(umbracoContext, UmbracoContext.Current);
        }

        [Test]
        public void Can_Mock_Umbraco_Helper()
        {
            var umbracoContext = TestObjects.GetUmbracoContextMock();

            // unless we can inject them in MembershipHelper, we need need this
            Container.Register(_ => Mock.Of<IMemberService>());
            Container.Register(_ => Mock.Of<IMemberTypeService>());
            Container.Register(_ => Mock.Of<IUserService>());
            Container.Register(_ => CacheHelper.CreateDisabledCacheHelper());
            Container.Register<ServiceContext>();

            // ReSharper disable once UnusedVariable
            var helper = new UmbracoHelper(umbracoContext,
                Mock.Of<IPublishedContent>(),
                Mock.Of<ITagQuery>(),
                Mock.Of<ICultureDictionary>(),
                Mock.Of<IUmbracoComponentRenderer>(),
                new MembershipHelper(umbracoContext, Mock.Of<MembershipProvider>(), Mock.Of<RoleProvider>()),
                new ServiceContext());
            Assert.Pass();
        }

        [Test]
        public void Can_Mock_UrlProvider()
        {
            var umbracoContext = TestObjects.GetUmbracoContextMock();

            var urlProviderMock = new Mock<IUrlProvider>();
            urlProviderMock.Setup(provider => provider.GetUrl(It.IsAny<UmbracoContext>(), It.IsAny<IPublishedContent>(), It.IsAny<UrlProviderMode>(), It.IsAny<string>(), It.IsAny<Uri>()))
                .Returns("/hello/world/1234");
            var urlProvider = urlProviderMock.Object;

            var theUrlProvider = new UrlProvider(umbracoContext, new [] { urlProvider }, umbracoContext.VariationContextAccessor);

            var contentType = new PublishedContentType(666, "alias", PublishedItemType.Content, Enumerable.Empty<string>(), Enumerable.Empty<PublishedPropertyType>(), ContentVariation.Nothing);
            var publishedContent = Mock.Of<IPublishedContent>();
            Mock.Get(publishedContent).Setup(x => x.ContentType).Returns(contentType);

            Assert.AreEqual("/hello/world/1234", theUrlProvider.GetUrl(publishedContent));
        }
    }
}
