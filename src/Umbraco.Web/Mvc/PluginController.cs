﻿using System;
using System.Collections.Concurrent;
using System.Web.Mvc;
using LightInject;
using Umbraco.Core;
using Umbraco.Core.Cache;
using Umbraco.Core.Logging;
using Umbraco.Core.Persistence;
using Umbraco.Core.Composing;
using Umbraco.Core.Services;
using Umbraco.Web.Security;
using Umbraco.Web.WebApi;

namespace Umbraco.Web.Mvc
{
    /// <summary>
    /// Provides a base class for plugin controllers.
    /// </summary>
    public abstract class PluginController : Controller, IDiscoverable
    {
        private static readonly ConcurrentDictionary<Type, PluginControllerMetadata> MetadataStorage
            = new ConcurrentDictionary<Type, PluginControllerMetadata>();

        private UmbracoHelper _umbracoHelper;

        // for debugging purposes
        internal Guid InstanceId { get; } = Guid.NewGuid();

        // note
        // properties marked as [Inject] below will be property-injected (vs constructor-injected) in
        // order to keep the constuctor as light as possible, so that ppl implementing eg a SurfaceController
        // don't need to implement complex constructors + need to refactor them each time we change ours.
        // this means that these properties have a setter.
        // what can go wrong?

        /// <summary>
        /// Gets or sets the Umbraco context.
        /// </summary>
        [Inject]
        public virtual UmbracoContext UmbracoContext { get; set; }

        /// <summary>
        /// Gets or sets the database context.
        /// </summary>
        [Inject]
        public IUmbracoDatabaseFactory DatabaseFactory { get; set; }

        /// <summary>
        /// Gets or sets the services context.
        /// </summary>
        [Inject]
        public ServiceContext Services { get; set; }

        /// <summary>
        /// Gets or sets the application cache.
        /// </summary>
        [Inject]
        public CacheHelper ApplicationCache { get; set;  }

        /// <summary>
        /// Gets or sets the logger.
        /// </summary>
        [Inject]
        public ILogger Logger { get; set; }

        /// <summary>
        /// Gets or sets the profiling logger.
        /// </summary>
        [Inject]
        public ProfilingLogger ProfilingLogger { get; set; }

        /// <summary>
        /// Gets the membership helper.
        /// </summary>
        public MembershipHelper Members => Umbraco.MembershipHelper;

        /// <summary>
        /// Gets the Umbraco helper.
        /// </summary>
        public UmbracoHelper Umbraco
        {
            get
            {
                return _umbracoHelper
                    ?? (_umbracoHelper = new UmbracoHelper(UmbracoContext, Services));
            }
            internal set // tests
            {
                _umbracoHelper = value;
            }
        }

        /// <summary>
        /// Gets metadata for this instance.
        /// </summary>
        internal PluginControllerMetadata Metadata => GetMetadata(GetType());

        /// <summary>
        /// Gets metadata for a controller type.
        /// </summary>
        /// <param name="controllerType">The controller type.</param>
        /// <returns>Metadata for the controller type.</returns>
        internal static PluginControllerMetadata GetMetadata(Type controllerType)
        {
            return MetadataStorage.GetOrAdd(controllerType, type =>
            {
                // plugin controller? back-office controller?
                var pluginAttribute = controllerType.GetCustomAttribute<PluginControllerAttribute>(false);
                var backOfficeAttribute = controllerType.GetCustomAttribute<IsBackOfficeAttribute>(true);

                return new PluginControllerMetadata
                {
                    AreaName = pluginAttribute?.AreaName,
                    ControllerName = ControllerExtensions.GetControllerName(controllerType),
                    ControllerNamespace = controllerType.Namespace,
                    ControllerType = controllerType,
                    IsBackOffice = backOfficeAttribute != null
                };
            });
        }
    }
}
