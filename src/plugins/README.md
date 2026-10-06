# Plugins

C# plugins for the Leave Request table (assembly `Contoso.LeaveManagement.Plugins`).

| Class | Message / stage | What it does |
|---|---|---|
| `CalculateLeaveDays` | Create, PreOperation, sync | Sets `cto_totaldays` on the row before it's saved (server-side source of truth) |
| `ValidateMaxLeaveDays` | Create, PreValidation, sync | Rejects requests longer than a maximum set in the step's unsecure configuration |
| `ApproveLeaveRequest` | Custom API `cto_ApproveLeaveRequest` | Sets the request to Approved, stamps the approval date, returns `Success` and `ResultMessage`. **Source to be added in the next release.** |

## Next version (in progress)

- [ ] Register both table plugins on **Update** too, with filtering attributes (`cto_startdate`, `cto_enddate`) and a **PreImage**, because on Update the Target holds only the changed columns.
- [ ] Read dates with `GetAttributeValue<DateTime?>()` so a cleared date can't cause a crash.
- [ ] Change the negative-span check to `totalDays < 1` (the column's minimum is 1).
- [ ] Switch the date columns to **Date Only** behaviour, so UTC conversion can't shift the day count.
- [ ] Rename the namespace to `LeaveManagement.Plugins` and add the assembly and its steps to the solution.
