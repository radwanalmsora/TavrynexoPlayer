# Builds src/TavrynexoPlayer/Languages/<code>.json from tools/langs/b*.txt (positional, order = keys.txt)
import json, glob, os, re, sys
here = os.path.dirname(os.path.abspath(__file__))
keys = [l.strip() for l in open(os.path.join(here, "langs", "keys.txt"), encoding="utf-8") if l.strip()]
out = os.path.join(os.path.dirname(here), "src", "TavrynexoPlayer", "Languages")
os.makedirs(out, exist_ok=True)
langs = {}
for f in sorted(glob.glob(os.path.join(here, "langs", "b*.txt"))):
    code, lines = None, []
    def flush():
        if code is not None:
            langs[code] = lines[:]
    for raw in open(f, encoding="utf-8").read().split("\n"):
        if raw.startswith("@"):
            flush(); code = raw[1:].strip(); lines = []
        else:
            lines.append(raw)
    flush()
# trailing empty line cleanup
for c in langs:
    while langs[c] and langs[c][-1] == "": langs[c].pop()
langs["en-GB"] = langs["en"][:]
if "pt-BR" in langs and "pt" not in langs:
    pt = langs["pt-BR"][:]
    for i, (a, b) in enumerate([("Tela cheia", "Ecrã inteiro"), ("Capturar tela", "Capturar ecrã")]):
        pt = [x.replace(a, b) for x in pt]
    langs["pt"] = pt
ph = [i for i, l in enumerate(langs["en"]) if "{0}" in l]
bad = 0
for c, v in sorted(langs.items()):
    if len(v) != len(keys):
        print("BAD COUNT", c, len(v), "expected", len(keys)); bad += 1; continue
    for i in range(len(keys)):
        if ("{0}" in v[i]) != (i in ph):
            print("PLACEHOLDER MISMATCH", c, i + 1, keys[i], v[i]); bad += 1
    json.dump(dict(zip(keys, v)), open(os.path.join(out, c + ".json"), "w", encoding="utf-8"), ensure_ascii=False, indent=1)
print("languages:", len(langs), "bad:", bad)
print(sorted(langs))
