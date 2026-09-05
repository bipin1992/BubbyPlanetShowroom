import asyncio
from pathlib import Path

import edge_tts

OUT = Path(r"C:\Users\atbip\OneDrive\Documents\MediaBubbyplanet")
OUT.mkdir(parents=True, exist_ok=True)

# Same voice as bubby-reward-system-girl10.mp3
VOICE = "hi-IN-SwaraNeural"
RATE = "+16%"
PITCH = "+70Hz"

# Same cute-girl Hindi style as the reward audio.
HINDI = """नमस्ते दोस्तों! मैं बब्बी प्लैनेट की छोटी सी सहेली हूँ। आज एक प्यारी सी बात बताऊँगी। अच्छा से सुनना हाँ?

सुनो सुनो! आज बब्बी प्लैनेट पर पंद्रह प्रतिशत छूट है! पंद्रह प्रतिशत! वाह!

कपड़ों पर पंद्रह प्रतिशत छूट। कितना अच्छा!
जूतों पर पंद्रह प्रतिशत छूट। ये तो प्यारा है!
खिलौनों पर पंद्रह प्रतिशत छूट। कितनी खुशी!

कपड़े, जूते, खिलौने — सब पर पंद्रह प्रतिशत छूट! समझ गए न?

मम्मी पापा को पकड़ के ले आना। बब्बी प्लैनेट चलो! बाय बाय!"""


async def save(path: Path) -> None:
    communicate = edge_tts.Communicate(HINDI, VOICE, rate=RATE, pitch=PITCH)
    await communicate.save(str(path))
    print(f"Wrote {path} ({path.stat().st_size} bytes)")


async def main() -> None:
    for name in ("bubby-15-percent-kid.mp3", "bubby-15-percent-hindi.mp3"):
        try:
            await save(OUT / name)
        except Exception as ex:
            alt = OUT / "bubby-15-percent-girl10.mp3"
            print(f"Could not write {name}: {ex}")
            await save(alt)
            return


if __name__ == "__main__":
    asyncio.run(main())
