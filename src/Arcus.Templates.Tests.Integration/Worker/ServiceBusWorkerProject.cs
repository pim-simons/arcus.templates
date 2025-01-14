﻿using System.Threading.Tasks;
using Arcus.Templates.Tests.Integration.Fixture;
using Arcus.Templates.Tests.Integration.Worker.Configuration;
using Arcus.Templates.Tests.Integration.Worker.Fixture;
using Arcus.Templates.Tests.Integration.Worker.ServiceBus;
using GuardNet;
using Microsoft.Extensions.Logging;
using Xunit.Abstractions;

namespace Arcus.Templates.Tests.Integration.Worker
{
    /// <summary>
    /// Project template to create Azure ServiceBus Queue worker projects.
    /// </summary>
    public class ServiceBusWorkerProject : WorkerProject
    {
        private ServiceBusWorkerProject(
            ServiceBusEntityType entityType,
            TestConfig configuration,
            TestServiceBusMessageProducer messageProducer,
            ITestOutputHelper outputWriter)
            : base(configuration.GetServiceBusProjectDirectory(entityType), 
                   configuration, 
                   messageProducer,
                   outputWriter)
        {
        }

        /// <summary>
        /// Starts a newly created project from the ServiceBus Queue worker project template.
        /// </summary>
        /// <param name="outputWriter">The output logger to add telemetry information during the creation and startup process.</param>
        /// <returns>
        ///     A ServiceBus Queue project with a set of services to interact with the worker.
        /// </returns>
        public static async Task<ServiceBusWorkerProject> StartNewWithQueueAsync(ITestOutputHelper outputWriter)
        {
            Guard.NotNull(outputWriter, nameof(outputWriter));

            var config = TestConfig.Create();
            var options = ServiceBusWorkerProjectOptions.Create(config);
            ServiceBusWorkerProject project = await StartNewWithQueueAsync(config, options, outputWriter);
            
            return project;
        }

        /// <summary>
        /// Starts a newly created project from the ServiceBus Queue worker project template.
        /// </summary>
        /// <param name="config">The collection of configuration values to correctly initialize the resulting project with secret values.</param>
        /// <param name="options">The project options to manipulate the resulting structure of the project.</param>
        /// <param name="outputWriter">The output logger to add telemetry information during the creation and startup process.</param>
        /// <returns>
        ///     A ServiceBus Queue project with a set of services to interact with the worker.
        /// </returns>
        public static async Task<ServiceBusWorkerProject> StartNewWithQueueAsync(
            TestConfig config,
            ServiceBusWorkerProjectOptions options,
            ITestOutputHelper outputWriter)
        {
            Guard.NotNull(config, nameof(config));
            Guard.NotNull(options, nameof(options));
            Guard.NotNull(outputWriter, nameof(outputWriter));

            ServiceBusWorkerProject project = await StartNewAsync(ServiceBusEntityType.Queue, config, options, outputWriter);
            return project;
        }

        /// <summary>
        /// Starts a newly created project from the ServiceBus Queue worker project template.
        /// </summary>
        /// <param name="outputWriter">The output logger to add telemetry information during the creation and startup process.</param>
        /// <returns>
        ///     A ServiceBus Queue project with a set of services to interact with the worker.
        /// </returns>
        public static async Task<ServiceBusWorkerProject> StartNewWithTopicAsync(ITestOutputHelper outputWriter)
        {
            Guard.NotNull(outputWriter, nameof(outputWriter));

            var config = TestConfig.Create();
            var options = ServiceBusWorkerProjectOptions.Create(config);
            ServiceBusWorkerProject project = await StartNewWithTopicAsync(config, options, outputWriter);
            
            return project;
        }

