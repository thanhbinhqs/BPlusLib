using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace Blib
{
    public static class ExtensionMethods
    {

        public static long ToLong(this IPAddress addr)
        {
            return (long)(uint)IPAddress.NetworkToHostOrder(
                 (int)IPAddress.Parse(addr.Address.ToString()).Address);
        }


    }
}
