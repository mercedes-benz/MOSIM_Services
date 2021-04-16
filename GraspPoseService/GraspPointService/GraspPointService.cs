using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.IO;
using System.Text;
using MMIStandard;
using MMICSharp.MMIStandard;
using Thrift;
using Thrift.Protocol;
using Thrift.Server;
using Thrift.Transport;


namespace GraspPointService
{
    public class GraspPointService : MGraspPoseService.Iface
    {
        public Dictionary<string, MVector3> defaultPositions = new Dictionary<string, MVector3>();
        public MServiceDescription ServiceDescription;

        public MVector3 global_pos { get; set; } = new MVector3();
        public MQuaternion global_rot { get; set; } = new MQuaternion(0, 0, 0, 1);

        private MSkeletonAccess.Client skeleton;


        public GraspPointService(MIPAddress address, MIPAddress register)
        {
            //this.loadDefaultPose("./default_pose.json");
            this.loadDefaultPose();

            Console.WriteLine("Start Service at port" + address.Port.ToString());
            var id = Guid.NewGuid().ToString();
            var name = "GraspPoseService";
            var language = "C#";
            var addresses = new List<MIPAddress> { new MIPAddress(address.Address, address.Port) };
            ServiceDescription = new MServiceDescription(name, id, language, addresses);

            TTransport transport = new TBufferedTransport(new TSocket(register.Address, 9099));
            TProtocol protocol = new TCompactProtocol(transport);

            skeleton = new MSkeletonAccess.Client(protocol);
            
            transport.Open();
            MAvatarPostureValues v = skeleton.GetCurrentPostureValues("1");
        }

        private static double NextValue(JsonTextReader reader)
        {
            while (reader.Read())
            {
                if (reader.Value != null)
                {
                    if (reader.TokenType == JsonToken.Float)
                    {
                        return double.Parse(reader.Value.ToString());
                    }
                }
            }
            return 0.0f;
        }

        
        private void loadDefaultPose()
        {
            //string json = System.IO.File.ReadAllText(file);
            byte[] resource = Resource.default_pose;
            string json = Encoding.Default.GetString(resource);

            JsonTextReader reader = new JsonTextReader(new StringReader(json));

            bool b_leftWrist = false;
            string currentJoint = "";

            while (reader.Read())
            {
                if (reader.Value != null)
                {
                    if (reader.TokenType == JsonToken.PropertyName)
                    {
                        if (reader.Value.Equals("LeftWrist"))
                        {
                            b_leftWrist = true;
                        }
                        else if (reader.Value.Equals("RightWrist"))
                        {
                            b_leftWrist = false;
                        }
                        else
                        {
                            currentJoint = reader.Value.ToString();
                            MVector3 pos = new MVector3(NextValue(reader), NextValue(reader), NextValue(reader));
                            defaultPositions.Add(currentJoint, pos);
                        }
                    }
                }
                else
                {
                    //Console.WriteLine("Token: {0}", reader.TokenType);
                }
            }
        }

        public static MVector3 Crossproduct(MVector3 v1, MVector3 v2)
        {
            MVector3 cp = new MVector3();
            cp.X = v1.Y * v2.Z - v1.Z * v2.Y;
            cp.Y = v1.Z * v2.X - v1.X * v2.Z;
            cp.Z = v1.X * v2.Y - v1.Y * v2.X;
            return cp;
        }

        public double Dot(MVector3 v1, MVector3 v2)
        {
            return v1.X * v2.X + v1.Y * v2.Y + v1.Z * v2.Z;
        }

        public MVector3 GetGlobalTipPosition(string name)
        {
            return global_rot.Multiply(defaultPositions[name]).Add(global_pos);
        }

        public MQuaternion GetGlobalTipRotation(string name)
        {
            return new MQuaternion(0, 0, 0, 1);
        }

        public static MQuaternion Normalize(MQuaternion q)
        {
            double mag = Math.Sqrt(q.X * q.X + q.Y * q.Y + q.Z * q.Z + q.W * q.W);
            return new MQuaternion(q.X / mag, q.Y / mag, q.Z / mag, q.W / mag);
        }


