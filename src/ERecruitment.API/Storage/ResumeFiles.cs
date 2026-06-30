namespace ERecruitment.API.Storage;

/// <summary>
/// Resolves on-disk locations for candidate resume files. Resumes live under
/// wwwroot/uploads/{tenantId}/candidates/{candidateId}/ but are intentionally NOT
/// served as static files — they are streamed only through authenticated,
/// tenant-scoped controller endpoints. This helper centralises path building and
/// guards against path traversal.
/// </summary>
public static class ResumeFiles
{
    public static string UploadsRoot =>
        Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads"));

    /// <summary>
    /// Returns the absolute path to a candidate's resume file, or null if the
    /// inputs are invalid or the resolved path would escape the uploads root.
    /// </summary>
    public static string? Resolve(Guid tenantId, Guid candidateId, string? fileName)
    {
        if (tenantId == Guid.Empty || candidateId == Guid.Empty || string.IsNullOrWhiteSpace(fileName))
            return null;

        var root = UploadsRoot;
        var safeName = Path.GetFileName(fileName); // strip any directory component
        var full = Path.GetFullPath(Path.Combine(
            root, tenantId.ToString(), "candidates", candidateId.ToString(), safeName));

        // Path-traversal guard: the resolved path must stay under the uploads root.
        return full.StartsWith(root, StringComparison.OrdinalIgnoreCase) ? full : null;
    }
}
