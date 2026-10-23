using System.Security;
using System.Security.Permissions;

// Unity's networking keeps what this library needs private
[module: UnverifiableCode]
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
