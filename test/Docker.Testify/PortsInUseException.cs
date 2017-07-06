using System;
using System.Collections.Generic;
using System.Text;

namespace Docker.Testify
{
    public class PortsInUseException : Exception
    {
        public PortsInUseException() 
            : base("Ports in range are not available")
        {
        }
    }
}
