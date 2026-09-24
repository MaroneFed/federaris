# -*- coding: utf-8 -*-
"""VERIFICATEUR STATIQUE DU PROJET FIEF.

Claude ne peut pas lancer Unity : il ne voit jamais ses propres erreurs de
compilation. Ce script les cherche a sa place, avant que tu ouvres l'editeur.

    python3 Tools/verifier.py

Il ne remplace pas un compilateur. Il attrape les cinq fautes qui sont
REELLEMENT arrivees sur ce projet :

  1. accolades ou parentheses desequilibrees
  2. appel a une methode qui n'existe pas (un bloc supprime par megarde)
  3. reference a Type.Membre qui n'existe pas
  4. appel avec le mauvais nombre d'arguments
  5. membre inexistant sur une VARIABLE (pas seulement sur un nom de type)
  6. CHAINE d'acces (Game.Hud.Hidden) et membre NON PUBLIC appele d'ailleurs
  7. TYPE INCONNU, et attribut orphelin devant une methode
  8. API Unity PERIMEE, devenue une erreur dans Unity 6 (GetInstanceID...)

Les trois derniers ont ete ajoutes apres coup, chacun parce qu'une faute est
passee jusqu'a Unity :

  - une methode oubliee utilisait une classe supprimee, avec un [Header] colle
    devant elle (CS0246 + CS0592) ;
  - un champ supprime de GameConfig restait appele via la VARIABLE config, ce
    que la verification par nom de type ne voyait pas (CS1061). Celle-la a mis
    Unity en Safe Mode.

  - Game.Hud.Hidden = ... ecrit dans le nouveau menu, alors que Hidden est
    prive dans Hud (CS0122). Rattrapee a la relecture, avant Unity cette fois ;
    le verificateur, lui, ne regardait jamais au-dela du premier point.

A chaque fois la regle a ete la meme : corriger le fichier ne suffit pas, il
faut apprendre la faute a l'outil, sinon elle revient.
"""
import io, os, re, sys
from collections import defaultdict

ROOT = "Assets/_Fief/Scripts"
files = []
for dp, dn, fn in os.walk(ROOT):
    for f in fn:
        if f.endswith(".cs"):
            files.append(os.path.join(dp, f))
files.sort()

def strip(src):
    out, i, n = [], 0, len(src)
    while i < n:
        c = src[i]
        if c == '/' and i + 1 < n and src[i+1] == '/':
            j = src.find('\n', i)
            i = n if j < 0 else j
        elif c == '/' and i + 1 < n and src[i+1] == '*':
            j = src.find('*/', i + 2)
            i = n if j < 0 else j + 2
        elif c == '"':
            if src[i-1:i] == '@':
                j = i + 1
                while j < n:
                    if src[j] == '"':
                        if src[j+1:j+2] == '"': j += 2; continue
                        break
                    j += 1
                out.append('0' * (j - i + 1)); i = j + 1
            else:
                j = i + 1
                while j < n and src[j] != '"':
                    j += 2 if src[j] == '\\' else 1
                out.append('0' * (j - i + 1)); i = j + 1
        elif c == "'":
            j = i + 1
            while j < n and src[j] != "'":
                j += 2 if src[j] == '\\' else 1
            out.append('0' * (j - i + 1)); i = j + 1
        else:
            out.append(c); i += 1
    return ''.join(out)

sources = {}
for path in files:
    sources[path] = strip(io.open(path, encoding="utf-8").read())

errors = []

# ---- 1. structure -------------------------------------------------------
for path, s in sources.items():
    for open_c, close_c, name in (('{', '}', 'accolade'), ('(', ')', 'parenthese')):
        depth, line = 0, 1
        for ch in s:
            if ch == '\n': line += 1
            elif ch == open_c: depth += 1
            elif ch == close_c:
                depth -= 1
                if depth < 0:
                    errors.append("%s:%d %s fermante en trop" % (path, line, name)); break
        if depth > 0:
            errors.append("%s : %d %s(s) non fermee(s)" % (path, depth, name))

# ---- collecte des types et membres -------------------------------------
BCL = set('''ReferenceEquals Equals GetType ToString GetHashCode Mathf Debug'''.split())

