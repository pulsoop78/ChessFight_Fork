"""Execute the actual intent adapter using stand-ins; no PhysX or ragdoll input simulation."""
from pathlib import Path
import shutil
import subprocess
import tempfile
import unittest
ROOT = Path(__file__).resolve().parents[2]

class ArmoryIntent(unittest.TestCase):
    @unittest.skipUnless(shutil.which('mcs') and shutil.which('mono'), 'Mono unavailable')
    def test_actual_voluntary_movement_and_forced_motion_exclusions(self):
        with tempfile.TemporaryDirectory() as temp:
            exe = str(Path(temp) / 'ArmoryIntent.exe')
            subprocess.run([
                'mcs', '-langversion:experimental', '-out:' + exe,
                str(ROOT / 'Assets/Scripts/RagdollLab/ArmoryPawnIntent.cs'),
                str(ROOT / 'Tests/Gameplay/Fixtures/ArmoryIntentHarness.cs'),
            ], check=True)
            subprocess.run(['mono', exe], check=True)
