
from datetime import datetime
import pickle
from pathlib import Path
import logging
logger = logging.getLogger(__name__)

class IKRecorder:
	
    def __init__(self, name = "unknown"):
        logger.info("New IKRecorder %s", name)
        self.name = name
        self.count = 0
        self.dir = Path.cwd().joinpath(self.name)
        logger.info("recording-folder: %s", self.dir.as_posix())
        if not self.dir.exists():
            logger.info("Create folder for recording: %s",  self.dir.as_posix())
            self.dir.mkdir()
        return
        
    def addRecord(self, payload):
        logger.debug("Add new Record to %s", self.name)
        data = (datetime.now(), payload)
        file = self.dir.joinpath(f"dump_{self.count}.pickle")
        self.count += 1
        with open(file, 'wb') as f:
            pickle.dump(data, f, pickle.HIGHEST_PROTOCOL)
        return
        
    @property
    def isempty(self):
        return bool(self.data)