KEYWORDS = set("""if else for foreach while switch return new using namespace class struct enum
interface public private protected internal static readonly const void var this base try catch
finally throw lock in out ref is as typeof sizeof default do break continue case get set add
remove value where partial override virtual abstract sealed async await yield null true false
nameof checked unchecked stackalloc fixed unsafe when delegate event operator explicit implicit
params extern global record init required""".split())

type_members = defaultdict(set)      # Type -> {membre}
type_bases = {}                      # Type -> base
member_arity = defaultdict(set)      # (Type, membre) -> {nb args possibles}
type_of_file = defaultdict(list)

decl_re = re.compile(
    r'\b(?:public|private|protected|internal)?\s*(?:static\s+|readonly\s+|const\s+|override\s+|virtual\s+|abstract\s+|sealed\s+|partial\s+|async\s+|extern\s+|unsafe\s+|new\s+)*'
    r'(?:[\w<>\[\],\.\?]+\s+)?(\w+)\s*\(([^;{)]*(?:\([^)]*\)[^;{)]*)*)\)\s*(?:where[^{;]*)?\{')
type_re = re.compile(r'\b(?:class|struct|enum|interface)\s+(\w+)(?:\s*:\s*([\w<>,\s\.]+))?')
field_re = re.compile(
    r'\b(?:public|private|protected|internal)\s+(?:static\s+|readonly\s+|const\s+|event\s+|volatile\s+)*'
    r'[\w<>\[\],\.\?]+\s+(\w+)\s*(?:=|;|\{)')

def enclosing_types(src):
    """[(name, start, end)] par correspondance d'accolades"""
    result = []
    for m in type_re.finditer(src):
        name, bases = m.group(1), m.group(2)
        brace = src.find('{', m.end())
        if brace < 0: continue
        depth, i = 0, brace
        while i < len(src):
            if src[i] == '{': depth += 1
            elif src[i] == '}':
                depth -= 1
                if depth == 0: break
            i += 1
        result.append((name, brace, i, bases))
    return result

file_types = {}
for path, s in sources.items():
    ts = enclosing_types(s)
    file_types[path] = ts
    for name, a, b, bases in ts:
        type_members[name]          # enregistre le type meme s'il n'a aucun membre
        body = s[a:b]

        # Les membres d'un enum ne sont ni des champs ni des methodes : ce sont
        # des identifiants nus separes par des virgules. Sans ce cas particulier,
        # le verificateur declarait ResourceType.Wood inexistant.
        if re.search(r'\benum\s+' + re.escape(name) + r'\b', s):
            for piece in body.strip('{} \n\r\t').split(','):
                piece = piece.split('=')[0].strip()
                if re.match(r'^[A-Za-z_]\w*$', piece): type_members[name].add(piece)
            continue
        if bases:
            first = bases.split(',')[0].strip().split('<')[0]
            if first: type_bases[name] = first
        for m in decl_re.finditer(body):
            member, args = m.group(1), m.group(2)
            if member in KEYWORDS: continue
            type_members[name].add(member)
            args = args.strip()
            if not args:
                member_arity[(name, member)].add(0)
            else:
                depth, count = 0, 1
                optional = 0
                for part_i, ch in enumerate(args):
                    if ch in '(<[': depth += 1
                    elif ch in ')>]': depth -= 1
                    elif ch == ',' and depth == 0: count += 1
                for seg in re.split(r',(?![^(<\[]*[)>\]])', args):
                    if '=' in seg: optional += 1
                for k in range(count - optional, count + 1):
                    member_arity[(name, member)].add(k)
        for m in field_re.finditer(body):
            type_members[name].add(m.group(1))

        # Membres declares SANS modificateur d'acces. En C# un champ sans mot-cle
        # est prive, et une interface n'en met jamais -- deux cas que field_re,
        # qui exige public/private/..., ratait completement. Resultat : le
        # verificateur croyait que Poncho n'a pas de champ ringY et que
        # IInteractable n'a pas de methode Interact.
        #
        # On se repere a l'INDENTATION : dans ce depot un membre de classe est a
        # huit espaces, une variable locale a douze ou plus. C'est fragile en
        # general, fiable ici, et ca evite de gober toutes les locales comme
        # membres -- ce qui viderait la verification de son sens.
        for m in re.finditer(r'^        (?!return\b|new\b|if\b|for\b|while\b|foreach\b)'
                             r'(?:[\w<>\[\],\.\?]+\s+)+(\w+)\s*(?:;|=[^=]|\(|\{\s*get)',
                             body, re.M):
            if m.group(1) not in KEYWORDS: type_members[name].add(m.group(1))
        # Declarations GROUPEES : "float a, b, c;" est du C# valide, et declare
        # trois champs. Sans ce cas, seul le dernier etait vu (faux positif sur
        # Soundscape et Sky, le 24/09/2026).
        for m in re.finditer(r'^        (?:(?:public|private|protected|internal|static|readonly)\s+)*'
                             r'[\w<>\[\]\.\?]+\s+(\w+(?:\s*,\s*\w+)+)\s*;',
                             body, re.M):
            for piece in m.group(1).split(','):
                piece = piece.strip()
                if piece and piece not in KEYWORDS: type_members[name].add(piece)
        for m in re.finditer(r'\b(?:class|struct|enum)\s+(\w+)', body):
            type_members[name].add(m.group(1))

