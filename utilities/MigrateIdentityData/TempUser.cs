// MIT License Copyright 2014 (c) David Melendez. All rights reserved. See License.txt in the project root for license information.

using Microsoft.Azure.Documents;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MigrateIdentityData
{
    public class TempUser
    {
        public Document User { get; set; }

        public List<Document> Roles { get; set; }

        public List<Document> Logins { get; set; }

        public List<Document> Claims { get; set; }

        public TempUser()
        {
            Roles = new List<Document>();
            Logins = new List<Document>();
            Claims = new List<Document>();
        }
    }
}
