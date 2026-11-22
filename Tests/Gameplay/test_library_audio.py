from pathlib import Path
import unittest,subprocess,tempfile,shutil,re
ROOT=Path(__file__).resolve().parents[2]
class LibraryAudio(unittest.TestCase):
    @unittest.skipUnless(shutil.which('mcs') and shutil.which('mono'),'Mono unavailable')
    def test_actual_fades_pause_and_cleanup(self):
        with tempfile.TemporaryDirectory() as folder:
            exe=str(Path(folder)/'audio.exe')
            subprocess.run(['mcs','-langversion:experimental','-out:'+exe,str(ROOT/'Assets/Scripts/RagdollLab/RoyalLibraryAudio.cs'),str(ROOT/'Tests/Gameplay/Fixtures/LibraryAudioHarness.cs')],check=True)
            subprocess.run(['mono',exe],check=True)
    def test_both_prefabs_have_audio_and_eight_edited_clips(self):
        guid=re.search(r'guid: (\w+)',(ROOT/'Assets/Scripts/RagdollLab/RoyalLibraryAudio.cs.meta').read_text())[1]
        for name in ('Assets/Prefabs/Obstacles/WarRoomComplete.prefab','Assets/Resources/RoyalPalace/WarRoomComplete.prefab'):
            self.assertEqual((ROOT/name).read_text().count('guid: '+guid),1)
        folder=ROOT/'Assets/Resources/RoyalLibraryAudio'
        self.assertEqual({p.stem for p in folder.glob('*.mp3')},{'Placement','Typewriter','Thinking','Stamp','BookOpen','BookClose','Portal','Ambience'})
        for p in folder.glob('*.mp3'):
            self.assertGreater(p.stat().st_size,1000);self.assertTrue(Path(str(p)+'.meta').exists())
