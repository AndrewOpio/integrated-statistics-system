using Microsoft.Data.SqlClient;

namespace VMSService.Tests.Withdrawals;

/// <summary>
/// Reads the live local WorkflowService/IdentityService databases directly, the same way ticket
/// 01's rebuild was verified — these tests exist to catch exactly the kind of drift that produced
/// the ApproveExceptional/RejectExceptional role bug and the ApproveToRTGSVefification typo.
/// </summary>
internal static class WorkflowConfigReader
{
    private const string WorkflowConnectionString =
        "Server=(localdb)\\mssqllocaldb;Database=Aswam_WorkflowDB;Trusted_Connection=True;MultipleActiveResultSets=true";

    private const string IdentityConnectionString =
        "Server=(localdb)\\mssqllocaldb;Database=IdentityDB;Trusted_Connection=True;MultipleActiveResultSets=true;Encrypt=false";

    public static List<(string ActionName, int RequiredRole, string ToStatus)> GetTransitionsFrom(string fromStatusName)
    {
        using var conn = new SqlConnection(WorkflowConnectionString);
        conn.Open();
        using var cmd = new SqlCommand(@"
            SELECT wt.ActionName, wt.RequiredRole, ts.Name AS ToStatus
            FROM WorkflowTransitions wt
            JOIN StatusTypes fs ON wt.FromStatusId = fs.Id
            JOIN StatusTypes ts ON wt.ToStatusId = ts.Id
            JOIN EntityTypes et ON wt.EntityTypeId = et.Id
            WHERE et.Name = 'Withdrawal Request' AND fs.Name = @FromStatus AND wt.IsDeleted = 0", conn);
        cmd.Parameters.AddWithValue("@FromStatus", fromStatusName);

        using var reader = cmd.ExecuteReader();
        var result = new List<(string, int, string)>();
        while (reader.Read())
            result.Add((reader.GetString(0), reader.GetInt32(1), reader.GetString(2)));
        return result;
    }

    public static int GetRoleId(string roleName)
    {
        using var conn = new SqlConnection(IdentityConnectionString);
        conn.Open();
        using var cmd = new SqlCommand(@"
            SELECT ar.Id
            FROM ApplicationRoles ar
            JOIN AspNetRoles r ON ar.RoleId = r.Id
            WHERE r.Name = @RoleName", conn);
        cmd.Parameters.AddWithValue("@RoleName", roleName);

        var result = cmd.ExecuteScalar()
            ?? throw new InvalidOperationException($"Role '{roleName}' not found in IdentityDB.");
        return (int)result;
    }

    public static int CountTransitionsReferencingStatus(string statusName)
    {
        using var conn = new SqlConnection(WorkflowConnectionString);
        conn.Open();
        using var cmd = new SqlCommand(@"
            SELECT COUNT(*)
            FROM WorkflowTransitions wt
            JOIN StatusTypes fs ON wt.FromStatusId = fs.Id
            JOIN StatusTypes ts ON wt.ToStatusId = ts.Id
            JOIN EntityTypes et ON wt.EntityTypeId = et.Id
            WHERE et.Name = 'Withdrawal Request' AND (fs.Name = @StatusName OR ts.Name = @StatusName)", conn);
        cmd.Parameters.AddWithValue("@StatusName", statusName);

        return (int)cmd.ExecuteScalar()!;
    }
}
