using Microsoft.VisualStudio.TestTools.UnitTesting;
using MMIStandard;
using System.Collections.Generic;
using MMICSharp.Common;
using MMICSharp.Clients;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Thrift;
using Thrift.Protocol;
using Thrift.Server;
using Thrift.Transport;


namespace GraspPointService
{
    [TestClass]
    public class GraspTestLocal
    {
        [TestMethod]
        public void Test_01_LoadDefaultPose()
        {
            GraspPointService service = new GraspPointService(new MIPAddress("127.0.0.1", 1234), new MIPAddress("127.0.0.1", 9009));
            Assert.AreEqual(10, service.defaultPositions.Count);
            foreach (KeyValuePair<string, MVector3> entry in service.defaultPositions)
            {
                Assert.IsTrue(service.GetGlobalTipPosition(entry.Key).Subtract(entry.Value).Magnitude() <= 0.0001);
            }

        }

        [TestMethod]
        public void Test_02_Transformations()
        {
            GraspPointService service = new GraspPointService(new MIPAddress("127.0.0.1", 1234), new MIPAddress("127.0.0.1", 9009));
            MVector3 d = new MVector3(10, 10, 10);
            service.global_pos = d;

            foreach (KeyValuePair<string, MVector3> entry in service.defaultPositions)
            {
                Assert.IsTrue(service.GetGlobalTipPosition(entry.Key).Subtract(entry.Value.Add(d)).Magnitude() <= 0.0001);
            }


            MQuaternion q = GraspPointService.Normalize(new MQuaternion(-0.43, 0.059, -0.376, 0.924));
            service.global_rot = q;

            foreach (KeyValuePair<string, MVector3> entry in service.defaultPositions)
            {
                Assert.IsTrue(service.GetGlobalTipPosition(entry.Key).Subtract(q.Multiply(entry.Value).Add(d)).Magnitude() <= 0.0001);
            }
        }

        private static bool AssertVectors(MVector3 v1, MVector3 v2)
        {
            return AssertVectors(v1, v2, 0.001f);
        }

        private static bool AssertVectors(MVector3 v1, MVector3 v2, float criterion)
        {
            return v1.Subtract(v2).Magnitude() <= criterion;
        }

        [TestMethod]
        public void Test_03_Grasp_NoRotation()
        {
            GraspPointService service = new GraspPointService(new MIPAddress("127.0.0.1", 1234), new MIPAddress("127.0.0.1", 9009));
            MVector3 d = new MVector3(-0.7531490325927734, 1.5269964933395386, -0.06743687391281128);
            MQuaternion q = GraspPointService.Normalize(new MQuaternion(0.011663678102195263, -0.07214047014713287, -0.9189794063568115, -0.38747456669807434));
            MJoint wrist = new MJoint("LeftWrist", MJointType.LeftWrist, d, q);
            MVector3 target = d.Add(q.Multiply(new MVector3(-1, 0, 0)));

            List<MGeometryConstraint> result = TestGrasp(service, wrist, target);
            Assert.AreEqual(result.Count, 6);

            Assert.IsTrue(AssertVectors(result[1].ParentToConstraint.Position, new MVector3(-0.7757415175437927, 1.3994311094284058, -0.02170582115650177), 0.01f));
            Assert.IsTrue(AssertVectors(result[2].ParentToConstraint.Position, new MVector3(-0.7892686724662781, 1.3968180418014526, -0.01914067007601261), 0.01f));
            Assert.IsTrue(AssertVectors(result[3].ParentToConstraint.Position, new MVector3(-0.7902005910873413, 1.3844854831695557, -0.045743849128484726), 0.01f));
        }

        [TestMethod]
        public void Test_04_Grasp_TwistRotation()
        {
            GraspPointService service = new GraspPointService(new MIPAddress("127.0.0.1", 1234), new MIPAddress("127.0.0.1", 9009));
            MVector3 d = new MVector3(-0.7531490325927734, 1.5269964933395386, -0.06743687391281128);
            MQuaternion q = GraspPointService.Normalize(new MQuaternion(0.011663678102195263, -0.07214047014713287, -0.9189794063568115, -0.38747456669807434));
            MJoint wrist = new MJoint("LeftWrist", MJointType.LeftWrist, d, q);

            // with twist rotation
            MQuaternion qtwist = new MQuaternion(0.21029071509838104, -0.15429531037807465, -0.8946714997291565, -0.36267584562301636);
            MVector3 target2 = d.Add(qtwist.Multiply(new MVector3(-1, 0, 0)));
            List<MGeometryConstraint> result2 = TestGrasp(service, wrist, target2);
            Assert.IsTrue(AssertVectors(result2[3].ParentToConstraint.Position, new MVector3(-0.7941258549690247, 1.3942217826843262, -0.014102483168244362), 0.01f));
            Assert.IsTrue(AssertVectors(result2[4].ParentToConstraint.Position, new MVector3(-0.782811164855957, 1.3946980237960815, -0.03699272498488426), 0.01f));
            Assert.IsTrue(AssertVectors(result2[5].ParentToConstraint.Position, new MVector3(-0.7711732983589172, 1.403513789176941, -0.059470854699611664), 0.01f));
        }


