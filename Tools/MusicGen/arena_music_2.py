# Generates Assets/Audio/Resources/Music/arena.ogg, the second arena track (José, 2026-10-29: the
# first one, arena_music.py, got tiresome): a chiptune in E minor at 132 BPM, 48 bars (~87 s) in
# six parts so it takes a while to repeat. Triangle bass, square lead, thin arpeggios and noise
# drums; it loops without a seam (note tails wrap to the start, the last bar's B leads back to Em).
# Needs numpy and ffmpeg.  python3 arena_music_2.py <out.ogg>
import sys, subprocess, numpy as np

SR = 44100
BPM = 132
STEP = 60 / BPM / 4                 # one 16th
BARS = 48
L = int(round(BARS * 16 * STEP * SR))
out = np.zeros((L, 2))
rng = np.random.default_rng(29)

NOTES = {'C': 0, 'C#': 1, 'D': 2, 'D#': 3, 'E': 4, 'F': 5, 'F#': 6, 'G': 7, 'G#': 8, 'A': 9, 'Bb': 10, 'B': 11}
def freq(name):
    n, o = name[:-1], int(name[-1])
    return 440 * 2 ** ((NOTES[n] + 12 * (o + 1) - 69) / 12)

def octave(name, up):
    return name[:-1] + str(int(name[-1]) + up)

def add(signal, at, pan=0.0, gain=1.0):
    start = int(round(at * SR)) % L
    idx = (np.arange(len(signal)) + start) % L
    out[idx, 0] += signal * gain * (1 - pan) / 2 * 2 ** 0.5
    out[idx, 1] += signal * gain * (1 + pan) / 2 * 2 ** 0.5

def envelope(t, dur, attack, decay):
    env = np.minimum(1, t / attack)
    if decay:
        env *= np.exp(-t / decay)
    rel = min(len(t), int(0.02 * SR))
    env[-rel:] *= np.linspace(1, 0, rel)
    return env

def pulse(f, dur, duty=0.5, attack=0.004, decay=None, vibrato=0.0):
    t = np.arange(int(dur * SR)) / SR
    ph = f * t
    if vibrato:
        ph = ph + vibrato * np.sin(2 * np.pi * 5.5 * t) * np.clip(t * 4 - 0.6, 0, 1) / (2 * np.pi * 5.5) * f / 100
    w = np.where((ph % 1) < duty, 1.0, -1.0)
    return w * envelope(t, dur, attack, decay)

def triangle(f, dur, decay=None):
    t = np.arange(int(dur * SR)) / SR
    ph = (f * t) % 1
    # the NES triangle is 16 steps high
    w = np.round((4 * np.abs(ph - 0.5) - 1) * 8) / 8
    return w * envelope(t, dur, 0.003, decay)

def smooth(x, k):
    # a cheap low pass: moving average over k samples
    return np.convolve(x, np.ones(k) / k, mode='same')

def kick():
    t = np.arange(int(0.2 * SR)) / SR
    f = 42 + 120 * np.exp(-t / 0.025)
    return np.sin(2 * np.pi * np.cumsum(f) / SR) * np.exp(-t / 0.08)

def snare():
    t = np.arange(int(0.18 * SR)) / SR
    noise = rng.uniform(-1, 1, len(t))
    return 0.75 * noise * np.exp(-t / 0.06) + 0.45 * np.sin(2 * np.pi * 200 * t) * np.exp(-t / 0.03)

def hat(length=0.03):
    t = np.arange(int(length * SR)) / SR
    noise = rng.uniform(-1, 1, len(t))
    return (noise - smooth(noise, 6)) * np.exp(-t / (length / 3))

def crash():
    t = np.arange(int(1.2 * SR)) / SR
    noise = rng.uniform(-1, 1, len(t))
    return (noise - smooth(noise, 4)) * np.exp(-t / 0.35)

