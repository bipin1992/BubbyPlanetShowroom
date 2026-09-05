import asyncio
from pathlib import Path

import edge_tts

OUT = Path(r"C:\Users\atbip\OneDrive\Documents\MediaBubbyplanet")
OUT.mkdir(parents=True, exist_ok=True)

# Same voice as bubby-reward-system-girl10.mp3
VOICE = "hi-IN-SwaraNeural"
RATE = "+16%"
PITCH = "+70Hz"

HINDI = """नमस्ते दोस्तों! मैं बब्बी प्लैनेट की छोटी सी सहेली हूँ। आज एक बड़ी प्यारी बात बताऊँगी। अच्छा से सुनना हाँ?

सुनो सुनो! बब्बी प्लैनेट पर सेल लग गई है! सेल! वाह!

इस सेल में चालीस प्रतिशत तक छूट है! चालीस प्रतिशत तक! कितना अच्छा!

और सुनो, ये तो और भी प्यारा है! एक पर एक फ्री भी है! एक लोगे, तो एक फ्री! समझ गए न?

चालीस प्रतिशत तक छूट, और एक पर एक फ्री! दोनों साथ! कितनी खुशी!

मम्मी पापा को पकड़ के ले आना। सेल छूट ना जाए। बब्बी प्लैनेट चलो! बाय बाय!"""


async def main() -> None:
    path = OUT / "bubby-sale-40-percent.mp3"
    communicate = edge_tts.Communicate(HINDI, VOICE, rate=RATE, pitch=PITCH)
    await communicate.save(str(path))
    print(f"Wrote {path} ({path.stat().st_size} bytes)")


if __name__ == "__main__":
    asyncio.run(main())