all_types = set(type_members) | set(type_bases)

def members_of(t, seen=None):
    seen = seen or set()
    if t in seen or t not in all_types: return set()
    seen.add(t)
    out = set(type_members.get(t, ()))
    b = type_bases.get(t)
    if b: out |= members_of(b, seen)
    return out

UNITY = {
    'MonoBehaviour': {'transform','gameObject','enabled','name','StartCoroutine','StopCoroutine',
        'Invoke','InvokeRepeating','CancelInvoke','GetComponent','GetComponentInChildren',
        'GetComponentsInChildren','GetComponents','AddComponent','Awake','Start','Update',
        'LateUpdate','FixedUpdate','OnEnable','OnDisable','OnDestroy','OnGUI','OnTriggerEnter',
        'OnTriggerExit','OnTriggerStay','OnCollisionEnter','OnCollisionExit','OnApplicationQuit',
        'OnDrawGizmos','tag','CompareTag','SendMessage','Destroy','DestroyImmediate','Instantiate',
        'DontDestroyOnLoad','FindObjectOfType','print','isActiveAndEnabled','hideFlags','useGUILayout'},
}
for t, b in list(type_bases.items()):
    pass

# ---- 2. appels non qualifies sans definition ----------------------------
call_re = re.compile(r'(?<![\w\.])(\w+)\s*\(')
for path, s in sources.items():
    for name, a, b, bases in file_types[path]:
        if re.search(r'\binterface\s+' + name + r'\b', s): continue   # declarations sans corps
        body = s[a:b]
        known = members_of(name)
        base = name
        chain = set()
        t = name
        while t:
            chain.add(t); t = type_bases.get(t)
        inherits_mono = 'MonoBehaviour' in chain or any(type_bases.get(x) == 'MonoBehaviour' for x in chain)
        if inherits_mono: known |= UNITY['MonoBehaviour']
        for m in call_re.finditer(body):
            fn = m.group(1)
            if fn in KEYWORDS or fn in all_types: continue
            if fn in known: continue
            # 'new Type(...)' n'est pas un appel de methode
            before = body[max(0, m.start() - 6):m.start()].rstrip()
            if before.endswith('new'): continue
            if before.endswith('['): continue          # attribut [Header(...)]
            if fn in BCL: continue
            # ignorer les appels sur des locales typees / API .NET-Unity
            line = body.count('\n', 0, m.start()) + 1
            errors.append("%s (%s) ligne ~%d : appel a %s() sans definition"
                          % (path, name, line, fn))

# ---- 3. references Type.Membre -----------------------------------------
ref_re = re.compile(r'\b([A-Z]\w*)\.(\w+)\b')
for path, s in sources.items():
    for m in ref_re.finditer(s):
        t, member = m.group(1), m.group(2)
        if t not in all_types: continue
        if member in members_of(t): continue
        line = s.count('\n', 0, m.start()) + 1
        errors.append("%s ligne %d : %s.%s n'existe pas" % (path, line, t, member))

