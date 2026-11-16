"""Production projectile and supply rules with engine stand-ins, not a PhysX playthrough."""
from pathlib import Path
import shutil,subprocess,tempfile,unittest
ROOT=Path(__file__).resolve().parents[2]
class ArmoryCrossbows(unittest.TestCase):
    @unittest.skipUnless(shutil.which('mcs') and shutil.which('mono'),'Mono unavailable')
    def test_bounded_projectiles_cover_and_safe_shield_recovery(self):
        with tempfile.TemporaryDirectory() as temp:
            exe=str(Path(temp)/'ArmoryCrossbows.exe')
            sources=[ROOT/'Tests/Gameplay/Fixtures/ArmoryCrossbowHarness.cs']
            sources += [ROOT/('Assets/Scripts/Gameplay/ImportedObstacles/'+name+'.cs') for name in ('ArmoryCarryShield','ArmoryCrossbowGallery')]
            subprocess.run(['mcs','-langversion:experimental','-out:'+exe,*map(str,sources)],check=True)
            subprocess.run(['mono',exe],check=True)