CHORD = {
    'Em': ['E', 'G', 'B'], 'C': ['C', 'E', 'G'], 'G': ['G', 'B', 'D'], 'D': ['D', 'F#', 'A'],
    'Am': ['A', 'C', 'E'], 'B': ['B', 'D#', 'F#'],
}
PROG_A = ['Em', 'C', 'G', 'D']
PROG_B = ['Am', 'D', 'G', 'B']
PROG_C = ['C', 'D', 'Em', 'Em', 'C', 'D', 'B', 'B']

# melodies, (note, 16ths) per bar; 'R' is a rest
MEL_A = [
    [('E5', 4), ('G5', 2), ('B5', 2), ('A5', 2), ('G5', 2), ('F#5', 2), ('G5', 2)],
    [('E5', 6), ('G5', 2), ('C6', 4), ('B5', 2), ('G5', 2)],
    [('D5', 4), ('G5', 4), ('B5', 2), ('A5', 2), ('G5', 4)],
    [('F#5', 6), ('A5', 2), ('D6', 4), ('C6', 2), ('A5', 2)],
    [('B5', 4), ('G5', 2), ('E5', 2), ('G5', 2), ('B5', 2), ('E6', 4)],
    [('C6', 4), ('B5', 2), ('G5', 2), ('E5', 4), ('G5', 4)],
    [('B5', 2), ('A5', 2), ('G5', 2), ('D5', 2), ('G5', 4), ('A5', 4)],
    [('F#5', 8), ('D5', 2), ('E5', 2), ('F#5', 2), ('A5', 2)],
]
MEL_B = [
    [('A5', 4), ('C6', 4), ('B5', 2), ('A5', 2), ('E5', 4)],
    [('F#5', 4), ('A5', 4), ('D6', 4), ('C6', 2), ('A5', 2)],
    [('B5', 6), ('G5', 2), ('D5', 4), ('G5', 4)],
    [('D#5', 4), ('F#5', 4), ('B5', 6), ('A5', 2)],
    [('C6', 2), ('B5', 2), ('A5', 4), ('E5', 2), ('A5', 2), ('C6', 4)],
    [('D6', 4), ('C6', 2), ('A5', 2), ('F#5', 4), ('A5', 4)],
    [('G5', 4), ('B5', 4), ('D6', 4), ('B5', 2), ('G5', 2)],
    [('F#5', 2), ('A5', 2), ('B5', 2), ('D#6', 2), ('B5', 8)],
]
MEL_C = [
    [('G5', 8), ('E5', 8)],
    [('F#5', 8), ('A5', 8)],
    [('G5', 12), ('F#5', 2), ('E5', 2)],
    [('E5', 16)],
    [('C6', 8), ('B5', 8)],
    [('A5', 8), ('F#5', 8)],
    [('D#5', 8), ('F#5', 8)],
    [('B5', 12), ('R', 4)],
]

# (first bar, bars, progression, melody, style)
PARTS = [
    (0, 4, PROG_A, None, 'intro'),
    (4, 8, PROG_A, MEL_A, 'rock'),
    (12, 8, PROG_B, MEL_B, 'drive'),
    (20, 4, PROG_A, None, 'break'),
    (24, 8, PROG_C, MEL_C, 'half'),
    (32, 8, PROG_A, MEL_A, 'high'),
    (40, 8, PROG_B, MEL_B, 'drive'),
]

def bar_time(b): return b * 16 * STEP

def lead(phrase, t0, duty, gain, up=0, pan=0.2):
    step = 0
    for name, length in phrase:
        if name != 'R':
            sig = pulse(freq(octave(name, up)), length * STEP * 0.95, duty=duty, decay=1.0, vibrato=1.2)
            add(sig, t0 + step * STEP, pan=pan, gain=gain)
            add(sig, t0 + (step + 3) * STEP, pan=-pan * 1.6, gain=gain * 0.3)   # echo
        step += length

