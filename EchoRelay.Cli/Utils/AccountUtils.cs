using EchoRelay.Core.Game;
using EchoRelay.Core.Server.Storage.Types;
using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EchoRelay.Cli.Utils
{
    public static class AccountUtils
    {
        //Facilitate finding accounts

        public static AccountResource GetAccount(string username)
        {
            AccountResource? resource = null;
            resource = Program.serverStorage?.Accounts.Values()
                    .FirstOrDefault(x => x.Profile.Server.DisplayName == username);

            return resource;
        }
        public static AccountResource GetAccount(int id)
        {
            AccountResource? resource = null;
            resource = Program.serverStorage?.Accounts.Get(XPlatformId.Parse(id.ToString())!);

            return resource;
        }

        public static bool Ban(AccountResource account, TimeSpan timeSpan)
        {
            try
            {
                account.BannedUntil = DateTime.UtcNow + timeSpan;
                Program.serverStorage?.Accounts.Set(account);
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }
    }
}
