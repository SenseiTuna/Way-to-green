"""Original instrumental: Tideglass, 76 BPM, D minor; no samples or external music.
Renders a seamless 32-bar electric-piano/pad/bass composition and a separate surf SFX.
"""
import math, wave, array, pathlib, random, json
root=pathlib.Path(__file__).resolve().parents[1]/'KiyiDongusu/Assets/_Game/Resources/Audio'
root.mkdir(parents=True,exist_ok=True)
sr=22050; beat=60/76; duration=32*4*beat; n=round(duration*sr)
left=array.array('f',[0])*n; right=array.array('f',[0])*n
def note(midi,start,length,amp,kind='keys',pan=0):
    frequency=440*2**((midi-69)/12); count=int(length*sr)
    for k in range(count):
        t=k/sr; env=min(1,t/.018)*min(1,(length-t)/.22)
        if kind=='pad': v=(math.sin(2*math.pi*frequency*t)+.35*math.sin(2*math.pi*frequency*1.002*t))*.5*env
        elif kind=='bass': v=math.sin(2*math.pi*frequency*t)*math.exp(-t*2)*env
        else: v=(math.sin(2*math.pi*frequency*t)+.25*math.sin(2*math.pi*frequency*2*t)*math.exp(-t*4)+.08*math.sin(2*math.pi*frequency*3*t))*math.exp(-t*1.65)*env
        index=(round(start*sr)+k)%n
        left[index]+=v*amp*(1-pan)*.5;right[index]+=v*amp*(1+pan)*.5
chords=[[62,65,69,72,76],[58,62,65,69],[60,65,69,72,79],[60,64,67,74],[55,58,62,65,69],[58,62,65,69],[57,60,64,67],[62,65,69,72,76]]
melody=[81,79,77,76,74,77,76,72,74,76,77,79,76,74,72,69]
for bar in range(32):
    start=bar*4*beat; chord=chords[bar%8]
    for j,p in enumerate(chord):
        note(p,start+j*.035,2.4,.075,pan=(j%2*2-1)*.26)
        note(p-12,start,4*beat+.7,.018,'pad',pan=(j%2*2-1)*.4)
    note(chord[0]-24,start,1.1,.12,'bass')
    note(chord[0]-12,start+2.5*beat,.8,.055,'bass')
    if bar%4!=3:
        note(melody[(bar*2)%16],start+1.5*beat,.95,.065,pan=.2)
        note(melody[(bar*2+1)%16],start+3*beat,.8,.045,pan=-.18)
peak=max(max(abs(x) for x in left),max(abs(x) for x in right));scale=.43/peak
pcm=array.array('h')
for a,b in zip(left,right):pcm.extend((int(a*scale*32767),int(b*scale*32767)))
with wave.open(str(root/'Tideglass.wav'),'wb') as f:f.setparams((2,2,sr,0,'NONE','not compressed'));f.writeframes(pcm.tobytes())
rng=random.Random(2406); surf=array.array('h'); low=0.; last=0.; seconds=9
for k in range(sr*seconds):
    t=k/sr; noise=rng.uniform(-1,1);low=.94*low+.06*noise
    envelope=math.sin(math.pi*t/seconds)**2*(.7+.3*math.sin(t*1.2)**2)
    value=(low*1.9+(noise-last)*.035)*envelope*.52;last=noise
    surf.append(int(max(-1,min(1,value))*32767))
with wave.open(str(root/'ShoreWave.wav'),'wb') as f:f.setparams((1,2,sr,0,'NONE','not compressed'));f.writeframes(surf.tobytes())
report={'title':'Tideglass','method':'Original deterministic offline synthesis; not an AI music-service output','bpm':76,'bars':32,'seconds':duration,'sampleRate':sr,'musicPeak':.43,'sources':'No external recordings or samples'}
evidence=pathlib.Path(__file__).resolve().parents[1]/'Evidence/audio-generation.json';evidence.write_text(json.dumps(report,indent=2));print(json.dumps(report))