for first, count, prog, melody, style in PARTS:
    for k in range(count):
        b = first + k
        t0 = bar_time(b)
        chord = prog[k % len(prog)]
        root = CHORD[chord][0]
        tones = CHORD[chord]
        last_of_part = k == count - 1

        # bass: a gallop (8th + two 16ths) in rock and drive, long notes in the half-time and the break
        if style in ('half', 'break'):
            for i in range(2):
                add(triangle(freq(root + '2'), STEP * 7.6), t0 + i * 8 * STEP, gain=0.55)
        else:
            for beat in range(4):
                for off, ln in ((0, 2), (2, 1), (3, 1)):
                    if style == 'intro' and off:
                        continue
                    note = root + ('3' if beat % 2 and off == 0 and style == 'drive' else '2')
                    add(triangle(freq(note), STEP * ln * 0.9), t0 + (beat * 4 + off) * STEP, gain=0.55)

        # arpeggio: chord tones up and down, thin pulse
        if style in ('drive', 'break', 'high', 'half'):
            seq = [0, 1, 2, 1] if style != 'break' else [0, 1, 2, 3]
            for i in range(16):
                j = seq[i % 4]
                note = tones[j % 3] + ('5' if j == 3 else '4')
                add(pulse(freq(note), STEP * 0.85, duty=0.125, decay=0.07), t0 + i * STEP, pan=-0.4,
                    gain=0.09 if style == 'break' else 0.055)

        # pad in the break and the half-time: soft held chord
        if style in ('break', 'half'):
            for n in tones:
                sig = pulse(freq(n + '4'), 16 * STEP, duty=0.5, attack=0.25, decay=2.5)
                add(smooth(sig, 12), t0, pan=0.0, gain=0.035)

        # lead
        if melody is not None:
            phrase = melody[k % len(melody)]
            if style == 'high':
                lead(phrase, t0, 0.25, 0.12, pan=0.25)
                lead(phrase, t0, 0.5, 0.06, up=-1, pan=-0.3)     # an octave under it
            elif style == 'half':
                lead(phrase, t0, 0.125, 0.15, pan=0.15)
            else:
                lead(phrase, t0, 0.5, 0.12, pan=0.2)

        # drums
        if k == 0 and style in ('rock', 'drive', 'high', 'half'):
            add(crash(), t0, pan=-0.2, gain=0.1)
        if style == 'break':
            if last_of_part:
                for i in range(16):   # build back in
                    add(snare(), t0 + i * STEP, gain=0.05 + 0.012 * i)
            continue
        if style == 'half':
            add(kick(), t0, gain=0.8)
            add(snare(), t0 + 8 * STEP, gain=0.38)
            if k % 2:
                add(kick(), t0 + 10 * STEP, gain=0.6)
            for i in range(4):
                add(hat(0.05), t0 + i * 4 * STEP, pan=0.2, gain=0.08)
        else:
            kicks = [0, 8] if style == 'intro' else [0, 6, 8] if style in ('rock', 'high') else [0, 4, 8, 12]
            for s in kicks:
                add(kick(), t0 + s * STEP, gain=0.75)
            if style != 'intro':
                for s in (4, 12):
                    add(snare(), t0 + s * STEP, gain=0.35)
            for i in range(8):
                open_hat = style == 'drive' and i % 2 == 1
                add(hat(0.08 if open_hat else 0.03), t0 + i * 2 * STEP, pan=0.2, gain=0.1 if i % 2 else 0.07)
        if last_of_part and style != 'intro':
            for i in range(12, 16):   # a short fill into the next part
                add(snare(), t0 + i * STEP, gain=0.2)

out /= np.max(np.abs(out)) / 0.5     # about -18 LUFS like the first track, under the effects
pcm = (out * 32767).astype('<i2').tobytes()
subprocess.run(['ffmpeg', '-y', '-loglevel', 'error', '-f', 's16le', '-ar', str(SR), '-ac', '2', '-i', '-',
                '-c:a', 'libvorbis', '-q:a', '5', sys.argv[1]], input=pcm, check=True)
print(f'{sys.argv[1]}: {L / SR:.1f} s')
