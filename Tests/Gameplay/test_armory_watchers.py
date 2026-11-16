"""Production watcher gallery under math/query stand-ins, not Unity/PhysX."""
from pathlib import Path
import shutil, subprocess, tempfile, unittest
ROOT=Path(__file__).resolve().parents[2]
class ArmoryWatchers(unittest.TestCase):
    @unittest.skipUnless(shutil.which('mcs') and shutil.which('mono'),'Mono unavailable')
    def test_voluntary_movement_cones_cover_and_resets(self):
        with tempfile.TemporaryDirectory() as temp:
            exe=str(Path(temp)/'Watchers.exe')
            subprocess.run(['mcs','-langversion:experimental','-out:'+exe,str(ROOT/'Tests/Gameplay/Fixtures/ArmoryWatcherHarness.cs'),str(ROOT/'Assets/Scripts/Gameplay/ImportedObstacles/ArmoryWatcherGallery.cs')],check=True)
            subprocess.run(['mono',exe],check=True)
