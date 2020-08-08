using CssCsData;
using CssCsData.Cloud;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Drive.v3.Data;
using Microsoft.Identity.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CssCsCloud.Cloud
{
  public class OauthResult
  {
    public string User { get; internal set; }
    public string Email { get; internal set; }
    public CloudName CloudName { get; internal set; }
  }

  public static class Oauth
  {
    /// <summary>
    /// 
    /// </summary>
    /// <param name="account"></param>
    /// <exception cref="NotSupportedException"></exception>
    /// <returns>ICloud</returns>
    public static ICloud GetCloud(Account account)
    {
      if (null == account) throw new ArgumentNullException(nameof(account));
      switch (account.CloudName)
      {
        case CloudName.GoogleDrive: return new CloudGoogleDrive(account);
        case CloudName.OneDrive: return new CloudOneDrive(account);

        default: throw new NotSupportedException();
      }
    }


    /// <summary>
    /// 
    /// </summary>
    /// <param name="cloudName"></param>
    /// <exception cref="NotSupportedException"></exception>
    /// <returns>OauthResult</returns>
    public static async Task<OauthResult> OauthNewAccount(CloudName cloudName)
    {
      try
      {
        OauthResult result = new OauthResult();
        result.CloudName = cloudName;
        switch (cloudName)
        {
          case CloudName.GoogleDrive:
            UserCredential uc = await CloudGoogleDrive.Oauth2().ConfigureAwait(false);
            using (DriveService service = CloudGoogleDrive.GetDriveService(uc))
            {
              var getRequest = service.About.Get();
              getRequest.Fields = "user";
              About about = await getRequest.ExecuteAsync().ConfigureAwait(false);
              result.Email = about.User.EmailAddress;
              result.User = uc.UserId;
            }
            break;

          case CloudName.OneDrive:
            AuthenticationResult ar = await CloudOneDrive.Oauth().ConfigureAwait(false);
            result.User = ar.Account.HomeAccountId.Identifier;
            result.Email = ar.Account.Username;
            break;

          default: throw new NotSupportedException();
        }
        return result;
      }
      catch(Exception ex)
      {
        if (ex is MsalClientException msalex &&
          !string.IsNullOrEmpty(msalex.ErrorCode) &&
          msalex.ErrorCode.Equals("authentication_canceled", StringComparison.OrdinalIgnoreCase)) return null;
        else throw;
      }
    }
  } 
}
