// =====================================================================
// Custom code for the "Leave API" custom connector
// =====================================================================
// DOCUMENTED RULES (learn.microsoft.com/en-us/connectors/custom-connectors/write-code):
//   - The class name MUST be "Script".
//   - It MUST implement (inherit from) the abstract base class "ScriptBase".
//   - It MUST implement "ExecuteAsync", which is called at runtime.
//   - Only ONE script file per custom connector.
//   - Max 2 minutes execution, max 1 MB script file, .NET Standard 2.0.
//   - Only the documented "supported namespaces" may be used
//     (System.Net.Http, Newtonsoft.Json.Linq, etc.).
//
// WHY THIS SCRIPT EXISTS (design):
//   The connector host (leaveapi.example.com) is a placeholder.
//   Microsoft Learn (define-blank, Step 4) states that when code is used,
//   "the code will execute, and we don't send the request to the back end".
//   So for GetLeaveTypes we BUILD the response ourselves (a mock),
//   which means the Test tab can return real data with no live API.
//
// NOTE: The documented samples show no "using" lines, so none are added
// here. If the portal reports a compile error, send the full text.
// =====================================================================

public class Script : ScriptBase
{
    // ExecuteAsync is the ONLY entry point the runtime calls.
    // Exam: transformation code goes HERE - not inside SendAsync.
    public override async Task<HttpResponseMessage> ExecuteAsync()
    {
        // Context.OperationId tells us WHICH action the maker called.
        // It matches the operationId in the OpenAPI definition.
        string operationId = this.Context.OperationId;

        // DOCUMENTED KNOWN ISSUE: in certain regions OperationId may arrive
        // base64 encoded. This decode-if-possible pattern is taken from the
        // "General known issues and limitations" section of write-code.
        try
        {
            byte[] data = Convert.FromBase64String(this.Context.OperationId);
            operationId = System.Text.Encoding.UTF8.GetString(data);
        }
        catch (FormatException)
        {
            // Not base64 - keep the original value. "GetLeaveTypes" is 13
            // characters, which is not valid base64 length, so it lands here.
        }

        // ROUTE 1: GetLeaveTypes -> build the response in code (no backend call)
        if (operationId == "GetLeaveTypes")
        {
            return this.HandleGetLeaveTypes();
        }

        // ROUTE 2: every other operation -> forward the request unchanged.
        // Context.SendAsync is the DOCUMENTED way to call the backend
        // ("use this method to send requests instead of HttpClient.SendAsync").
        // SendAsync returns the backend response AS-IS; it does not transform.
        // (These operations will still fail at runtime because the host is
        //  fictitious - that is expected.)
        HttpResponseMessage forwarded = await this.Context
            .SendAsync(this.Context.Request, this.CancellationToken)
            .ConfigureAwait(false);
        return forwarded;
    }

    // Builds the same JSON shape GetLeaveTypes declares in the Swagger:
    // { "value": [ { "code": 4001, "name": "Annual Leave" }, ... ] }
    // Keeping the SAME shape matters: x-ms-dynamic-values  reads
    // value-collection = value, value-path = code, value-title = name.
    private HttpResponseMessage HandleGetLeaveTypes()
    {
        // Codes are the real Dataverse choice values of the
        // global choice "Leave Types" (cto_leavetype).
        JObject output = new JObject
        {
            ["value"] = new JArray
            {
                new JObject { ["code"] = 4001, ["name"] = "Annual Leave" },
                new JObject { ["code"] = 4002, ["name"] = "Sick Leave" },
                new JObject { ["code"] = 4003, ["name"] = "Unpaid Leave" },
                new JObject { ["code"] = 4004, ["name"] = "Maternity/Paternity" }
            }
        };

        // 200 OK + JSON body. CreateJsonContent is the documented ScriptBase
        // helper that turns serialized JSON into StringContent.
        HttpResponseMessage response = new HttpResponseMessage(HttpStatusCode.OK);
        response.Content = CreateJsonContent(output.ToString());
        return response;
    }
}
