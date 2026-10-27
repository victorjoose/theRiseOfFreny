# Generates Assets/Audio/Resources/Music/arena.ogg: a chiptune loop in D minor (Dm Bb C A) at
# 150 BPM, 32 bars, that loops without a seam (note tails wrap to the start).
# Needs numpy and ffmpeg.  python3 arena_music.py <out.ogg>
import sys, subprocess, numpy as np

SR = 44100
BPM = 150
STEP = 60 / BPM / 4                 # one 16th
BARS = 32
L = int(round(BARS * 16 * STEP * SR))
out = np.zeros((L, 2))
rng = np.random.default_rng(7)

NOTES = {'C': 0, 'C#': 1, 'D': 2, 'Eb': 3, 'E': 4, 'F': 5, 'F#': 6, 'G': 7, 'G#': 8, 'A': 9, 'Bb': 10, 'B': 11}
def freq(name):
    n, o = name[:-1], int(name[-1])
    return 440 * 2 ** ((NOTES[n] + 12 * (o + 1) - 69) / 12)

def add(signal, at, pan=0.0, gain=1.0):
    start = int(round(at * SR)) % L
    idx = (np.arange(len(signal)) + start) % L
    out[idx, 0] += signal * gain * (1 - pan) / 2 * 2 ** 0.5
    out[idx, 1] += signal * gain * (1 + pan) / 2 * 2 ** 0.5

def pulse(f, dur, duty=0.5, attack=0.004, decay=None, vibrato=0.0):
    t = np.arange(int(dur * SR)) / SR
    ph = f * t
    if vibrato:
        ph = ph + vibrato * np.sin(2 * np.pi * 5.5 * t) * np.clip(t * 4 - 0.6, 0, 1) / (2 * np.pi * 5.5) * f / 100
    w = np.where((ph % 1) < duty, 1.0, -1.0)
    env = np.minimum(1, t / attack)
    env *= np.exp(-t / decay) if decay else 1
    rel = int(0.02 * SR)
    env[-rel:] *= np.linspace(1, 0, rel)
    return w * env

def lowpass(x, cutoff):
    a = np.exp(-2 * np.pi * cutoff / SR)
    y = np.zeros_like(x); acc = 0.0
    for i, v in enumerate(x):
        acc = (1 - a) * v + a * acc; y[i] = acc
    return y

def kick():
    t = np.arange(int(0.18 * SR)) / SR
    f = 45 + 110 * np.exp(-t / 0.03)
    return np.sin(2 * np.pi * np.cumsum(f) / SR) * np.exp(-t / 0.07)

def snare():
    t = np.arange(int(0.16 * SR)) / SR
    noise = rng.uniform(-1, 1, len(t))
    return (0.7 * noise * np.exp(-t / 0.05) + 0.5 * np.sin(2 * np.pi * 185 * t) * np.exp(-t / 0.04))

def hat(length=0.03):
    t = np.arange(int(length * SR)) / SR
    noise = rng.uniform(-1, 1, len(t))
    noise = noise - lowpass(noise, 6000)
    return noise * np.exp(-t / (length / 3))

CHORDS = [('D', ['D', 'F', 'A']), ('Bb', ['Bb', 'D', 'F']), ('C', ['C', 'E', 'G']), ('A', ['A', 'C#', 'E'])]

LEAD_A = [
    [('A4', 4), ('D5', 2), ('F5', 2), ('E5', 2), ('D5', 2), ('C5', 2), ('D5', 2)],
    [('F5', 6), ('D5', 2), ('Bb4', 4), ('C5', 2), ('D5', 2)],
    [('E5', 4), ('G5', 4), ('F5', 2), ('E5', 2), ('C5', 4)],
    [('C#5', 6), ('E5', 2), ('A5', 4), ('G5', 2), ('E5', 2)],
]
LEAD_B = [
    [('D5', 2), ('F5', 2), ('A5', 4), ('G5', 2), ('F5', 2), ('E5', 2), ('F5', 2)],
    [('D5', 4), ('F5', 4), ('Bb5', 4), ('A5', 2), ('F5', 2)],
    [('G5', 4), ('E5', 2), ('C5', 2), ('E5', 4), ('G5', 4)],
    [('A5', 8), ('C#5', 4), ('E5', 4)],
]

def bar_time(b): return b * 16 * STEP

for b in range(BARS):
    root, tones = CHORDS[b % 4]
    t0 = bar_time(b)
    section = b // 4          # 0 intro, 1 +arp, 2-3 lead A/B, 4-5 lead high, 6 breakdown, 7 build
    breakdown = section == 6
    # bass: octave-jumping 8ths
    for i in range(8):
        if breakdown and i % 2:
            continue
        note = root + ('2' if i % 2 == 0 else '3')
        sig = lowpass(pulse(freq(note), STEP * 2 * 0.9, duty=0.25, decay=0.25), 1400)
        add(sig, t0 + i * 2 * STEP, gain=0.32)
    # arpeggio
    if section >= 1:
        for i in range(16):
            note = tones[i % 3] + ('5' if i % 6 < 3 else '4')
            add(pulse(freq(note), STEP * 0.9, duty=0.125, decay=0.06), t0 + i * STEP, pan=-0.35, gain=0.07 if not breakdown else 0.1)
    # lead
    if section in (2, 3, 4, 5) or (breakdown and b % 2 == 0):
        phrase = LEAD_A if section in (2, 4, 6) else LEAD_B
        step = 0
        for name, length in phrase[b % 4]:
            if section in (4, 5):
                name = name[:-1] + str(int(name[-1]) + 1) if b % 4 == 3 and length >= 6 else name
            sig = pulse(freq(name), length * STEP * 0.95, duty=0.5 if section < 4 else 0.25, decay=1.2, vibrato=1.0)
            sig = lowpass(sig, 3500)
            add(sig, t0 + step * STEP, pan=0.25, gain=0.14)
            add(sig, t0 + step * STEP + 3 * STEP, pan=-0.4, gain=0.04)   # echo
            step += length
    # drums
    if not breakdown:
        for beat in range(4):
            add(kick(), t0 + beat * 4 * STEP, gain=0.75)
            if section >= 2 and beat % 2 == 1:
                add(snare(), t0 + beat * 4 * STEP, gain=0.33)
        if section >= 2 and b % 4 == 3:
            add(kick(), t0 + 14 * STEP, gain=0.6)
        for i in range(8):
            add(hat(0.03 if i % 2 == 0 else 0.07), t0 + i * 2 * STEP, pan=0.2, gain=0.12 if i % 2 else 0.08)
    else:
        add(kick(), t0, gain=0.6)
    if section == 7 and b % 4 == 3:
        for i in range(8, 16):
            add(snare(), t0 + i * STEP, gain=0.12 + 0.03 * (i - 8))

out /= np.max(np.abs(out)) / 0.6     # about -18 LUFS, under the effects
pcm = (out * 32767).astype('<i2').tobytes()
subprocess.run(['ffmpeg', '-y', '-loglevel', 'error', '-f', 's16le', '-ar', str(SR), '-ac', '2', '-i', '-',
                '-c:a', 'libvorbis', '-q:a', '5', sys.argv[1]], input=pcm, check=True)
print(f'{sys.argv[1]}: {L / SR:.1f} s')
