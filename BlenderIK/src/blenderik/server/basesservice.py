
from typing import Dict
import logging

# MOSIM-declarations
import MMIStandard.services.MMIServiceBase
from MMIStandard.avatar.ttypes import MAvatarDescription
from MMIStandard.core.ttypes import MServiceDescription, MBoolResponse

logger = logging.getLogger(__name__)

class BaseService(MMIServiceBase.Iface):
	
    def GetStatus(self) -> Dict[str, str]:
        """
        Implementation for <MMIServiceBase>: Returns the present status of the 
        service.
        """
        #logger.info("Call to dummy-Method GetStatus. This is not properly implemented yet.")
        return {"Running": "True"} # dummy

    def GetDescription(self) -> MServiceDescription:
        """
        Implementation for <MMIServiceBase>: Returns the specific 
        <MServiceDescription> for the service.
        """
        logger.info("Call to dummy-Method GetDescription. This is not properly implemented yet.")
        return self.description # only exists in the Server!
		
    def Setup(self, description: MAvatarDescription, properties: Dict[str, str]) -> MBoolResponse: # argument names don't match specs
        """
        Implementation for <MMIServiceBase>: Basic method to setup the service. 
        This function can be used to reduce the network traffic. For instance, 
        instead of transferring the full hierarchy, only the posture values can 
        be transmitted if being initialized in before.
        
        Parameters:
         - description 
         - properties

        """
        logger.debug("Call to Setup")
        self.app.disableAllConstraints()
        bpy.context.view_layer.update()
        for b in self.app.object.pose.bones: # Should this be a method of self.app?
            b.rotation_mode="QUATERNION"
            b.rotation_quaternion = Quaternion((1,0,0,0))
            b.location = Vector((0,0,0))
        bpy.context.view_layer.update()
        #self.app.ScaleMAvatarPosture(description)
        return MBoolResponse(Successful=True)
    
    def Consume(self, properties: Dict[str, str]) -> Dict[str, str]:
        """
        Implementation for <MMIServiceBase>: Function to consume a service 
        without needing the explicit interface. This can be utilized if new 
        services are added to the framework which signature is not known yet.
        
        Parameters:
         - properties

        """
        logging.error("Call to Consume: Method is not implemented!")
        pass
