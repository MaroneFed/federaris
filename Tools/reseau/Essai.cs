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