# ---- 4. arite des appels Type.Membre(...) ------------------------------
arity_re = re.compile(r'\b([A-Z]\w*)\.(\w+)\s*\(')
def count_args(src, start):
    depth, i, n = 0, start, len(src)
    count, seen = 1, False
    while i < n:
        ch = src[i]
        if ch in '([{': depth += 1
        elif ch in ')]}':
            depth -= 1
            if depth == 0: return 0 if not seen else count
        elif ch == ',' and depth == 1: count += 1
        elif not ch.isspace(): seen = True
        i += 1
    return -1

for path, s in sources.items():
    for m in arity_re.finditer(s):
        t, member = m.group(1), m.group(2)
        if (t, member) not in member_arity: continue
        n = count_args(s, m.end() - 1)
        if n < 0: continue
        if n not in member_arity[(t, member)]:
            line = s.count('\n', 0, m.start()) + 1
            errors.append("%s ligne %d : %s.%s appele avec %d argument(s), attendu %s"
                          % (path, line, t, member, n, sorted(member_arity[(t, member)])))

# ---- 4b. membre inexistant sur une VARIABLE -----------------------------
#
# La passe 3 ne verifie que les acces par NOM DE TYPE (Palette.Banner). Elle ne
# voyait rien quand on passe par une variable -- et c'est exactement comme ca que
# config.playerFiefIndex a survecu a la suppression du champ, jusqu'a bloquer
# Unity en Safe Mode. On suit donc aussi les variables dont le type est une
# classe du projet.
#
# Une variable dont le nom designe DEUX types differents dans le meme fichier est
# ignoree : mieux vaut rater un cas que crier au loup.
decl_var_re = re.compile(r'(?<![\w.])([A-Z]\w*)\s+([a-z_]\w*)\s*(?==|;|,|\)|\s+in\b)')
member_re = re.compile(r'(?<![\w.])([a-z_]\w*)\.(\w+)')

for path, s2 in sources.items():
    # On note TOUS les types declares pour chaque nom, Unity compris : une variable
    # "c" qui est une Color dans une methode et une Cache dans une autre est ambigue
    # (le verificateur ne suit pas les portees), donc on l'ignore.
    holder = {}
    for m in decl_var_re.finditer(s2):
        holder.setdefault(m.group(2), set()).add(m.group(1))
    holder = dict((v, ts) for v, ts in holder.items() if len(ts) == 1 and next(iter(ts)) in all_types)

    for var, types in holder.items():
        if len(types) != 1: continue
        t = next(iter(types))
        known = members_of(t)

        chain = set()
        cur = t
        while cur:
            chain.add(cur); cur = type_bases.get(cur)
        if 'MonoBehaviour' in chain or any(type_bases.get(x) == 'MonoBehaviour' for x in chain):
            known |= UNITY['MonoBehaviour']

        for m in member_re.finditer(s2):
            if m.group(1) != var: continue
            if m.group(2) in known: continue
            line = s2.count('\n', 0, m.start()) + 1
            errors.append("%s ligne %d : %s est un %s, qui n'a pas de membre %s"
                          % (path, line, var, t, m.group(2)))

# ---- 4c. chaines d'acces et visibilite ---------------------------------
#
# Deux fautes que rien ne voyait, rattrapees a la relecture le 23/09 :
#
#   Game.Hud.Hidden = ...   ->  Hud.Hidden est PRIVE : Unity refuse (CS0122).
#
# La passe 3 verifiait Game.Hud, jamais ce qui suit. On suit donc la chaine :
# Game.Hud est un champ de type Hud, donc Hidden se cherche dans Hud -- et comme
# on y accede depuis une AUTRE classe, il doit etre public.

def_member_re = re.compile(
    r'^[ \t]*public\s+(?:static\s+|readonly\s+|const\s+|override\s+|virtual\s+|abstract\s+|'
    r'sealed\s+|new\s+|event\s+|async\s+)*(?:[\w<>\[\],\.\?]+\s+)?(\w+)\s*(?:[;=({]|$)', re.M)
