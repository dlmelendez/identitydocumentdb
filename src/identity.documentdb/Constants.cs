// MIT License Copyright 2014 (c) David Melendez. All rights reserved. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElCamino.AspNet.Identity.DocumentDB
{
    public static class Constants
    {
        public const string ETagWildcard = "*";

        public static class AppSettingsKeys
        {
            public const string DatabaseNameKey = "IdDocDb_Database";
            public const string DatabaseUriKey = "IdDocDb_Uri";
            public const string DatabaseAuthKey = "IdDocDb_AuthKey";
        }

        public static class DocumentCollectionIds
        {
            public const string RolesCollection = "Roles";
            public const string UsersCollection = "Users";
        }

        public static class RowKeyConstants
        {
            #region Identity User
            public const string PreFixIdentityUserClaim     = "C_";
            public const string PreFixIdentityUserRole      = "R_";
            public const string PreFixIdentityUserLogin     = "L_";
            public const string PreFixIdentityUserEmail     = "E_";
            public const string PreFixIdentityUserName      = "U_";

            public const string FormatterIdentityUserClaim  = PreFixIdentityUserClaim + "{0}";
            public const string FormatterIdentityUserRole   = PreFixIdentityUserRole + "{0}";
            public const string FormatterIdentityUserLogin  = PreFixIdentityUserLogin + "{0}";
            public const string FormatterIdentityUserEmail  = PreFixIdentityUserEmail + "{0}";
            public const string FormatterIdentityUserName   = PreFixIdentityUserName + "{0}";
            #endregion

            #region Identity Role
            public const string PreFixIdentityRole = "R_";
            public const string FormatterIdentityRole = PreFixIdentityRole + "{0}";
            #endregion
        }
    }
}
