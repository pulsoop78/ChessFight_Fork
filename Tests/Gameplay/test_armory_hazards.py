"""Production armory hazard controllers under engine stand-ins; not a Unity/PhysX test."""
from pathlib import Path
import shutil
import subprocess
import tempfile
import unittest
ROOT=Path(__file__).resolve().parents[2]
class ArmoryHazards(unittest.TestCase):
    @unittest.skipUnless(shutil.which('mcs') and shutil.which('mono'),'Mono unavailable')
    def test_spear_safety_waves_and_dullahan_hit_windows(self):
        with tempfile.TemporaryDirectory() as temp:
            exe=str(Path(temp)/'ArmoryHazards.exe')
            sources=[ROOT/'Tests/Gameplay/Fixtures/ArmoryHazardsHarness.cs',ROOT/'Assets/Scripts/Core/ArmorySpearPattern.cs',ROOT/'Assets/Scripts/Gameplay/ImportedObstacles/ArmorySpearFloor.cs',ROOT/'Assets/Scripts/Gameplay/ImportedObstacles/ArmoryDullahan.cs']
            subprocess.run(['mcs','-langversion:experimental','-out:'+exe,*map(str,sources)],check=True)
            subprocess.run(['mono',exe],check=True)