typed_field_re = re.compile(
    r'^        (?:public\s+|private\s+|protected\s+|internal\s+)?(?:static\s+|readonly\s+)*'
    r'([A-Z]\w*)\s+(\w+)\s*(?:;|=|\{)', re.M)

public_members = defaultdict(set)
field_type = defaultdict(dict)
type_span = {}                              # type -> (fichier, debut, fin)
for path, s2 in sources.items():
    for name, a, b, bases in file_types[path]:
        type_span[name] = (path, a, b)
        body = s2[a:b]
        is_enum = re.search(r'\benum\s+' + re.escape(name) + r'\b', s2) is not None
        is_iface = re.search(r'\binterface\s+' + re.escape(name) + r'\b', s2) is not None
        if is_enum or is_iface:
            public_members[name] |= type_members[name]
            continue
        for m in def_member_re.finditer(body):
            public_members[name].add(m.group(1))
        for m in re.finditer(r'\bpublic\s+(?:static\s+)?(?:class|struct|enum|interface)\s+(\w+)', body):
            public_members[name].add(m.group(1))
        for m in typed_field_re.finditer(body):
            if m.group(1) in all_types: field_type[name][m.group(2)] = m.group(1)

def public_of(t, seen=None):
    seen = seen or set()
    if t in seen: return set()
    seen.add(t)
    out = set(public_members.get(t, ()))
    b = type_bases.get(t)
    if b == 'MonoBehaviour': out |= UNITY['MonoBehaviour']
    elif b: out |= public_of(b, seen)
    return out

def full_members(t):
    """Membres propres + herites, y compris ceux d'Unity pour un MonoBehaviour."""
    out = set(members_of(t))
    cur, seen = t, set()
    while cur and cur not in seen:
        seen.add(cur)
        if type_bases.get(cur) == 'MonoBehaviour':
            out |= UNITY['MonoBehaviour']
            break
        cur = type_bases.get(cur)
    return out

def enclosing(path, pos):
    best = None
    for name, a, b, bases in file_types[path]:
        if a <= pos <= b and (best is None or a >= best[1]): best = (name, a)
    return best[0] if best else None

def nested_in(inner, outer):
    if inner is None: return False
    if inner == outer: return True
    if outer not in type_span or inner not in type_span: return False
    pi, ai, bi = type_span[inner]; po, ao, bo = type_span[outer]
    return pi == po and ao <= ai and bi <= bo

chain_re = re.compile(r'(?<![\w.])([A-Za-z_]\w*)((?:\.[A-Za-z_]\w*)+)')
for path, s2 in sources.items():
    holder = {}
    for m in decl_var_re.finditer(s2):
        holder.setdefault(m.group(2), set()).add(m.group(1))
    holder = dict((v, ts) for v, ts in holder.items() if len(ts) == 1 and next(iter(ts)) in all_types)

    for m in chain_re.finditer(s2):
        head, rest = m.group(1), m.group(2).split('.')[1:]
        if head in all_types: cur = head
        elif head in holder and len(holder[head]) == 1: cur = next(iter(holder[head]))
        else: continue
        here = enclosing(path, m.start())
        line = s2.count('\n', 0, m.start()) + 1
        for member in rest:
            if cur not in all_types: break
            if member not in full_members(cur):
                if cur not in (head,) or head not in all_types:   # la passe 3 couvre deja Type.Membre
                    errors.append("%s ligne %d : %s n'a pas de membre %s" % (path, line, cur, member))
                break
            if not nested_in(here, cur) and member not in public_of(cur):
                errors.append("%s ligne %d : %s.%s n'est pas public, on ne peut pas y acceder "
                              "depuis %s (Unity refuse : CS0122)" % (path, line, cur, member, here))
                break
            if member in all_types: cur = member
            elif member in field_type.get(cur, {}): cur = field_type[cur][member]
            else: break

