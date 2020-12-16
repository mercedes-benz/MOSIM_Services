from mathutils import Vector
#import MMIStandard.mmu.ttypes as tmmu
from MMIStandard.constraints.ttypes import MInterval, MInterval3

from typing import Tuple
    
def rotationconstraint(mRotation: Tuple[float, float, float], limits: MInterval3) -> float:
    return _l1normbox(mRotation, limits)
    
def _l1normbox(position, limits) -> float:
    """ Returns zero if the position lies inside of the Box or the L1-norm 
    distances to the edge of the cube"""
    
    L1err = 0.
    L1err += max(limits.X.Min-position[0], 0.)
    L1err += max(position[0]-limits.X.Max, 0.)
        
    L1err += max(limits.Y.Min-position[1], 0.)
    L1err += max(position[1]-limits.Y.Max, 0.)
        
    L1err += max(limits.Z.Min-position[2], 0.)
    L1err += max(position[2]-limits.Z.Max, 0.)
        
    return L1err
    
def _l1normellipsoid(position, limits) -> float:
    """ Returns zero if position is inside the Ellipsoid, defined by the limits.
    If position is outside of the limits, the L1-norm to the closest Point on 
    the surface of the ellipsoid is returned."""
    
    L1err = 0.
    center = interval3Center(limits)
    rel = position - center
    
    k = rel.x^2/a^2 + rel.y^2/b^2 + rel.z^2/c^2
    if k > 1:
        err = 1 - 1/math.sqrt(k)
        L1err += rel.x * err
        L1err += rel.y * err
        L1err += rel.z * err
        
    return L1err
    
_HANDLERS = {
    'BOX': _l1normbox,
    0: _l1normbox,
    'ELLIPSOID': _l1normellipsoid,
    1: _l1normellipsoid,
}

def translationconstraint(mPosition: Vector, limits: MInterval3, shape) -> float:
                    
    try:
        return _HANDLERS[shape](mPosition, limits)
    except KeyError:
        raise ValueError("Unknown shape %s. Must be one of %s" % (shape, _HANDLERS.keys()))