using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetworkExplorer
{
    internal class Device
    {
        public string IP { get; private set;  }

        public List<Port> Ports { get; private set; }

        public Device(string ip)
        {
            IP = ip;
            Ports = new List<Port>();
        }

        public Device(string ip, List<Port> ports)
        {
            IP = ip;
            Ports = ports;
        }


    }

    internal struct Port
    {
        public int PortNr { get; private set; }
        public string Name { get; private set; }
        public string Description {  get; private set; }
    }
}
