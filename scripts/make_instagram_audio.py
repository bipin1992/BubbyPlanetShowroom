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

सुनो सुनो! इंस्टाग्राम पर बब्बी प्लैनेट को जॉइन करो! इंस्टाग्राम!

इंस्टाग्राम खोलो, और सर्च करो — बब्बी प्लैनेट! एक शब्द में, बब्बी प्लैनेट! मिल जाएगा!

ताज़ा कलेक्शन देखने के लिए इंस्टाग्राम पर बब्बी प्लैनेट सर्च करो।
डिस्काउंट की जानकारी के लिए बब्बी प्लैनेट सर्च करो।
सेल की खबर सबसे पहले पाने के लिए बब्बी प्लैनेट सर्च करो।

लेटेस्ट कलेक्शन, डिस्काउंट, और सेल — सब इंस्टाग्राम पर! समझ गए न?

अभी इंस्टाग्राम खोलो, बब्बी प्लैनेट सर्च करो, और फॉलो करो। बाय बाय!"""


async def main() -> None:
    path = OUT / "bubby-instagram.mp3"
    communicate = edge_tts.Communicate(HINDI, VOICE, rate=RATE, pitch=PITCH)
    await communicate.save(str(path))
    print(f"Wrote {path} ({path.stat().st_size} bytes)")


if __name__ == "__main__":
    asyncio.run(main())
