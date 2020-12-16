// SPDX-License-Identifier: MIT
// The content of this file has been developed in the context of the MOSIM research project.
// Original author(s): Janis Sprenger

using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using CoordinateSystemMapper;
using MMIStandard;
using System.Collections.Generic;

namespace CoordinateSystemMapperTests
{ 

    public static class MoreAssert {
        public static double eps = 0.0000001d;
        public static bool AreEqual(MVector3 v, MVector3 v2)
        {
            if (!((v.X - v2.X < eps) && (v.Y - v2.Y < eps) && (v.Z - v2.Z < eps))) {
                throw new AssertFailedException("AreEquals Failed: " + v.ToString() + " vs. " + v2.ToString());
            } else
            {
                return true;
            }
        }
        public static bool AreEqual(MQuaternion v, MQuaternion v2)
        {
            if(! ((v.X - v2.X < eps) && (v.Y - v2.Y < eps) && (v.Z - v2.Z < eps) && (v.W - v2.W < eps)))
            {
                throw new AssertFailedException("AreEquals Failed: " + v.ToString() + " vs. " + v2.ToString());
            } else
            {
                return true;
            }

        }

        public static void AreEqual(MTransform t, MTransform t2)
        {
            if(!((t.ID.Equals(t2.ID)) && (AreEqual(t.Position, t2.Position)) && (AreEqual(t.Rotation, t2.Rotation))))
            {
                throw new AssertFailedException("AreEquals Failed: " + t.ToString() + " vs. " + t2.ToString());
            }
        }
    }


    [TestClass]
    public class UnitTest1
    {

        private CoordinateSystemMapperImpl mapper = new CoordinateSystemMapperImpl(new MIPAddress(), new MIPAddress());
        [TestMethod]
        public void TestIdentity()
        {
            List<MDirection> coordinateSystem = new List<MDirection>() { MDirection.Right, MDirection.Up, MDirection.Forward };
            MTransform t = new MTransform("asdf", new MVector3(1, 2, 3), new MQuaternion(1, 2, 3, 4));
            double qlength = t.Rotation.Length();
            t.Rotation.X /= qlength;
            t.Rotation.Y /= qlength;
            t.Rotation.Z /= qlength;
            t.Rotation.W /= qlength;

            MTransform t2 = mapper.TransformToMMI(t, coordinateSystem[0], coordinateSystem[1], coordinateSystem[2]);
            MoreAssert.AreEqual(t, t2);
            MTransform t3 = mapper.TransformFromMMI(t2, coordinateSystem[0], coordinateSystem[1], coordinateSystem[2]);
            MoreAssert.AreEqual(t2, t3);

            MQuaternion q1 = mapper.QuaternionToMMI(t.Rotation, coordinateSystem[0], coordinateSystem[1], coordinateSystem[2]);
            MoreAssert.AreEqual(t.Rotation, q1);
            MQuaternion q2 = mapper.QuaternionFromMMI(q1, coordinateSystem[0], coordinateSystem[1], coordinateSystem[2]);
            MoreAssert.AreEqual(q2, q1);
        
            MVector3 v1 = mapper.VectorToMMI(t.Position, coordinateSystem[0], coordinateSystem[1], coordinateSystem[2]);
            MoreAssert.AreEqual(t.Position, v1);
            MVector3 v2 = mapper.VectorFromMMI(v1, coordinateSystem[0], coordinateSystem[1], coordinateSystem[2]);
            MoreAssert.AreEqual(v2, v1);
        }

        [TestMethod]
        public void TestUnreal()
        {
            List<MDirection> coordinateSystem = new List<MDirection>() { MDirection.Forward, MDirection.Right, MDirection.Up };
            MTransform t = new MTransform("asdf", new MVector3(1, 2, 3), new MQuaternion(1, 2, 3, 4));
            double qlength = t.Rotation.Length();
            t.Rotation.X /= qlength;
            t.Rotation.Y /= qlength;
            t.Rotation.Z /= qlength;
            t.Rotation.W /= qlength;

            MTransform t1 = mapper.TransformToMMI(t, coordinateSystem[0], coordinateSystem[1], coordinateSystem[2]);
            Assert.IsTrue(t1.Rotation.X == t.Rotation.Y);
            Assert.IsTrue(t1.Rotation.Y == t.Rotation.Z);
            Assert.IsTrue(t1.Rotation.Z == t.Rotation.X);
            Assert.IsTrue(t1.Rotation.W == t.Rotation.W);

            Assert.IsTrue(t1.Position.X == t.Position.Y);
            Assert.IsTrue(t1.Position.Y == t.Position.Z);
            Assert.IsTrue(t1.Position.Z == t.Position.X);
            MTransform t2 = mapper.TransformFromMMI(t1, coordinateSystem[0], coordinateSystem[1], coordinateSystem[2]);
            MoreAssert.AreEqual(t2, t);
        }

        [TestMethod]
        public void TestDFKI()
        {
            List<MDirection> coordinateSystem = new List<MDirection>() { MDirection.Left, MDirection.Up, MDirection.Forward };
            MTransform t = new MTransform("asdf", new MVector3(1, 2, 3), new MQuaternion(1, 2, 3, 4));
            double qlength = t.Rotation.Length();
            t.Rotation.X /= qlength;
            t.Rotation.Y /= qlength;
            t.Rotation.Z /= qlength;
            t.Rotation.W /= qlength;

            MTransform t1 = mapper.TransformToMMI(t, coordinateSystem[0], coordinateSystem[1], coordinateSystem[2]);
            Assert.IsTrue(t1.Rotation.X == -t.Rotation.X);
            Assert.IsTrue(t1.Rotation.Y == t.Rotation.Y);
            Assert.IsTrue(t1.Rotation.Z == t.Rotation.Z);
            Assert.IsTrue(t1.Rotation.W == -t.Rotation.W);

            Assert.IsTrue(t1.Position.X == -t.Position.X);
            Assert.IsTrue(t1.Position.Y == t.Position.Y);
            Assert.IsTrue(t1.Position.Z == t.Position.Z);
            MTransform t2 = mapper.TransformFromMMI(t1, coordinateSystem[0], coordinateSystem[1], coordinateSystem[2]);
            MoreAssert.AreEqual(t2, t);
        }


        [TestMethod]
        public void TestBlender()
        {
            List<MDirection> coordinateSystem = new List<MDirection>() { MDirection.Right, MDirection.Forward, MDirection.Up };
            MTransform t = new MTransform("asdf", new MVector3(1, 2, 3), new MQuaternion(1, 2, 3, 4));
            double qlength = t.Rotation.Length();
            t.Rotation.X /= qlength;
            t.Rotation.Y /= qlength;
            t.Rotation.Z /= qlength;
            t.Rotation.W /= qlength;

            MTransform t1 = mapper.TransformToMMI(t, coordinateSystem[0], coordinateSystem[1], coordinateSystem[2]);
            Assert.AreEqual(t1.Rotation.X, t.Rotation.X);
            Assert.AreEqual(t1.Rotation.Y, t.Rotation.Z);
            Assert.AreEqual(t1.Rotation.Z, t.Rotation.Y);
            Assert.AreEqual(t1.Rotation.W, -t.Rotation.W);

            Assert.AreEqual(t1.Position.X, t.Position.X);
            Assert.AreEqual(t1.Position.Y, t.Position.Z);
            Assert.AreEqual(t1.Position.Z, t.Position.Y);
            MTransform t2 = mapper.TransformFromMMI(t1, coordinateSystem[0], coordinateSystem[1], coordinateSystem[2]);
            MoreAssert.AreEqual(t2, t);
        }
    }
}
