﻿// <copyright file="AspNetMetricsAppBuilderExtensions.cs" company="Allan Hardy">
// Copyright (c) Allan Hardy. All rights reserved.
// </copyright>

using System;
using System.Threading.Tasks;
using App.Metrics;
using App.Metrics.Configuration;
using App.Metrics.DependencyInjection.Internal;
using App.Metrics.Extensions.Middleware;
using App.Metrics.Extensions.Middleware.DependencyInjection.Options;
using App.Metrics.Reporting.Abstractions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

// ReSharper disable CheckNamespace
namespace Microsoft.AspNetCore.Builder
{
    // ReSharper restore CheckNamespace
    public static class AspNetMetricsAppBuilderExtensions
    {
        /// <summary>
        ///     Adds App Metrics Middleware to the <see cref="T:Microsoft.AspNetCore.Builder.IApplicationBuilder" /> request
        ///     execution pipeline.
        /// </summary>
        /// <param name="app">The <see cref="T:Microsoft.AspNetCore.Builder.IApplicationBuilder" />.</param>
        /// <returns>A reference to this instance after the operation has completed.</returns>
        /// <exception cref="System.ArgumentNullException">
        ///     <see cref="T:Microsoft.AspNetCore.Builder.IApplicationBuilder" /> cannot be null
        /// </exception>
        public static IApplicationBuilder UseMetrics(this IApplicationBuilder app)
        {
            if (app == null)
            {
                throw new ArgumentNullException(nameof(app));
            }

            // Verify if AddMetrics was done before calling UseMetrics
            // We use the MetricsMarkerService to make sure if all the services were added.
            MetricsServicesHelper.ThrowIfMetricsNotRegistered(app.ApplicationServices);

            var appMetricsOptions = app.ApplicationServices.GetRequiredService<AppMetricsOptions>();
            var aspNetMetricsOptions = app.ApplicationServices.GetRequiredService<AspNetMetricsOptions>();

            if (aspNetMetricsOptions.PingEndpointEnabled)
            {
                app.UseMiddleware<PingEndpointMiddleware>();
            }

            if (aspNetMetricsOptions.HealthEndpointEnabled)
            {
                HealthServicesHelper.ThrowIfMetricsNotRegistered(app.ApplicationServices);

                app.UseMiddleware<HealthCheckEndpointMiddleware>();
            }

            if (aspNetMetricsOptions.MetricsTextEndpointEnabled && appMetricsOptions.MetricsEnabled)
            {
                app.UseMiddleware<MetricsEndpointTextEndpointMiddleware>();
            }

            if (aspNetMetricsOptions.MetricsEndpointEnabled && appMetricsOptions.MetricsEnabled)
            {
                app.UseMiddleware<MetricsEndpointMiddleware>();
            }

            if (aspNetMetricsOptions.EnvironmentInfoEndpointEnabled)
            {
                app.UseMiddleware<EnvironmentInfoMiddleware>();
            }

            if (appMetricsOptions.MetricsEnabled && aspNetMetricsOptions.DefaultTrackingEnabled)
            {
                app.UseMiddleware<ActiveRequestCounterEndpointMiddleware>();
                app.UseMiddleware<ErrorRequestMeterMiddleware>();
                app.UseMiddleware<PerRequestTimerMiddleware>();
                app.UseMiddleware<OAuthTrackingMiddleware>();
                app.UseMiddleware<PostAndPutRequestSizeHistogramMiddleware>();
                app.UseMiddleware<RequestTimerMiddleware>();
            }

            if (appMetricsOptions.MetricsEnabled && aspNetMetricsOptions.ApdexTrackingEnabled)
            {
                app.UseMiddleware<ApdexMiddleware>();
            }

            return app;
        }

        /// <summary>
        ///     Runs the configured App Metrics Reporting options once the application has started.
        /// </summary>
        /// <param name="app">The <see cref="T:Microsoft.AspNetCore.Builder.IApplicationBuilder" />.</param>
        /// <param name="lifetime">The <see cref="T:Microsoft.AspNetCore.Hosting.IApplicationLifetime" />.</param>
        /// <returns>
        ///     A reference to this instance after the operation has completed.
        /// </returns>
        /// <exception cref="System.ArgumentNullException">
        ///     <see cref="T:Microsoft.AspNetCore.Builder.IApplicationBuilder" /> &amp;
        ///     <see cref="T:Microsoft.AspNetCore.Hosting.IApplicationLifetime" /> cannot be null
        /// </exception>
        public static IApplicationBuilder UseMetricsReporting(this IApplicationBuilder app, IApplicationLifetime lifetime)
        {
            if (app == null)
            {
                throw new ArgumentNullException(nameof(app));
            }

            if (lifetime == null)
            {
                throw new ArgumentNullException(nameof(lifetime));
            }

            var reportFactory = app.ApplicationServices.GetRequiredService<IReportFactory>();
            var metrics = app.ApplicationServices.GetRequiredService<IMetrics>();
            var reporter = reportFactory.CreateReporter();

            lifetime.ApplicationStarted.Register(() => { Task.Run(() => reporter.RunReports(metrics, lifetime.ApplicationStopping), lifetime.ApplicationStopping); });

            return app;
        }
    }
}