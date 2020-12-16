import unittest
import bpy
from pathlib import Path
import json
from math import pi

from BlenderMMI.MAvatarPostureGenerator import JSON2MAvatarPosture
from server.ikservice import IKService
import MMIStandard.services.ttypes as tservice
import MMIStandard.scene.ttypes as tscene
import MMIStandard.mmu.ttypes as tmmu



RESOURCES = Path(bpy.data.filepath).parent # not so clean!

class TestIKService(unittest.TestCase):

    @classmethod
    def setUpClass(cls):
        cls._m_avatar_posture = JSON2MAvatarPosture(RESOURCES/"intermediate.mos")
        with RESOURCES.joinpath('PostureValueCases.json').open() as file:
            cls._posture_value_cases = json.load(file)
        
    def setUp(self):
        self.adapter = IKService(self._m_avatar_posture)
        
    def test_ComputeIK(self):
        righthand = tscene.MEndeffectorType._NAMES_TO_VALUES['RightHand']
        posture = tscene.MAvatarPostureValues(AvatarID='lalala', 
            PostureData=self._posture_value_cases['tpose'])
        props = [tservice.MIKProperty(Values=[1., 1., 1.], Weight=1., Target=righthand, OperationType=0)]
        result = self.adapter.ComputeIK(posture, props)
        
    def test_CalculateIKPosture(self):
        posture = tscene.MAvatarPostureValues(AvatarID='lalala', 
            PostureData=self._posture_value_cases['tpose'])
        
        rightwrist = tscene.MJointType._NAMES_TO_VALUES['RightWrist']
        box = tmmu.MTranslationConstraintType._NAMES_TO_VALUES['BOX']
        
        trnl_x = tmmu.MInterval(Min=0.3, Max=0.5)
        trnl_y = tmmu.MInterval(Min=1.2, Max=1.4)
        trnl_z = tmmu.MInterval(Min=0.4, Max=0.6)
        tranllimits = tmmu.MInterval3(X=trnl_x, Y=trnl_y, Z=trnl_z)
        translation = tmmu.MTranslationConstraint(Type=box, Limits=tranllimits)
        
        rot_x = tmmu.MInterval(Min=-0.1, Max=0.1)
        rot_y = tmmu.MInterval(Min=pi/2.-0.1, Max=pi/2+0.1)
        rot_z = tmmu.MInterval(Min=pi/4, Max=pi/2)
        rotlimits = tmmu.MInterval3(X=rot_x, Y=rot_y, Z=rot_z)
        rotation = tmmu.MRotationConstraint(Limits=rotlimits)
        
        geoconstraint = tmmu.MGeometryConstraint(
            ParentObjectID='lalala',
            TranslationConstraint=translation,
            RotationConstraint=rotation,
        )
            
        jointconstraint = tmmu.MJointConstraint(JointType=rightwrist, 
            GeometryConstraint=geoconstraint)
        
        constraints = [
            tmmu.MConstraint(ID='Whatever', JointConstraint=jointconstraint)
        ]
        self.adapter.CalculateIKPosture(posture, constraints, {})