# ---- 5a. attribut orphelin : [Header] / [Tooltip] ne valent que sur un champ
attr_re = re.compile(r'\[\s*(Header|Tooltip|Range|Space)\s*\(')
for path, s2 in sources.items():
    for m in attr_re.finditer(s2):
        # fin de l'attribut, puis ce qui suit
        depth, i = 0, m.end() - 1
        while i < len(s2):
            if s2[i] == '(': depth += 1
            elif s2[i] == ')':
                depth -= 1
                if depth == 0: break
            i += 1
        rest = s2[i + 1:]
        j = 0
        # sauter les espaces ET les attributs qui suivent : [Header] [Tooltip] champ
        while j < len(rest):
            if rest[j] in ' \t\r\n]':
                j += 1
            elif rest[j] == '[':
                depth2 = 0
                while j < len(rest):
                    if rest[j] == '[': depth2 += 1
                    elif rest[j] == ']':
                        depth2 -= 1
                        if depth2 == 0: j += 1; break
                    j += 1
            else:
                break
        tail = rest[j:j + 400]
        if not tail: continue
        # un champ finit par ; ou = avant la premiere ( ; une methode a une (
        semi = tail.find(';')
        eq = tail.find('=')
        par = tail.find('(')
        field = (semi >= 0 and (par < 0 or semi < par)) or (eq >= 0 and (par < 0 or eq < par))
        if not field and par >= 0:
            line = s2.count('\n', 0, m.start()) + 1
            errors.append("%s ligne %d : [%s] pose devant une METHODE, pas un champ "
                          "(Unity refuse : CS0592)" % (path, line, m.group(1)))

# ---- 5b. type inconnu ---------------------------------------------------
import os.path
known_path = os.path.join(os.path.dirname(os.path.abspath(__file__)), "types-externes.txt")
external = set()
if os.path.exists(known_path):
    for line in io.open(known_path, encoding="utf-8"):
        line = line.split('#')[0].strip()
        if line: external.add(line)

used = set()
new_re = re.compile(r'\bnew\s+([A-Z]\w*)')
decl_type_re = re.compile(r'(?<![\w.<])([A-Z]\w*)(?:\[\])?\s+[a-z_]\w*\s*[=;,)]')
generic_re = re.compile(r'<\s*([A-Z]\w*)\s*[,>]')
for path, s2 in sources.items():
    for rx in (new_re, decl_type_re, generic_re):
        for m in rx.finditer(s2):
            used.add(m.group(1))

unknown = sorted(n for n in used
                 if n not in all_types and n not in external and n not in KEYWORDS)
for n in unknown:
    where = [p for p, s2 in sources.items()
             if re.search(r'(?<![\w.])' + re.escape(n) + r'(?![\w])', s2)]
    errors.append("type inconnu : %s (utilise dans %s). S'il vient d'Unity ou de .NET, "
                  "ajoute-le a Tools/types-externes.txt ; sinon il a ete supprime par erreur."
                  % (n, ", ".join(w.replace(ROOT + "/", "") for w in where[:3])))

# ---- 5c. API Unity perimees (erreur dans Unity 6) ----------------------
# Ce qui compile dans un tutoriel de 2022 peut etre une ERREUR dans Unity 6.
# GetInstanceID() l'a appris a Martin le 24/09/2026 (CS0619). Une ligne par
# piege : le motif, puis ce qu'il faut ecrire a la place.
OBSOLETE = [
    (r'\.GetInstanceID\s*\(', "GetInstanceID() est perime (CS0619) : n'en pas avoir besoin, ou un compteur a soi"),
    (r'\bFindObjectOfType\s*<', "FindObjectOfType est perime : FindFirstObjectByType"),
    (r'\bFindObjectsOfType\s*<', "FindObjectsOfType est perime : FindObjectsByType"),
    (r'\.velocity\b(?=[^;]*Rigidbody)', "Rigidbody.velocity est perime : linearVelocity"),
]
for path, s2 in sources.items():
    for pattern, advice in OBSOLETE:
        for m in re.finditer(pattern, s2):
            line = s2.count('\n', 0, m.start()) + 1
            errors.append("%s ligne %d : %s" % (path, line, advice))

print("%d fichiers, %d types" % (len(files), len(all_types)))
if errors:
    print("\n%d PROBLEME(S) :" % len(errors))
    for e in errors[:200]: print("  " + e)
    sys.exit(1)
print("OK : aucune anomalie")
