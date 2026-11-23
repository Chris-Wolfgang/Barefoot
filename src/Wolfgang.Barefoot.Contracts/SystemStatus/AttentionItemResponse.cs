namespace Wolfgang.Barefoot.Contracts.SystemStatus;

/// <summary>
/// One thing the Admin should look at: a row of the System page's "Needs attention" list, which
/// links to the page that puts it right (DESIGN §12).
/// </summary>
/// <param name="Title">What is wrong, in a few words.</param>
/// <param name="Detail">What is known about it, in a few words.</param>
/// <param name="Target">The page the row links to.</param>
public sealed record AttentionItemResponse
(
    string Title,
    string Detail,
    AttentionTarget Target
);
