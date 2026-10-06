using System;
using Microsoft.Xrm.Sdk;

namespace Contoso.LeaveManagement.Plugins
{
    /// <summary>
    /// Step: cto_leaverequest, message Create, stage PreOperation, synchronous.
    /// Calculates cto_totaldays from the start and end dates.
    /// WHY PreOperation: value changes for the row in the message belong here.
    /// The modified Target is what gets written, so there is NO second save.
    /// </summary>
    public class CalculateLeaveDays : IPlugin
    {
        public void Execute(IServiceProvider serviceProvider)
        {
            // Services are locals, never fields: plugin instances are cached and shared.
            ITracingService tracing = (ITracingService)
                serviceProvider.GetService(typeof(ITracingService));
            IPluginExecutionContext context = (IPluginExecutionContext)
                serviceProvider.GetService(typeof(IPluginExecutionContext));

            tracing.Trace("CalculateLeaveDays START | Message={0} | Table={1} | Stage={2}",
                context.MessageName, context.PrimaryEntityName, context.Stage);

            if (!(context.InputParameters.Contains("Target") &&
                  context.InputParameters["Target"] is Entity))
            {
                tracing.Trace("No Entity Target. Exiting.");
                return;
            }

            Entity target = (Entity)context.InputParameters["Target"];

            // Both dates must be present. A blank column is simply absent from Target.
            if (!target.Contains("cto_startdate") || !target.Contains("cto_enddate"))
            {
                tracing.Trace("Dates missing. Cannot calculate. Exiting.");
                return;
            }

            DateTime start = (DateTime)target["cto_startdate"];
            DateTime end   = (DateTime)target["cto_enddate"];

            // .Date strips the time part, so 8:00 AM vs 5:00 PM can't skew the count.
            // .Days gives whole days between them; +1 makes the range INCLUSIVE
            // (19 Sep to 19 Sep = 1 day, not 0).
            int totalDays = (end.Date - start.Date).Days + 1;

            // Safety net: a separate validation rejects end < start, but this class must
            // not depend on another step's existence. Never write a negative day count.
            if (totalDays < 0)
            {
                tracing.Trace("Negative span ({0}). Not setting cto_totaldays.", totalDays);
                return;
            }

            // Modify the Target itself. Dataverse saves this modified row.
            target["cto_totaldays"] = totalDays;

            tracing.Trace("cto_totaldays set to {0}. CalculateLeaveDays DONE.", totalDays);
        }
    }
}
