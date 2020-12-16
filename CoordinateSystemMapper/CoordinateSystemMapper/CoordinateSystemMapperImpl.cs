// SPDX-License-Identifier: MIT
// The content of this file has been developed in the context of the MOSIM research project.
// Original author(s): Janis Sprenger

using System;
using System.Collections.Generic;
using MMIStandard;

namespace CoordinateSystemMapper
{

    public class CoordinateSystemMapperImpl : MCoordinateSystemMapper.Iface
    {
        private MServiceDescription description;


        public CoordinateSystemMapperImpl(MIPAddress address, MIPAddress registerAddress)
        {
            string ID = System.Guid.NewGuid().ToString(); // Todo receive new ID;
            List<MIPAddress> addresses = new List<MIPAddress>() { address }; //TODO fill with addresses. 
            description = new MServiceDescription("CoordinateSystemMapper", ID, "csharp", addresses);            
        }

        public MBoolResponse Setup(MAvatarDescription avatar, Dictionary<string, string> properties)
        {
            // Avatar and properties description not required. 
            return new MBoolResponse(true);
        }

        public MBoolResponse Restart(Dictionary<string, string> properties)
        {
            // Service is stateless and thus needs not restarting. 
            return new MBoolResponse(true);
        }

        public Dictionary<string, string> Consume(Dictionary<string, string> properties)
        {
            // consume not supported for this service, yet. 
            throw new NotImplementedException();
        }

        public MBoolResponse Dispose(Dictionary<string, string> properties)
        {
            // Service is stateless and thus no ressources have to be disposed. 
            return new MBoolResponse(true);
        }

        public MServiceDescription GetDescription()
        {
            return description;
        }

        public Dictionary<string, string> GetStatus()
        {
            return new Dictionary<string, string>()
            {
                { "Running", "true"}
            };
        }

        private bool LeftHanded(MDirection[] coordinateSystem)
        {
            switch (coordinateSystem[0])
            {
                case MDirection.Right:
                    if( (coordinateSystem[1] == MDirection.Up && coordinateSystem[2] == MDirection.Forward) ||
                        (coordinateSystem[1] == MDirection.Backward && coordinateSystem[2] == MDirection.Up) ||
                        (coordinateSystem[1] == MDirection.Down && coordinateSystem[2] == MDirection.Backward) ||
                        (coordinateSystem[1] == MDirection.Forward && coordinateSystem[2] == MDirection.Down))
                    {
                        return true;
                    } else
                    {
                        return false;
                    }
                    break;
                case MDirection.Up:
                    if ((coordinateSystem[1] == MDirection.Forward && coordinateSystem[2] == MDirection.Right) ||
                        (coordinateSystem[1] == MDirection.Right && coordinateSystem[2] == MDirection.Backward) ||
                        (coordinateSystem[1] == MDirection.Backward && coordinateSystem[2] == MDirection.Left) ||
                        (coordinateSystem[1] == MDirection.Left && coordinateSystem[2] == MDirection.Forward))
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                case MDirection.Forward:
                    if ((coordinateSystem[1] == MDirection.Right && coordinateSystem[2] == MDirection.Up) ||
                        (coordinateSystem[1] == MDirection.Up && coordinateSystem[2] == MDirection.Left) ||
                        (coordinateSystem[1] == MDirection.Left && coordinateSystem[2] == MDirection.Down) ||
                        (coordinateSystem[1] == MDirection.Down && coordinateSystem[2] == MDirection.Right))
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                case MDirection.Left:
                    if ((coordinateSystem[1] == MDirection.Forward && coordinateSystem[2] == MDirection.Up) ||
                        (coordinateSystem[1] == MDirection.Up && coordinateSystem[2] == MDirection.Backward) ||
                        (coordinateSystem[1] == MDirection.Backward && coordinateSystem[2] == MDirection.Down) ||
                        (coordinateSystem[1] == MDirection.Down && coordinateSystem[2] == MDirection.Forward))
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                case MDirection.Down:
                    if ((coordinateSystem[1] == MDirection.Forward && coordinateSystem[2] == MDirection.Left) ||
                        (coordinateSystem[1] == MDirection.Right && coordinateSystem[2] == MDirection.Forward) ||
                        (coordinateSystem[1] == MDirection.Backward && coordinateSystem[2] == MDirection.Right) ||
                        (coordinateSystem[1] == MDirection.Left && coordinateSystem[2] == MDirection.Backward))
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                case MDirection.Backward:
                    if ((coordinateSystem[1] == MDirection.Up && coordinateSystem[2] == MDirection.Right) ||
                        (coordinateSystem[1] == MDirection.Left && coordinateSystem[2] == MDirection.Up) ||
                        (coordinateSystem[1] == MDirection.Down && coordinateSystem[2] == MDirection.Left) ||
                        (coordinateSystem[1] == MDirection.Right && coordinateSystem[2] == MDirection.Down))
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
            }
            return false;
        }