        public List<MGeometryConstraint> GetGraspPoses(MAvatarPostureValues posture, MJointType handType, MSceneObject sceneObject, bool repositionHand)
        {
            MVector3 objectPos = sceneObject.Transform.Position;
            MVector3 handPos = new MVector3();
            MQuaternion handRot = new MQuaternion(0, 0, 0, 1);

            MAvatarPostureValues before = skeleton.GetCurrentPostureValues(posture.AvatarID);

            skeleton.SetChannelData(posture);
            handPos = skeleton.GetGlobalJointPosition(posture.AvatarID, handType);
            handRot = skeleton.GetGlobalJointRotation(posture.AvatarID, handType);

            // find rotation to target:
            MVector3 vector_to = objectPos.Subtract(handPos).Normalize();
            MVector3 DefaultHandDir = new MVector3(1, 0, 0);
            if (handType == MJointType.LeftWrist)
            {
                DefaultHandDir = new MVector3(-1, 0, 0);
            }
            MVector3 handDirection = handRot.Multiply(DefaultHandDir);
            MQuaternion newWristRotation = new MQuaternion(handRot.X, handRot.Y, handRot.Z, handRot.W);
            if (handDirection.Subtract(vector_to).Magnitude() > 0.01)
            {
                MVector3 crossproduct = Crossproduct(handDirection, vector_to);
                double w = Math.Sqrt(handDirection.Magnitude() * handDirection.Magnitude() * vector_to.Magnitude() * vector_to.Magnitude()) + Dot(handDirection, vector_to);
                newWristRotation = Normalize(Normalize(new MQuaternion(crossproduct.X, crossproduct.Y, crossproduct.Z, w)).Multiply(handRot));
            }

            List<MGeometryConstraint> constraints = new List<MGeometryConstraint>(6);
            MGeometryConstraint wrist, thumb, index, middle, ring, little;
            MTransform Twrist, Tthumb, Tindex, Tmiddle, Tring, Tlittle;
            string rthumb = "RightThumbTip";
            string rindex = "RightIndexTip";
            string rmiddle = "RightMiddleTip";
            string rring = "RightRingTip";
            string rlittle = "RightLittleTip";
            string lthumb = "LeftThumbTip";
            string lindex = "LeftIndexTip";
            string lmiddle = "LeftMiddleTip";
            string lring = "LeftRingTip";
            string llittle = "LeftLittleTip";

            if (handType == MJointType.RightWrist)
            {

                //MJoint wrist = new MJoint("RightWrist", MJointType.RightWrist, handPos, newWristRotation);
                Twrist = new MTransform("RightWrist", handPos, newWristRotation);
                this.global_pos = handPos;
                this.global_rot = newWristRotation;

                Tthumb = new MTransform(rthumb, GetGlobalTipPosition(rthumb), GetGlobalTipRotation(rthumb));
                Tindex = new MTransform(rindex, GetGlobalTipPosition(rindex), GetGlobalTipRotation(rindex));
                Tmiddle = new MTransform(rmiddle, GetGlobalTipPosition(rmiddle), GetGlobalTipRotation(rmiddle));
                Tring = new MTransform(rring, GetGlobalTipPosition(rring), GetGlobalTipRotation(rring));
                Tlittle = new MTransform(rlittle, GetGlobalTipPosition(rlittle), GetGlobalTipRotation(rlittle));
            }
            else
            {
                Twrist = new MTransform("LeftWrist", handPos, newWristRotation);
                this.global_pos = handPos;
                this.global_rot = newWristRotation;

                Tthumb = new MTransform(lthumb, GetGlobalTipPosition(lthumb), GetGlobalTipRotation(lthumb));
                Tindex = new MTransform(lindex, GetGlobalTipPosition(lindex), GetGlobalTipRotation(lindex));
                Tmiddle = new MTransform(lmiddle, GetGlobalTipPosition(lmiddle), GetGlobalTipRotation(lmiddle));
                Tring = new MTransform(lring, GetGlobalTipPosition(lring), GetGlobalTipRotation(lring));
                Tlittle = new MTransform(llittle, GetGlobalTipPosition(llittle), GetGlobalTipRotation(llittle));
            }

            for (int i = 0; i < 6; i++)
                constraints.Add(new MGeometryConstraint(""));

            constraints[0].ParentToConstraint = Twrist;
            constraints[1].ParentToConstraint = Tthumb;
            constraints[2].ParentToConstraint = Tindex;
            constraints[3].ParentToConstraint = Tmiddle;
            constraints[4].ParentToConstraint = Tring;
            constraints[5].ParentToConstraint = Tlittle;

            return constraints;
        }

