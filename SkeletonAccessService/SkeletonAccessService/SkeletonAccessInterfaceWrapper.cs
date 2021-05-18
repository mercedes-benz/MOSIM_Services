using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MMICSharp.Common;
using MMIStandard;

namespace SkeletonAccessService
{
    public class SkeletonAccessInterfaceWrapper : IntermediateSkeleton
    {
        private MIPAddress m_Address;
        public SkeletonAccessInterfaceWrapper(MIPAddress address)
        {
            m_Address = address;
        }

        public new MServiceDescription GetDescription()
        {
            MServiceDescription desc = new MServiceDescription();
            desc.ID = Guid.NewGuid().ToString();
            desc.Language = "CSharp";
            desc.Name = "Standalone Skeleton Access";
            desc.Addresses = new List<MIPAddress>() { this.m_Address };
            desc.Parameters = new List<MParameter>();
            desc.Properties = new Dictionary<string, string>();
            return desc;
        }

    }
}
