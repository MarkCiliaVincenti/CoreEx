﻿// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx;
using CoreEx.Configuration;
using CoreEx.Database.SqlServer.Outbox;
using CoreEx.Hosting;
using CoreEx.Hosting.HealthChecks;
using Microsoft.Extensions.Logging;
using System;
using System.Text;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Provides <see cref="IServiceCollection"/> extension methods.
    /// </summary>
    public static class DatabaseServiceCollectionExtensions
    {
        /// <summary>
        /// Adds the <see cref="EventOutboxHostedService"/> using the <see cref="ServiceCollectionHostedServiceExtensions.AddHostedService{THostedService}(IServiceCollection)"/>. 
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/>.</param>
        /// <param name="eventOutboxDequeueFactory">The function to create an instance of <see cref="EventOutboxDequeueBase"/> (used to set the underlying <see cref="EventOutboxHostedService.EventOutboxDequeueFactory"/> property).</param>
        /// <param name="partitionKey">The optional partition key.</param>
        /// <param name="destination">The optional destination name (i.e. queue or topic).</param>
        /// <param name="healthCheck">Indicates whether a corresponding <see cref="TimerHostedServiceHealthCheck"/> should be configured.</param>
        /// <returns>The <see cref="IServiceCollection"/>.</returns>
        /// <remarks>To turn off the execution of the <see cref="EventOutboxHostedService"/>(s) at runtime set the '<c>EventOutboxHostedService:Enabled</c>' configuration setting to <c>false</c>.</remarks>
        public static IServiceCollection AddSqlServerEventOutboxHostedService(this IServiceCollection services, Func<IServiceProvider, EventOutboxDequeueBase> eventOutboxDequeueFactory, string? partitionKey = null, string? destination = null, bool healthCheck = true)
        {
            var exe = services.BuildServiceProvider().GetRequiredService<SettingsBase>().GetCoreExValue<bool?>("EventOutboxHostedService:Enabled");
            if (!exe.HasValue || exe.Value)
            {
                // Add the health check.
                var hc = healthCheck ? new TimerHostedServiceHealthCheck() : null;
                if (hc is not null)
                {
                    var sb = new StringBuilder("sql-server-event-outbox");
                    if (partitionKey is not null)
                        sb.Append($"-PartitionKey-{partitionKey}");

                    if (destination is not null)
                        sb.Append($"-Destination-{destination}");

                    services.AddHealthChecks().AddCheck(sb.ToString(), hc);
                }

                // Add the hosted service with the health check where applicable.
                services.AddHostedService(sp => new EventOutboxHostedService(sp, sp.GetRequiredService<ILogger<EventOutboxHostedService>>(), sp.GetRequiredService<SettingsBase>(), sp.GetRequiredService<IServiceSynchronizer>(), hc, partitionKey, destination)
                {
                    EventOutboxDequeueFactory = eventOutboxDequeueFactory.ThrowIfNull(nameof(eventOutboxDequeueFactory))
                });
            }

            return services;
        }
    }
}