        /// <summary>
        /// Starts a newly created project from the ServiceBus Queue worker project template.
        /// </summary>
        /// <param name="config">The collection of configuration values to correctly initialize the resulting project with secret values.</param>
        /// <param name="options">The project options to manipulate the resulting structure of the project.</param>
        /// <param name="outputWriter">The output logger to add telemetry information during the creation and startup process.</param>
        /// <returns>
        ///     A ServiceBus Queue project with a set of services to interact with the worker.
        /// </returns>
        public static async Task<ServiceBusWorkerProject> StartNewWithTopicAsync(
            TestConfig config,
            ServiceBusWorkerProjectOptions options,
            ITestOutputHelper outputWriter)
        {
            Guard.NotNull(config, nameof(config));
            Guard.NotNull(options, nameof(options));
            Guard.NotNull(outputWriter, nameof(outputWriter));

            ServiceBusWorkerProject project = await StartNewAsync(ServiceBusEntityType.Topic, config, options, outputWriter);
            return project;
        }

        /// <summary>
        /// Starts a newly created project from the ServiceBus Queue or Topic worker project template.
        /// </summary>
        /// <param name="entityType">The resource entity for which the worker template should be created, you can also use <see cref="StartNewWithQueueAsync(ITestOutputHelper)"/> or <see cref="StartNewWithTopicAsync(ITestOutputHelper)"/> instead.</param>
        /// <param name="configuration">The collection of configuration values to correctly initialize the resulting project with secret values.</param>
        /// <param name="options">The project options to manipulate the resulting structure of the project.</param>
        /// <param name="outputWriter">The output logger to add telemetry information during the creation and startup process.</param>
        /// <returns>
        ///     A ServiceBus Queue project with a set of services to interact with the worker.
        /// </returns>
        public static async Task<ServiceBusWorkerProject> StartNewAsync(
            ServiceBusEntityType entityType, 
            TestConfig configuration, 
            ServiceBusWorkerProjectOptions options, 
            ITestOutputHelper outputWriter)
        {
            ServiceBusWorkerProject project = CreateNew(entityType, configuration, options, outputWriter);

            EventGridConfig eventGridConfig = configuration.GetEventGridConfig();
            string serviceBusConnection = configuration.GetServiceBusConnectionString(entityType);

            await project.StartAsync(options, 
                CommandArgument.CreateSecret("EVENTGRID_TOPIC_URI", eventGridConfig.TopicUri),
                CommandArgument.CreateSecret("EVENTGRID_AUTH_KEY", eventGridConfig.AuthenticationKey),
                CommandArgument.CreateSecret("ARCUS_SERVICEBUS_CONNECTIONSTRING", serviceBusConnection));

            return project;
        }

        private static ServiceBusWorkerProject CreateNew(
            ServiceBusEntityType entityType, 
            TestConfig configuration, 
            ServiceBusWorkerProjectOptions options,
            ITestOutputHelper outputWriter)
        {
            string connectionString = configuration.GetServiceBusConnectionString(entityType);
            var producer = new TestServiceBusMessageProducer(connectionString);
            var project = new ServiceBusWorkerProject(entityType, configuration, producer, outputWriter);

            project.CreateNewProject(options);
            project.AddTestMessageHandler();

            return project;
        }

        private void AddTestMessageHandler()
        {
            AddPackage("Arcus.EventGrid", "3.2.0");
            AddPackage("Arcus.EventGrid.Publishing", "3.2.0");
            AddTypeAsFile<Order>();
            AddTypeAsFile<Customer>();
            AddTypeAsFile<OrderCreatedEvent>();
            AddTypeAsFile<OrderCreatedEventData>();
            AddTypeAsFile<TestOrdersAzureServiceBusMessageHandler>();
            
            UpdateFileInProject("Program.cs", contents => 
                RemovesUserErrorsFromContents(contents)
                    .Replace(".MinimumLevel.Debug()", ".MinimumLevel.Verbose()")
                    .Replace("EmptyMessageHandler", nameof(TestOrdersAzureServiceBusMessageHandler))
                    .Replace("EmptyMessage", nameof(Order))
                    .Replace("stores.AddAzureKeyVaultWithManagedIdentity(\"https://your-keyvault.vault.azure.net/\", CacheConfiguration.Default);", ""));
        }
    }
}
