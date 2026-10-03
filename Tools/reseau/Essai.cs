using System;
using System.Diagnostics;
using System.Threading;
using Fief.Net;

// Un hote, deux invites, un faux joueur qui se trompe de version : tout le monde se parle
// sur 127.0.0.1, comme deux fenetres du jeu sur le meme PC.
static class Essai
{
    static int Main()
    {
        Stopwatch clock = Stopwatch.StartNew();
        Func<float> now = () => (float)clock.Elapsed.TotalSeconds;
        int port = 47000 + new Random().Next(1000);
        NetLink host = NetLink.Host(port, "Martin", 8);
        NetLink a = NetLink.Join("127.0.0.1", port, "Frere", now());
        NetLink b = NetLink.Join("127.0.0.1", port, "Martin", now());
        Loop(now, 1.5f, host, a, b);
        Check(a.Status == NetLink.State.Connected, "l'invite A est connecte");
        Check(b.Status == NetLink.State.Connected, "l'invite B est connecte");
        Check(host.Roster.Count == 3, "l'hote voit 3 joueurs (" + host.Roster.Count + ")");
        Check(a.Roster.Count == 3 && b.Roster.Count == 3, "les invites voient 3 joueurs");
        Check(a.MySlot != b.MySlot && a.MySlot > 0 && b.MySlot > 0, "places differentes (" + a.MySlot + ", " + b.MySlot + ")");
        Check(host.Roster.Exists(m => m.Name == "Martin 2"), "le second Martin devient Martin 2");
        // LES MESSAGES DU JEU (etape 2) : 30 % des paquets perdus expres. Les fiables arrivent
        // tous, une seule fois ; les autres, a peu pres 70 %.
        host.DropForTests = 0.3; a.DropForTests = 0.3;
        for (int i = 0; i < 40; i++)
        {
            a.Send(0, new byte[] { 1, (byte)i }, true);
            host.Send(a.MySlot, new byte[] { 2, (byte)i }, true);
            a.Send(0, new byte[] { 3, (byte)i }, false);
        }
        host.Broadcast(new byte[] { 4, 99 }, true);
        Loop(now, 3f, host, a, b);
        int[] gotHost = Count(host), gotA = Count(a), gotB = Count(b);
        Check(gotHost[1] == 40, "l'hote recoit les 40 fiables de A, une fois chacun (" + gotHost[1] + ")");
        Check(gotA[2] == 40, "A recoit les 40 fiables de l'hote (" + gotA[2] + ")");
        Check(gotHost[3] > 10 && gotHost[3] < 40, "des non fiables se perdent (" + gotHost[3] + "/40)");
        Check(gotA[4] == 1 && gotB[4] == 1, "le message a tous arrive chez A et B");
        Check(host.PendingCount == 0 && a.PendingCount == 0, "plus rien en attente d'accuse");
        host.DropForTests = 0; a.DropForTests = 0;
        Check(host.HasPeer(a.MySlot) && !host.HasPeer(7), "l'hote sait qui est la");

        // (06/10) LA LISTE DES PARTIES : un chercheur trouve l'hote tout seul, sans adresse.
        NetFinder finder = NetFinder.Start(port);
        for (float t0 = now(); now() - t0 < 1.2f; ) { finder.Poll(now()); host.Poll(now()); a.Poll(now()); b.Poll(now()); Thread.Sleep(10); }
        NetFinder.Found seen = finder.Games.Count > 0 ? finder.Games[0] : null;
        Check(seen != null && seen.Host == "Martin" && seen.Players == 3 && seen.Max == 8 && !seen.Started && seen.SameVersion,
              "le chercheur trouve la partie de Martin, 3/8 (" + finder.Games.Count + " trouvee(s))");
        finder.Dispose();
        // LE CODE : une adresse en 7 signes, et retour.
        string code = NetCode.Encode("192.168.1.23");
        Check(code.Length == 8 && NetCode.Decode(code) == "192.168.1.23" && NetCode.Decode(code.ToLowerInvariant().Replace("-", "")) == "192.168.1.23",
              "le code " + code + " redonne 192.168.1.23");
        Check(NetCode.ToAddress("26.14.200.7") == "26.14.200.7" && NetCode.Decode("hello") == null, "une adresse tapee reste une adresse");

        // (v31) LE MEME JEU PAR UN AUTRE TUYAU : un faux "Steam" en memoire (des numeros, pas
        // d'adresses IP). C'est ce que fait SteamWire dans le jeu : NetLink ne voit pas la difference.
        {
            Standard sw = new Standard();
            NetLink sh = NetLink.HostOver(new MemWire(sw, 76561198000000001UL), "Hote", 8);
            NetLink sa = NetLink.JoinOver(new MemWire(sw, 76561198000000002UL), new PeerId(76561198000000001UL), "Ami", now());
            NetLink sb = NetLink.JoinOver(new MemWire(sw, 76561198000000003UL), new PeerId(76561198000000001UL), "Cousin", now());
            Loop(now, 1f, sh, sa, sb);
            Check(sa.Status == NetLink.State.Connected && sb.Status == NetLink.State.Connected && sh.Roster.Count == 3,
                  "par Steam (simule) : deux amis entrent dans le salon (" + sh.Roster.Count + ")");
            for (int i = 0; i < 20; i++) sa.Send(0, new byte[] { 5, (byte)i }, true);
            sh.Broadcast(new byte[] { 6, 1 }, true);
            Loop(now, 1f, sh, sa, sb);
            int[] gh = Count(sh), ga = Count(sa), gb = Count(sb);
            Check(gh[5] == 20 && ga[6] == 1 && gb[6] == 1, "par Steam (simule) : les messages du jeu passent");
            sb.Dispose();
            Loop(now, 0.5f, sh, sa);
            Check(sh.Roster.Count == 2, "par Steam (simule) : le cousin part, l'hote le voit");
            sh.Dispose(); sa.Dispose();
        }

        // Un invite part : l'hote et l'autre invite le voient.
        a.Dispose();
        Loop(now, 1.5f, host, b);
        Check(host.Roster.Count == 2, "apres le depart de A, l'hote voit 2 joueurs (" + host.Roster.Count + ")");
        Check(b.Roster.Count == 2, "et B aussi (" + b.Roster.Count + ")");
        // Le salon ferme : un nouveau venu est refuse.
        host.Locked = true;
        NetLink c = NetLink.Join("127.0.0.1", port, "Retard", now());
        Loop(now, 1.5f, host, b, c);
        Check(c.Status == NetLink.State.Refused && c.RefusedFor == NetLink.Refusal.Started, "le retardataire est refuse (match commence)");
        Check(b.PingToHost >= 0f, "ping vers l'hote : " + b.PingToHost.ToString("0.0") + " ms");
        // L'hote s'en va : l'invite le voit.
        host.Dispose();
        Loop(now, 1f, b);
        Check(b.Status == NetLink.State.Lost, "l'hote parti, l'invite le voit");
        b.Dispose(); c.Dispose();
        Console.WriteLine(failures == 0 ? "RESEAU OK" : failures + " ECHEC(S)");
        return failures == 0 ? 0 : 1;
    }

