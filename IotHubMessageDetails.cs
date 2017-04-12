using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dmx.Helper.Azure.IotHubSignUp
{
    public sealed class IotHubMessageDetails 
    {
        public IotHubMessageDetails(object msg)
        {
            Message = msg;
        }
        private object Message;
    }
}
