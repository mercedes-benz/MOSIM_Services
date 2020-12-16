import mathutils
import math
from typing import Tuple

#import MMIStandard.mmu.ttypes as tmmu
from MMIStandard.constraints.ttypes import MInterval, MInterval3

def intervalCenter(limit: MInterval) -> float:

    if math.isfinite(limit.Min) and math.isfinite(limit.Max):
        if limit.Min <= limit.Max:
            # happy case
            center = (limit.Min + limit.Max) / 2.
        else:
            raise ValueError("Lower limit greater than upper limit.")
            
    elif math.isfinite(limit.Min) and limit.Max == float('inf'):
        center = limit.Min
        
    elif limit.Min == float('-inf') and math.isfinite(limit.Max):
        center = limit.Max
    
    elif limit.Min == float('-inf') and limit.Max == float('inf'):
        center = 0.
        
    else:
        raise ValueError("Limits cannot be NaN, min=+inf and max=-inf")
        
    return center

def interval3Center(limits: MInterval3) -> tuple:
    
    center = (
        intervalCenter(limits.X),
        intervalCenter(limits.Y),
        intervalCenter(limits.Z)
    )
    return center
    
def euler_m2b(vec_m) -> mathutils.Euler:
    vec_b = (vec_m[0], -vec_m[1], -vec_m[2])
    return mathutils.Euler(vec_b, 'XZY')
    
def euler_b2m(vec) -> Tuple[float, float, float]:
    vec = (vec[0], -vec[1], -vec[2])
    return vec
    
def vector_m2b(vec) -> mathutils.Vector:
    vec = (-vec[0], vec[1], vec[2])
    return mathutils.Vector(vec)
    
def bPosition_to_MVector(position: mathutils.Vector) -> Tuple[float, float, float]:
    vec = (-position.x, position.y, position.z)
    return vec

def rotation_m2b(vec) -> mathutils.Quaternion:
    rot3 = rotation_y(-vec[1])@ rotation_x(vec[0]) @ rotation_z(-vec[2])
    return rot3.to_quaternion()
    
def rotation_x(angle):
    rot = mathutils.Matrix([
        (1,0,0), 
        (0, math.cos(angle), -math.sin(angle)), 
        (0, math.sin(angle), math.cos(angle))
    ])
    return rot

def rotation_y(angle):
    rot = mathutils.Matrix([
        (math.cos(angle), 0, math.sin(angle)), 
        (0, 1, 0), 
        (-math.sin(angle), 0, math.cos(angle))
    ])
    return rot

def rotation_z(angle):
    rot = mathutils.Matrix([
        (math.cos(angle), -math.sin(angle), 0), 
        (math.sin(angle), math.cos(angle), 0), 
        (0, 0, 1)
    ])
    return rot