    static int failures;
    static void Check(bool ok, string what) { Console.WriteLine((ok ? "  ok   " : "  ECHEC ") + what); if (!ok) failures++; }

    /// <summary>Vide la boite : combien de messages de chaque sorte (le premier octet).</summary>
    static int[] Count(NetLink l)
    {
        int[] n = new int[8];
        while (l.Inbox.Count > 0)
        {
            NetLink.Incoming m = l.Inbox.Dequeue();
            n[m.Data[0]]++;
        }
        return n;
    }

    static void Loop(Func<float> now, float seconds, params NetLink[] links)
    {
        float end = now() + seconds;
        while (now() < end)
        {
            foreach (NetLink l in links) l.Poll(now());
            Thread.Sleep(16);
        }
    }
}

/// <summary>Un faux standard Steam : chaque numero a sa boite aux lettres.</summary>
sealed class Standard
{
    public readonly System.Collections.Generic.Dictionary<ulong, System.Collections.Generic.Queue<(byte[], ulong)>> Boxes =
        new System.Collections.Generic.Dictionary<ulong, System.Collections.Generic.Queue<(byte[], ulong)>>();
}

/// <summary>Un tuyau en memoire qui se comporte comme SteamWire (des PeerId au lieu d'adresses IP).</summary>
sealed class MemWire : IWire
{
    readonly Standard sw;
    readonly ulong me;
    public MemWire(Standard sw, ulong me) { this.sw = sw; this.me = me; sw.Boxes[me] = new System.Collections.Generic.Queue<(byte[], ulong)>(); }
    public bool Receive(byte[] buffer, out int length, out System.Net.EndPoint from)
    {
        length = 0; from = null;
        System.Collections.Generic.Queue<(byte[], ulong)> box;
        if (!sw.Boxes.TryGetValue(me, out box) || box.Count == 0) return false;
        var m = box.Dequeue();
        System.Array.Copy(m.Item1, buffer, m.Item1.Length);
        length = m.Item1.Length;
        from = new PeerId(m.Item2);
        return true;
    }
    public void Send(byte[] data, int length, System.Net.EndPoint to)
    {
        System.Collections.Generic.Queue<(byte[], ulong)> box;
        PeerId p = to as PeerId;
        if (p == null || !sw.Boxes.TryGetValue(p.Id, out box)) return;
        byte[] copy = new byte[length];
        System.Array.Copy(data, copy, length);
        box.Enqueue((copy, me));
    }
    public void Close() { sw.Boxes.Remove(me); }
}
