// TokenAccount.cs
public class TokenAccount
{
    /// <summary>
    /// 企业微信id
    /// </summary>
    public string WeComID { get; set; }
    public string UserName { get; set; }
    public string BearerToken { get; set; }
    /// <summary>
    /// 报工系统内ID
    /// </summary>
    public int UserId { get; set; }
    /// <summary>
    /// 报工系统内部门ID
    /// </summary>
    public int DeptId { get; set; }
}