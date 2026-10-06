using System;
using Microsoft.Xrm.Sdk;

namespace Contoso.LeaveManagement.Plugins
{
    /// <summary>
    /// Step: cto_leaverequest, message Create, stage PreValidation, synchronous.
    /// Rejects requests longer than a maximum number of days.
    /// The maximum comes from UNSECURE config; a "policy code" comes from SECURE config.
    /// The same class can be registered twice with different limits: that is the point
    /// of configuration data.
    /// </summary>
    public class ValidateMaxLeaveDays : IPlugin
    {
        // Config values are fixed PER REGISTERED STEP, so they are safe in readonly fields.
        // What must never live in fields is SERVICE instances and CONTEXT data, which
        // change on every call. These strings do not.
        private readonly string _unsecureConfig;
        private readonly string _secureConfig;

        // Three constructors are supported: (), (string unsecure),
        // (string unsecure, string secure). Dataverse picks this one when both are supplied.
        public ValidateMaxLeaveDays(string unsecure, string secure)
        {
            _unsecureConfig = unsecure;
            _secureConfig = secure;
        }

        public void Execute(IServiceProvider serviceProvider)
        {
            ITracingService tracing = (ITracingService)
                serviceProvider.GetService(typeof(ITracingService));
            IPluginExecutionContext context = (IPluginExecutionContext)
                serviceProvider.GetService(typeof(IPluginExecutionContext));

            // Safe to trace: unsecure config is not a secret by definition.
            tracing.Trace("Unsecure config = '{0}'", _unsecureConfig ?? "(null)");

            // NEVER trace the secure value itself. Trace logs are readable by others,
            // which would defeat the purpose of putting it in the secure store.
            tracing.Trace("Secure config supplied: {0}",
                string.IsNullOrEmpty(_secureConfig) ? "NO" : "YES");

            // int.TryParse returns false instead of throwing if the text is not a number.
            // Defensive because config is typed by a human in the registration tool.
            int maxDays;
            if (!int.TryParse(_unsecureConfig, out maxDays))
            {
                tracing.Trace("Unsecure config is not a whole number. Skipping validation.");
                return;
            }

            if (!(context.InputParameters.Contains("Target") &&
                  context.InputParameters["Target"] is Entity))
            {
                return;
            }

            Entity target = (Entity)context.InputParameters["Target"];

            if (!target.Contains("cto_startdate") || !target.Contains("cto_enddate"))
            {
                tracing.Trace("Dates not supplied. Nothing to validate.");
                return;
            }

            DateTime start = (DateTime)target["cto_startdate"];
            DateTime end   = (DateTime)target["cto_enddate"];
            int requestedDays = (end.Date - start.Date).Days + 1;

            tracing.Trace("Requested {0} day(s). Configured maximum is {1}.",
                requestedDays, maxDays);

            if (requestedDays > maxDays)
            {
                throw new InvalidPluginExecutionException(
                    string.Format(
                        "Leave requests cannot exceed {0} days. You requested {1} days. " +
                        "Please split the request or contact HR.",
                        maxDays, requestedDays));
            }

            tracing.Trace("Within the configured maximum.");
        }
    }
}
