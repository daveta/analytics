// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Bot.Configuration;
using System;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Text;

namespace makeAppInsightsUrl
{

    /// <summary>
    /// Application Insights Export Query Helper.
    /// </summary>
    class ExportQueryHelper
    {
        private readonly BotConfiguration _botConfig;
        private readonly AppInsightsService _appInsights;

        // URL constants
        private const string PortalDomainName = "ms.portal.azure.com";
        private const string Options = "/isQueryEditorVisible/true";
        private const string BladeIdentifier = "/blade/Microsoft_OperationsManagementSuite_Workspace/AnalyticsBlade";
        private const string Initiator = "/initiator/AnalyticsShareLinkToQuery";

        /// <summary>
        /// Initializes the Application Insights Query helper with appropriate metadata.
        /// </summary>
        /// <param name="botConfig">The BotConfiguration for the bot.</param>
        /// <param name="appInsightsInstance">[Optional] Identifies an Application Insights instance name to use within the Bot Configuration.</param>
        public ExportQueryHelper(BotConfiguration botConfig, string appInsightsInstance = null)
        {
            _botConfig = botConfig ?? throw new ArgumentNullException(nameof(botConfig));

            if (!string.IsNullOrWhiteSpace(appInsightsInstance))
            {
                _appInsights = (AppInsightsService)botConfig.Services.Find(x => x.Name == appInsightsInstance && x.Type == ServiceTypes.AppInsights)
                    ?? throw new InvalidOperationException($"Application Insights `{appInsightsInstance}` not found in bot configuration.");
            }
            else
            {
                _appInsights = (AppInsightsService)botConfig.Services.Find(x => x.Type == ServiceTypes.AppInsights)
                    ?? throw new InvalidOperationException("No Application Insights resource found in bot configuration.");
            }

            // Add AAD tenant ID only if it is a valid Guid
            Guid aadTenantGuid;
            if (!Guid.TryParse(_appInsights.TenantId, out aadTenantGuid))
            {
                throw new InvalidOperationException("Application Insights tenant ID is invalid");
            }
        }

        /// <summary>
        /// Builds a URL that brings up Application Insights "Logs" blade and pre-populates query.
        /// </summary>
        /// <returns>A URL that brings up Application Insights "Logs" blade with pre-populated query.</returns>
        public string BuildNavigationUrl(string query)
        {
            // Build uri of correct format.
            string portalUri = $"https://{PortalDomainName}#@{_appInsights.TenantId}";
            portalUri += string.Format(CultureInfo.InvariantCulture, BladeIdentifier + Initiator + Options + Scope() + Query(query));
            return portalUri;
        }

        // Encode the query
        private string Query(string query)
        {
            return ($"/query/{ WebUtility.UrlEncode(CompressAndEncode(query)) }/isQueryBase64Compressed/true");
        }

        // Resource scope definition
        private string Scope()
        {
            return $"/scope/" + WebUtility.UrlEncode("{\"resources\":[{\"resourceId\":\"" + ResourceId() + "\"}]}");
        }

        // Gzip compress and then base64 encode text
        private string CompressAndEncode(string text)
        {
            var bytes = Encoding.UTF8.GetBytes(text);
            using (var memoryStream = new MemoryStream())
            {
                using (var zipStream = new GZipStream(memoryStream, CompressionMode.Compress))
                {
                    zipStream.Write(bytes, 0, bytes.Length);
                }
                return System.Convert.ToBase64String(memoryStream.ToArray());
            }
        }

        // Resource ID embedded in the scope.
        private string ResourceId()
        {
            return $"/subscriptions/{_appInsights.SubscriptionId}/resourcegroups/{_appInsights.ResourceGroup}/providers/microsoft.insights/components/{_appInsights.ServiceName}";
        }
    }
}
