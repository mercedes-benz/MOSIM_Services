using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MMICSharp.Common;
using MMIStandard;

namespace RetargetingServiceServer
{
    public class RetargetingInterfaceWrapper : RetargetingService
    {
        public RetargetingInterfaceWrapper(string ip, int port) : base(ip, port)
        {
           
        }

        public override MAvatarPostureValues RetargetToIntermediate(MAvatarPosture globalTarget)
        {
            MAvatarPostureValues ret = base.RetargetToIntermediate(globalTarget);

            return ret;
        }

        public override MAvatarPosture RetargetToTarget(MAvatarPostureValues intermediatePostureValues)
        {
            MAvatarPosture p = base.RetargetToTarget(intermediatePostureValues);
            for(int i = 0; i<p.Joints.Count; i++)
            {
                if(p.Joints[i].Position == null)
                {
                    p.Joints[i].Position = new MVector3(0, 0, 0);
                }
                if(p.Joints[i].Rotation == null)
                {
                    p.Joints[i].Rotation = new MQuaternion(0, 0, 0, 1);
                }
            }

            return p;
        }


    }
}
