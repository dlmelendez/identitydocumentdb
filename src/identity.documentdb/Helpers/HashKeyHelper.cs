// MIT License Copyright 2014 (c) David Melendez. All rights reserved. See License.txt in the project root for license information.

using Microsoft.AspNet.Identity;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace ElCamino.AspNet.Identity.DocumentDB.Helpers
{
    public class HashKeyHelper : UriEncodeKeyHelper
    {
        public override string GenerateRowKeyUserLoginInfo(UserLoginInfo info)
        {
            string hash = ConvertKeyToHash(base.GenerateRowKeyUserLoginInfo(info));
            return string.Format(Constants.RowKeyConstants.FormatterIdentityUserLogin, hash);
        }

        public override string GenerateRowKeyIdentityUserRole(string plainRoleName)
        {
            string hash = ConvertKeyToHash(base.GenerateRowKeyIdentityUserRole(plainRoleName));
            return string.Format(Constants.RowKeyConstants.FormatterIdentityUserRole, hash);

        }

        public override string GenerateRowKeyIdentityRole(string plainRoleName)
        {
            string hash = ConvertKeyToHash(base.GenerateRowKeyIdentityRole(plainRoleName));
            return string.Format(Constants.RowKeyConstants.FormatterIdentityRole, hash);

        }

        public override string GenerateRowKeyIdentityUserClaim(string claimType, string claimValue)
        {
            string hash = ConvertKeyToHash(base.GenerateRowKeyIdentityUserClaim(claimType, claimValue));
            return string.Format(Constants.RowKeyConstants.FormatterIdentityUserClaim, hash);
        }


        public override string GenerateRowKeyIdentityUserLogin(string loginProvider, string providerKey)
        {
            string hash = ConvertKeyToHash(base.GenerateRowKeyIdentityUserLogin(loginProvider, providerKey));
            return string.Format(Constants.RowKeyConstants.FormatterIdentityUserLogin, hash);

        }

        public override double KeyVersion
        {
            get
            {
                return 2.0;
            }
        }

        public static string ConvertKeyToHash(string input)
        {
            using (SHA256 sha = SHA256.Create())
            {
                return GetHash(sha, input);
            }
        }

        private static string GetHash(SHA256 shaHash, string input)
        {
            // Convert the input string to a byte array and compute the hash. 
            byte[] data = shaHash.ComputeHash(Encoding.Unicode.GetBytes(input));
            Debug.WriteLine(string.Format("Key Size before hash: {0} bytes", Encoding.Unicode.GetBytes(input).Length));

            // Create a new Stringbuilder to collect the bytes 
            // and create a string.
            StringBuilder sBuilder = new StringBuilder(32);

            // Loop through each byte of the hashed data  
            // and format each one as a hexadecimal string. 
            for (int i = 0; i < data.Length; i++)
            {
                sBuilder.Append(data[i].ToString("X2"));
            }

            // Return the hexadecimal string. 
            return sBuilder.ToString();
        }
    }
}