        [TestMethod]
        public void Test_04_Grasp_FullRotation_Left()
        {
            GraspPointService service = new GraspPointService(new MIPAddress("127.0.0.1", 1234), new MIPAddress("127.0.0.1", 9009));
            MVector3 d = new MVector3(-0.7531490325927734, 1.5269964933395386, -0.06743687391281128);
            MQuaternion q = GraspPointService.Normalize(new MQuaternion(-0.051147, -0.287468, -0.826503, -0.481289));
            MJoint wrist = new MJoint("LeftWrist", MJointType.LeftWrist, d, q);

            // with twist rotation
            MQuaternion qtarget = new MQuaternion(-0.070946, -0.283232, -0.791161, -0.537411);
            MVector3 target2 = d.Add(qtarget.Multiply(new MVector3(-1, 0, 0)));
            List<MGeometryConstraint> result2 = TestGrasp(service, wrist, target2);
            Assert.IsTrue(AssertVectors(result2[1].ParentToConstraint.Position, new MVector3(-0.798723, 1.438287, 0.027053), 0.01f));
            Assert.IsTrue(AssertVectors(result2[2].ParentToConstraint.Position, new MVector3(-0.810607, 1.442431, 0.033216), 0.01f));
            Assert.IsTrue(AssertVectors(result2[3].ParentToConstraint.Position, new MVector3(-0.824583, 1.425051, 0.014155), 0.01f));
            Assert.IsTrue(AssertVectors(result2[4].ParentToConstraint.Position, new MVector3(-0.826580, 1.423268, -0.011243), 0.01f));
            Assert.IsTrue(AssertVectors(result2[5].ParentToConstraint.Position, new MVector3(-0.824011, 1.428439, -0.037417), 0.01f));
        }

        [TestMethod]
        public void Test_04_Grasp_FullRotation_Right()
        {
            GraspPointService service = new GraspPointService(new MIPAddress("127.0.0.1", 1234), new MIPAddress("127.0.0.1", 9009));
            MVector3 d = new MVector3(0.753149, 1.526996, -0.067437);
            MQuaternion q = GraspPointService.Normalize(new MQuaternion(-0.054499, 0.172148, 0.855220, -0.485791));
            MJoint wrist = new MJoint("RightWrist", MJointType.RightWrist, d, q);

            // with twist rotation
            MQuaternion qtarget = GraspPointService.Normalize(new MQuaternion(-0.063543, 0.169019, 0.828282, -0.530419));
            MVector3 target2 = d.Add(qtarget.Multiply(new MVector3(1, 0, 0)));
            List<MGeometryConstraint> result2 = TestGrasp(service, wrist, target2);
            Assert.IsTrue(AssertVectors(result2[1].ParentToConstraint.Position, new MVector3(0.781985, 1.420199, 0.011878), 0.01f));
            Assert.IsTrue(AssertVectors(result2[2].ParentToConstraint.Position, new MVector3(0.818831, 1.422909, 0.006287), 0.01f));
            Assert.IsTrue(AssertVectors(result2[3].ParentToConstraint.Position, new MVector3(0.829268, 1.409252, -0.017488), 0.01f));
            Assert.IsTrue(AssertVectors(result2[4].ParentToConstraint.Position, new MVector3(0.827827, 1.412431, -0.042787), 0.01f));
            Assert.IsTrue(AssertVectors(result2[5].ParentToConstraint.Position, new MVector3(0.822123, 1.422696, -0.066881), 0.01f));
        }