        private MTransform ToMMI(MTransform t, MDirection[] axes)
        {

            double qx = 0.0d, qy = 0.0d, qz = 0.0d, qw = 1.0d;
            double vx = 0.0d, vy = 0.0d, vz = 0.0d;

            double[] q = new double[] { t.Rotation.X, t.Rotation.Y, t.Rotation.Z, t.Rotation.W };
            double[] v = new double[] { t.Position.X, t.Position.Y, t.Position.Z };

            for (int i = 0; i < 3; i++)
            {
                switch (axes[i])
                {
                    case MDirection.Right:
                        qx = q[i];
                        vx = v[i];
                        break;
                    case MDirection.Left:
                        qx = -q[i];
                        vx = -v[i];
                        break;
                    case MDirection.Up:
                        qy = q[i];
                        vy = v[i];
                        break;
                    case MDirection.Down:
                        qy = -q[i];
                        vy = -q[i];
                        break;
                    case MDirection.Forward:
                        qz = q[i];
                        vz = v[i];
                        break;
                    case MDirection.Backward:
                        qz = -q[i];
                        vz = -v[i];
                        break;
                }
            }
            if (LeftHanded(axes))
            {
                qw = q[3];
            }
            else
            {
                qw = -q[3];
            }


            return new MTransform("", new MVector3(vx, vy, vz), new MQuaternion(qx, qy, qz, qw));
        }

        private MTransform FromMMI(MTransform t, MDirection[] axes)
        {
            double[] q = new double[] { 0.0d, 0.0d, 0.0d, 1.0d };
            double[] v = new double[] { 0.0d, 0.0d, 0.0d };

            for (int i = 0; i < 3; i++)
            {
                switch (axes[i])
                {
                    case MDirection.Right:
                        q[i] = t.Rotation.X;
                        v[i] = t.Position.X;
                        break;
                    case MDirection.Left:
                        q[i] = -t.Rotation.X;
                        v[i] = -t.Position.X;
                        break;
                    case MDirection.Up:
                        q[i] = t.Rotation.Y;
                        v[i] = t.Position.Y;
                        break;
                    case MDirection.Down:
                        q[i] = -t.Rotation.Y;
                        v[i] = -t.Position.Y;
                        break;
                    case MDirection.Forward:
                        q[i] = t.Rotation.Z;
                        v[i] = t.Position.Z;
                        break;
                    case MDirection.Backward:
                        q[i] = -t.Rotation.Z;
                        v[i] = -t.Position.Z;
                        break;
                }
            }
            if (LeftHanded(axes))
            {
                q[3] = t.Rotation.W;
            }
            else
            {
                q[3] = -t.Rotation.W;
            }


            return new MTransform("", new MVector3(v[0], v[1], v[2]), new MQuaternion(q[0], q[1], q[2], q[3]));
        }


        public MQuaternion QuaternionFromMMI(MQuaternion quat, MDirection firstAxis, MDirection secondAxis, MDirection thirdAxis)
        {
            return QuaternionFromMMI_L(quat, new List<MDirection>() { firstAxis, secondAxis, thirdAxis });
        }

        public MQuaternion QuaternionFromMMI_L(MQuaternion quat, List<MDirection> coordinateSystem)
        {
            MTransform t = new MTransform("", new MVector3(0, 0, 0), quat);
            t = FromMMI(t, coordinateSystem.ToArray());
            return t.Rotation;
        }

        public MQuaternion QuaternionToMMI(MQuaternion quat, MDirection firstAxis, MDirection secondAxis, MDirection thirdAxis)
        {
            return QuaternionToMMI_L(quat, new List<MDirection>() { firstAxis, secondAxis, thirdAxis });
        }

        public MQuaternion QuaternionToMMI_L(MQuaternion quat, List<MDirection> coordinateSystem)
        {
            MTransform t = new MTransform("", new MVector3(0, 0, 0), quat);
            t = ToMMI(t, coordinateSystem.ToArray());
            return t.Rotation;
        }


        public MTransform TransformFromMMI(MTransform transform, MDirection firstAxis, MDirection secondAxis, MDirection thirdAxis)
        {
            return TransformFromMMI_L(transform, new List<MDirection>() { firstAxis, secondAxis, thirdAxis });
        }

        public MTransform TransformFromMMI_L(MTransform transform, List<MDirection> coordinateSystem)
        {
            MTransform t = FromMMI(transform, coordinateSystem.ToArray());
            t.ID = transform.ID;
            return t;

        }

        public MTransform TransformToMMI(MTransform transform, MDirection firstAxis, MDirection secondAxis, MDirection thirdAxis)
        {
            return TransformToMMI_L(transform, new List<MDirection>() { firstAxis, secondAxis, thirdAxis });
        }

        public MTransform TransformToMMI_L(MTransform transform, List<MDirection> coordinateSystem)
        {
            MTransform t = ToMMI(transform, coordinateSystem.ToArray());
            t.ID = transform.ID;
            return t;
        }

        public MVector3 VectorFromMMI(MVector3 vec, MDirection firstAxis, MDirection secondAxis, MDirection thirdAxis)
        {
            return VectorFromMMI_L(vec, new List<MDirection>() { firstAxis, secondAxis, thirdAxis });
        }

        public MVector3 VectorFromMMI_L(MVector3 vec, List<MDirection> coordinateSystem)
        {
            MTransform t = new MTransform("", vec, new MQuaternion());
            t = FromMMI(t, coordinateSystem.ToArray());
            return t.Position;
        }

        public MVector3 VectorToMMI(MVector3 vec, MDirection firstAxis, MDirection secondAxis, MDirection thirdAxis)
        {
            return VectorToMMI_L(vec, new List<MDirection>() { firstAxis, secondAxis, thirdAxis });
        }

        public MVector3 VectorToMMI_L(MVector3 vec, List<MDirection> coordinateSystem)
        {
            MTransform t = new MTransform("", vec, new MQuaternion());
            t = ToMMI(t, coordinateSystem.ToArray());
            return t.Position;
        }
    }
}
