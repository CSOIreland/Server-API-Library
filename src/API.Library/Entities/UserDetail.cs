namespace API
{
    /// <summary>
    /// UserPrincipal data
    /// </summary>
    public class UserDetail
    {
       public UserDetail()
       {
       }

        public UserDetail(string? _identity, string? _SamAccountName, string? _UserPrincipalName, string? _DistinguishedName, string? _DisplayName, string? _Name, string? _GivenName, string? _MiddleName, string? _Surname, string? _EmailAddress, string? _EmployeeId, string? _Description, bool? _Enabled, string? _VoiceTelephoneNumber,string? _JobTitle,string _ManagerUserPrincipalName, dynamic _additionalProperties )
        {
            Identity = _identity;
            SamAccountName = _SamAccountName;
            UserPrincipalName = _UserPrincipalName;
            DistinguishedName = _DistinguishedName;
            DisplayName = _DisplayName;
            Name = _Name;
            GivenName = _GivenName;
            MiddleName = _MiddleName;
            Surname = _Surname;
            EmailAddress = _EmailAddress;
            EmployeeId = _EmployeeId;
            Description = _Description;
            Enabled = _Enabled;
            VoiceTelephoneNumber = _VoiceTelephoneNumber;
            JobTitle = _JobTitle;
            ManagerUserPrincipalName = _ManagerUserPrincipalName;
            AdditionalProperties = _additionalProperties;
        }

       public string Identity { get; set; }
       public string? SamAccountName { get; set; }

       public string? UserPrincipalName { get; set; }

       public string? DistinguishedName { get; set; }

       public string? DisplayName { get; set; }

       public string? Name { get; set; }

       public string? GivenName { get; set; }

       public string? MiddleName { get; set; }

       public string? Surname { get; set; }

       public string? EmailAddress { get; set; }
       public string? EmployeeId { get; set; }
       public string? Description { get; set; }

       public bool? Enabled { get; set; }

       public string? VoiceTelephoneNumber { get; set; }

       public string? JobTitle { get; set; }

       public string? ManagerUserPrincipalName { get; set; }

       public dynamic? AdditionalProperties { get; set; }

    }
}