        private List<MGeometryConstraint> TestGrasp(GraspPointService service, MJoint Wrist, MVector3 targetPos)
        {

            MQuaternion q = Wrist.Rotation;
            MVector3 d = Wrist.Position;
            List<MJoint> jointList = new List<MJoint> { Wrist };


            MAvatarPosture example_posture = new MAvatarPosture("Test", jointList);
            MSceneObject target = new MSceneObject("Target", "TargetName", new MTransform("TargetTransform", targetPos, new MQuaternion(0, 0, 0, 1)));


            // find rotation to target:
            MVector3 vector_to = target.Transform.Position.Subtract(d).Normalize();
            MVector3 DefaultHandDir = new MVector3(1, 0, 0);
            if (Wrist.Type == MJointType.LeftWrist)
            {
                DefaultHandDir = new MVector3(-1, 0, 0);
            }
            MVector3 handDirection = q.Multiply(DefaultHandDir);
            MQuaternion newWristRotation = new MQuaternion(q.X, q.Y, q.Z, q.W);
            if (handDirection.Subtract(vector_to).Magnitude() > 0.01)
            {
                MVector3 crossproduct = GraspPointService.Crossproduct(handDirection, vector_to);
                double w = Math.Sqrt(handDirection.Magnitude() * handDirection.Magnitude() * vector_to.Magnitude() * vector_to.Magnitude()) + service.Dot(handDirection, vector_to);
                newWristRotation = GraspPointService.Normalize(new MQuaternion(crossproduct.X, crossproduct.Y, crossproduct.Z, w)).Multiply(q);
            }


            MVector3 newTarget = newWristRotation.Multiply(DefaultHandDir).Normalize();

            Assert.IsTrue(newTarget.Subtract(vector_to).Magnitude() <= 0.001);

            TTransport transport = new TBufferedTransport(new TSocket("127.0.0.1", 9099));
            TProtocol protocol = new TCompactProtocol(transport);

            MSkeletonAccess.Client skeleton = new MSkeletonAccess.Client(protocol);

            transport.Open();

            MAvatarPostureValues v = skeleton.GetCurrentPostureValues("1");

            List<MGeometryConstraint> result = service.GetGraspPoses(v, Wrist.Type, target, false);

            Assert.AreEqual(result.Count, 6);

            Assert.IsTrue(AssertVectors(result[0].ParentToConstraint.Position, d));
            string lr = "Right";
            if (Wrist.Type == MJointType.LeftWrist)
            {
                lr = "Left";
            }

            /*
            Assert.IsTrue(AssertVectors(result.Joints[1].Position, service.GetGlobalTipPosition(lr + "ThumbTip")));
            Assert.IsTrue(AssertVectors(result.Joints[2].Position, service.GetGlobalTipPosition(lr + "IndexTip")));
            Assert.IsTrue(AssertVectors(result.Joints[3].Position, service.GetGlobalTipPosition(lr + "MiddleTip")));
            Assert.IsTrue(AssertVectors(result.Joints[4].Position, service.GetGlobalTipPosition(lr + "RingTip")));
            Assert.IsTrue(AssertVectors(result.Joints[5].Position, service.GetGlobalTipPosition(lr + "LittleTip")));
            */

            return result;
        }

        /*
        [TestMethod]
        public void Test_04_Thrift()
        {
            MMIRegisterServiceClient client = new MMIRegisterServiceClient("127.0.0.1", 9009);
            client.Start();
            string sessionID = client.Access.CreateSessionID(new Dictionary<string, string>());
            List<MServiceDescription> services = client.Access.GetRegisteredServices(sessionID);
            GraspPoseServiceClient GraspService;
            List<MJoint> joints = new List<MJoint>();

            MVector3 d = new MVector3(-0.7531490325927734, 1.5269964933395386, -1.5269964933395386);
            MQuaternion q = GraspPointService.Normalize(new MQuaternion(-0.43, 0.059, -0.376, 0.924));
            List<MJoint> jointList = new List<MJoint> { new MJoint("LeftWrist", MJointType.LeftWrist, d, q) };

            MAvatarPosture example_posture = new MAvatarPosture("Test", jointList);
            MSceneObject target = new MSceneObject("Target", "TargetName", new MTransform("TargetTransform", new MVector3(20, 20, 20), new MQuaternion(0, 0, 0, 1)));

            MHandPose local = TestGrasp(new GraspPointService(0), jointList[0], target.Transform.Position);

            bool thriftTest = false;

            foreach (MServiceDescription desc in services)
            {
                if (desc.Name == "GraspPoseService")
                {
                    GraspService = new GraspPoseServiceClient(desc.Addresses[0].Address, desc.Addresses[0].Port);
                    GraspService.Start();
                    
                    MHandPose remote = GraspService.Access.ComputeGraspPose(example_posture, MJointType.LeftWrist, target, true);

                    Assert.AreEqual(remote.Joints.Count, local.Joints.Count);
                    for(int i = 0; i<remote.Joints.Count; i++)
                    {
                        Assert.IsTrue(AssertVectors(remote.Joints[i].Position, local.Joints[i].Position));
                    }
                    //GraspService.Access.ComputeGraspPose(example_posture, MJointType.LeftWrist, );
                    //Console.Write(desc.Name);
                    thriftTest = true;
                }
            }
            Assert.IsTrue(thriftTest);

        }*/


    }
}
