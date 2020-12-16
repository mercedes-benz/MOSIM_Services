import unittest
import bpy
from pathlib import Path
import json

from BlenderMMI.IntermediateSkeletonApplication import IntermediateSkeletonApplication
from BlenderMMI.MAvatarPostureGenerator import JSON2MAvatarPosture

RESOURCES = Path(bpy.data.filepath).parent # not so clean!

class TestIntermediateSkeletonApplication(unittest.TestCase):

    @classmethod
    def setUpClass(cls):
        cls._m_avatar_posture = JSON2MAvatarPosture(RESOURCES/"intermediate.mos")
        with RESOURCES.joinpath('PostureValueCases.json').open() as file:
            cls._posture_value_cases = json.load(file)
        
    def setUp(self):
        self.app = IntermediateSkeletonApplication("lalala", self._m_avatar_posture)
		
    def test_prop_name(self):
        self.assertEqual(self.app.name, "lalala")
        
    def test_ApplyMAvatarPostureValues(self):
        """Make sure, the method runs at all."""
        self.app.ApplyMAvatarPostureValues(self._posture_value_cases['tpose'])
        
    def test_ReadMAvatarPostureValues(self):
        """Expect the T-Pose from the default-MAvatarPosture"""
        pose = self.app.ReadMAvatarPostureValues()
        for value, tpose in zip(pose, self._posture_value_cases['tpose']):
            self.assertAlmostEqual(value, tpose)
        
    def test_AddPositionConstraint(self):
        pass
        
    def test_getJointPosition(self):
        pass
        
    def test_AddRotationConstraint(self):
        pass
        
    def test_getJointRotation(self):
        pass