        public MHandPose ComputeGraspPose(MAvatarPosture posture, MJointType handType, MSceneObject sceneObject, bool repositionHand)
        {
            MVector3 objectPos = sceneObject.Transform.Position;
            MVector3 handPos = new MVector3();
            MQuaternion handRot = new MQuaternion(0, 0, 0, 1);
            for (int i = 0; i<posture.Joints.Count; i++)
            {
                if(posture.Joints[i].Type == handType)
                {
                    handPos = posture.Joints[i].Position;
                    handRot = posture.Joints[i].Rotation;
                }
            }

            // find rotation to target:
            MVector3 vector_to = objectPos.Subtract(handPos).Normalize();
            MVector3 DefaultHandDir = new MVector3(1, 0, 0);
            if (handType == MJointType.LeftWrist)
            {
                DefaultHandDir = new MVector3(-1, 0, 0);
            }
            MVector3 handDirection = handRot.Multiply(DefaultHandDir);
            MQuaternion newWristRotation = new MQuaternion(handRot.X, handRot.Y, handRot.Z, handRot.W);
            if (handDirection.Subtract(vector_to).Magnitude() > 0.01)
            {
                MVector3 crossproduct = Crossproduct(handDirection, vector_to);
                double w = Math.Sqrt(handDirection.Magnitude() * handDirection.Magnitude() * vector_to.Magnitude() * vector_to.Magnitude()) + Dot(handDirection, vector_to);
                newWristRotation = Normalize(Normalize(new MQuaternion(crossproduct.X, crossproduct.Y, crossproduct.Z, w)).Multiply(handRot));
            }

            

            if (handType == MJointType.RightWrist)
            {

                MJoint wrist = new MJoint("RightWrist", MJointType.RightWrist, handPos, newWristRotation);
                this.global_pos = handPos;
                this.global_rot = newWristRotation;
                MJoint rightThumb = new MJoint("RightThumbTip", MJointType.RightThumbTip, GetGlobalTipPosition("RightThumbTip"), GetGlobalTipRotation("RightThumbTip"));
                MJoint rightIndex = new MJoint("RightIndexTip", MJointType.RightIndexTip, GetGlobalTipPosition("RightIndexTip"), GetGlobalTipRotation("RightIndexTip"));
                MJoint rightMiddle = new MJoint("RightMiddleTip", MJointType.RightMiddleTip, GetGlobalTipPosition("RightMiddleTip"), GetGlobalTipRotation("RightMiddleTip"));
                MJoint rightRing = new MJoint("RightRingTip", MJointType.RightRingTip, GetGlobalTipPosition("RightRingTip"), GetGlobalTipRotation("RightRingTip"));
                MJoint rightLittle = new MJoint("RightLittleTip", MJointType.RightLittleTip, GetGlobalTipPosition("RightLittleTip"), GetGlobalTipRotation("RightLittleTip"));

                List<MJoint> list = new List<MJoint> { wrist, rightThumb, rightIndex, rightMiddle, rightRing, rightLittle };
                MHandPose hand = new MHandPose(list);
                return hand;
            } else
            {
                MJoint wrist = new MJoint("LeftWrist", MJointType.LeftWrist, handPos, newWristRotation);
                this.global_pos = handPos;
                this.global_rot = newWristRotation;
                MJoint leftThumb = new MJoint("LeftThumbTip", MJointType.LeftThumbTip, GetGlobalTipPosition("LeftThumbTip"), GetGlobalTipRotation("LeftThumbTip"));
                MJoint leftIndex = new MJoint("LeftIndexTip", MJointType.LeftIndexTip, GetGlobalTipPosition("LeftIndexTip"), GetGlobalTipRotation("LeftIndexTip"));
                MJoint leftMiddle = new MJoint("LeftMiddleTip", MJointType.LeftMiddleTip, GetGlobalTipPosition("LeftMiddleTip"), GetGlobalTipRotation("LeftMiddleTip"));
                MJoint leftRing = new MJoint("LeftRingTip", MJointType.LeftRingTip, GetGlobalTipPosition("LeftRingTip"), GetGlobalTipRotation("LeftRingTip"));
                MJoint leftLittle = new MJoint("LeftLittleTip", MJointType.LeftLittleTip, GetGlobalTipPosition("LeftLittleTip"), GetGlobalTipRotation("LeftLittleTip"));

                List<MJoint> list = new List<MJoint> { wrist, leftThumb, leftIndex, leftMiddle, leftRing, leftLittle };
                MHandPose hand = new MHandPose(list);
                return hand;
            }
            
        }

        public Dictionary<string, string> GetStatus()
        {
            return new Dictionary<string, string>();
        }

        public MServiceDescription GetDescription()
        {
            return ServiceDescription;
        }

        public MBoolResponse Setup(MAvatarDescription avatar, Dictionary<string, string> properties)
        {
            return new MBoolResponse(true);
        }

        public Dictionary<string, string> Consume(Dictionary<string, string> properties)
        {
            throw new NotImplementedException();
        }
    }
}
