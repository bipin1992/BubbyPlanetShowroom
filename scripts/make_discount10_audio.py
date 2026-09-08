import asyncio
from pathlib import Path

import edge_tts

OUT = Path.home() / "Documents" / "MediaBubbyplanet"
OUT.mkdir(parents=True, exist_ok=True)

# Same voice as bubby-reward-system-girl10.mp3
VOICE = "hi-IN-SwaraNeural"
RATE = "+16%"
PITCH = "+70Hz"

HINDI = """नमस्ते दोस्तों! मैं बब्बी प्लैनेट की छोटी सी सहेली हूँ। आज एक प्यारी सी बात बताऊँगी। अच्छा से सुनना हाँ?

सुनो सुनो! बब्बी प्लैनेट पर हर आइटम पर दस प्रतिशत छूट है! दस प्रतिशत! वाह!

कपड़े हो, जूते हो, खिलौने हो — हर चीज़ पर दस प्रतिशत छूट! समझ गए न?

हर आइटम पर दस प्रतिशत! कितना अच्छा! कितनी खुशी!

मम्मी पापा को पकड़ के ले आना। बब्बी प्लैनेट चलो! बाय बाय!"""


async def main() -> None:
    path = OUT / "bubby-10-percent.mp3"
    communicate = edge_tts.Communicate(HINDI, VOICE, rate=RATE, pitch=PITCH)
    await communicate.save(str(path))
    print(f"Wrote {path} ({path.stat().st_size} bytes)")


if __name__ == "__main__":
    asyncio.run(main())
