import unittest
from pathlib import Path
import sys

def test():
    unittest.main(verbosity=3)
    
if __name__ == '__main__':
    sys.path.insert(0, Path(r"C:\MOSIM\Dennis\Repo\BlenderIKService"))
    print(Path.cwd())
    test()